// Muni.Api/Controllers/MercadoPagoController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muni.Infrastructure;
using Muni.Domain;
using MercadoPago.Config;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;

namespace Muni.Api.Controllers;

[ApiController]
[Route("api/v1/mercadopago")]
public class MercadoPagoController : ControllerBase
{
    private readonly MuniDbContext _db;
    private readonly IConfiguration _cfg;

    public MercadoPagoController(MuniDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;

        if (string.IsNullOrWhiteSpace(MercadoPagoConfig.AccessToken))
            MercadoPagoConfig.AccessToken = _cfg["MercadoPago:AccessToken"];
    }

    /// <summary>
    /// Crea la Preferencia para una factura y devuelve los links.
    /// NOTA: Agrega invoiceId en la NotificationUrl para que el webhook concilie sin consultar MP.
    /// </summary>
    [HttpPost("preferencia/{invoiceId:guid}")]
    public async Task<IActionResult> CrearPreferencia(Guid invoiceId, CancellationToken ct)
    {
        var inv = await _db.Invoices
            .Include(i => i.Obligation).ThenInclude(o => o.Vehicle)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (inv is null) return NotFound(new { error = "Factura no encontrada" });
        if (inv.Estado == "PAGADA") return Conflict(new { error = "La factura ya está pagada" });

        var title = string.IsNullOrWhiteSpace(inv.Obligation?.Periodo)
            ? $"Impuesto Vehicular - {inv.Id}"
            : $"Impuesto Vehicular {inv.Obligation.Periodo} - {inv.Obligation.Vehicle.Patente}";
        var price = inv.Importe <= 0 ? 1m : inv.Importe;

        // Base NotificationUrl desde appsettings, le agregamos ?invoiceId=...
        var baseNotif = _cfg["MercadoPago:NotificationUrl"]?.TrimEnd('/')
                        ?? throw new InvalidOperationException("MercadoPago:NotificationUrl no configurada.");
        var notifUrl = $"{baseNotif}?invoiceId={inv.Id}";

        var prefReq = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest> {
                new() { Title = title, Quantity = 1, CurrencyId = "ARS", UnitPrice = price }
            },
            ExternalReference = inv.Id.ToString(), // por si en el futuro volvés a validar “full”
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = _cfg["MercadoPago:BackUrls:Success"],
                Failure = _cfg["MercadoPago:BackUrls:Failure"],
                Pending = _cfg["MercadoPago:BackUrls:Pending"]
            },
            AutoReturn = "approved",
            NotificationUrl = notifUrl
        };

        var prefClient = new PreferenceClient();
        Preference pref = await prefClient.CreateAsync(prefReq);

        return Ok(new
        {
            invoiceId = inv.Id,
            externalReference = pref.ExternalReference,
            preferenceId = pref.Id,
            initPoint = pref.InitPoint,
            sandboxInitPoint = pref.SandboxInitPoint
        });
    }

    /// <summary>
    /// Webhook “simple”: toma invoiceId de la query y acredita directo (sin consultar MP).
    /// Idempotente por (Provider, ExternalId).
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var q = Request.Query;

        // Lo pasamos nosotros al crear la preferencia (NotificationUrl?invoiceId=...)
        var invoiceIdStr = q["invoiceId"].ToString();

        // Datos que pueden venir en la query
        var paymentId = q["data.id"].ToString();   // cuando type=payment
        var orderId = q["id"].ToString();        // cuando topic=merchant_order
        var type = q["type"].ToString();
        var topic = q["topic"].ToString();

        Console.WriteLine($"[MP] WH hit type={type} topic={topic} invoiceId={invoiceIdStr} paymentId={paymentId} orderId={orderId}");

        if (!Guid.TryParse(invoiceIdStr, out var invoiceId))
        {
            Console.WriteLine("[MP] WH sin invoiceId válido => OK");
            return Ok();
        }

        var inv = await _db.Invoices.AsTracking().FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (inv is null)
        {
            Console.WriteLine($"[MP] Invoice {invoiceId} no encontrada => OK");
            return Ok();
        }

        // ============= AQUÍ VA EL CAMBIO CLAVE DE IDEMPOTENCIA =============
        // 1) ¿Ya hay un Payment para esta factura con Provider=MP?
        var existing = await _db.Payments
            .FirstOrDefaultAsync(p => p.Provider == "MP" && p.InvoiceId == inv.Id, ct);

        // 2) Si existe, ACTUALIZO en vez de insertar otro (para conservar ambos IDs si llegan en distintos webhooks)
        if (existing != null)
        {
            // Si ahora tengo un paymentId y el ExternalId anterior era MO/INV, lo "mejoro"
            if (!string.IsNullOrWhiteSpace(paymentId) && !existing.ExternalId.StartsWith("MP:PAY:"))
            {
                existing.ExternalId = $"MP:PAY:{paymentId}";
            }

            // (Opcional) guardar también el orderId en un campo metadata si lo tenés; si no, no hace falta.
            // existing.Metadata = ... (si definiste el campo)

            // Aseguro estados
            inv.Estado = "PAGADA";
            var oblE = await _db.TaxObligations.FirstOrDefaultAsync(o => o.Id == inv.ObligationId, ct);
            if (oblE != null) oblE.Estado = "PAGADA";

            await _db.SaveChangesAsync(ct);
            Console.WriteLine($"[MP] Ya había Payment para invoice={inv.Id}. Actualizado ExternalId={existing.ExternalId}. OK");
            return Ok();
        }
        // ===================================================================

        // Si no existía un Payment para esta factura, creo UNO.
        // ExternalId estable: priorizo paymentId; si no hay, uso orderId; sino, fallback por invoiceId
        string externalId =
            !string.IsNullOrWhiteSpace(paymentId) ? $"MP:PAY:{paymentId}" :
            !string.IsNullOrWhiteSpace(orderId) ? $"MP:MO:{orderId}" :
                                                    $"MP:INV:{invoiceId}";

        // Monto desde TU factura
        var monto = inv.Importe <= 0 ? 1m : inv.Importe;

        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = inv.Id,
            Provider = "MP",
            Monto = monto,
            FechaAcreditacion = DateTime.Now,
            ExternalId = externalId,
            Estado = "APPLIED"
        });

        inv.Estado = "PAGADA";
        var obl = await _db.TaxObligations.FirstOrDefaultAsync(o => o.Id == inv.ObligationId, ct);
        if (obl != null) obl.Estado = "PAGADA";

        var rows = await _db.SaveChangesAsync(ct);
        Console.WriteLine($"[MP] Aplicado OK invoice={inv.Id} externalId={externalId} rows={rows}");

        return Ok();
    }
    [HttpGet("success")]
    public IActionResult Success(
    [FromQuery] string? external_reference,
    [FromQuery] string? payment_id,
    [FromQuery] string? merchant_order_id)
    {
        // Redirigir al front (home del proyecto)
        return Redirect("/");
    }

}


