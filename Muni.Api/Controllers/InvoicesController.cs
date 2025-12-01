// Muni.Api/Controllers/InvoicesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Muni.Infrastructure;
using Muni.Domain;
using Muni.Application; // IBillingService
using Muni.Infrastructure.Integrations.PagoFacil;
using Muni.Application.Printing;

namespace Muni.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly MuniDbContext _db;
    private readonly IBillingService _billing;           // << nuevo
    private readonly string _empresa4;

    public InvoicesController(
        MuniDbContext db,
        IBillingService billing,                         // << nuevo
        IOptions<PagoFacilOptions> pf)
    {
        _db = db;
        _billing = billing;                              // << nuevo
        _empresa4 = pf.Value.Empresa4 ?? "0000";
    }

    /// <summary>Emite la factura de una obligación y genera el código de barras de Pago Fácil.</summary>
    [HttpPost("{obligationId:guid}")]
    public async Task<IActionResult> Emitir(Guid obligationId, CancellationToken ct)
    {
        var obl = await _db.TaxObligations
            .Include(o => o.Vehicle).ThenInclude(v => v.Owner)
            .FirstOrDefaultAsync(o => o.Id == obligationId, ct);

        if (obl == null) return NotFound(new { error = "Obligación no encontrada" });
        if (obl.Estado == "PAGADA") return Conflict(new { error = "La obligación ya está pagada" });

        // idempotencia simple por obligación
        var existing = await _db.Invoices.FirstOrDefaultAsync(i => i.ObligationId == obl.Id, ct);
        if (existing != null) return Ok(new InvoiceResponse(existing));

        var cliente14 = BillingHelpers.BuildCliente14(obl, obl.Vehicle.Owner);
        var importeV1 = Math.Round(obl.ImportePrimerVenc, 2);
        var recargoV2 = Math.Round(obl.RecargoSegundoVenc, 2);
        var vto1 = obl.FechaPrimerVenc;
        var vto2 = obl.FechaSegundoVenc;

        var barcode = PagoFacilBarcode.Build(_empresa4, importeV1, vto1, cliente14, recargoV2, vto2);

        var numero = BillingHelpers.MakeInvoiceNumber(_db);
        var inv = new Invoice
        {
            Id = Guid.NewGuid(),
            ObligationId = obl.Id,
            Numero = numero,
            Importe = importeV1,
            Vto1 = vto1,
            Vto2 = vto2,
            Barcode = barcode,
            EmpresaPF4 = _empresa4,
            Cliente14 = cliente14,
            Moneda1 = "0",
            Estado = "EMITIDA"
        };

        obl.Estado = "FACTURADA";

        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync(ct);
        return Ok(new InvoiceResponse(inv));
    }

    /// <summary>Emisión on-demand por identificador (PATENTE/CUIT/OWNER_ID) y período YYYYMM. Crea obligación si falta.</summary>
    [HttpPost("emitir")]
    public async Task<IActionResult> EmitirOnDemand([FromBody] EmitirRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Identificador) ||
            string.IsNullOrWhiteSpace(req.TipoIdentificador) ||
            string.IsNullOrWhiteSpace(req.Periodo) || req.Periodo.Length != 6)
            return BadRequest(new { error = "identificador/tipo/periodo inválidos (Periodo=YYYYMM)" });

        // 1) Resolver vehículo y owner
        Vehicle? veh = null;
        Owner? owner = null;
        switch (req.TipoIdentificador.Trim().ToUpperInvariant())
        {
            case "PATENTE":
                veh = await _db.Vehicles.Include(v => v.Owner)
                        .FirstOrDefaultAsync(v => v.Patente == req.Identificador, ct);
                owner = veh?.Owner;
                break;

            case "CUIT":
                owner = await _db.Owners.FirstOrDefaultAsync(o => o.CuitCuil == req.Identificador, ct);
                if (owner != null)
                    veh = await _db.Vehicles.Include(v => v.Owner)
                           .FirstOrDefaultAsync(v => v.OwnerId == owner.Id, ct);
                break;

            case "OWNER_ID":
                if (Guid.TryParse(req.Identificador, out var ownerId))
                {
                    owner = await _db.Owners.FirstOrDefaultAsync(o => o.Id == ownerId, ct);
                    if (owner != null)
                        veh = await _db.Vehicles.Include(v => v.Owner)
                               .FirstOrDefaultAsync(v => v.OwnerId == owner.Id, ct);
                }
                break;

            default:
                return BadRequest(new { error = "TipoIdentificador debe ser PATENTE | CUIT | OWNER_ID" });
        }
        if (veh is null || owner is null)
            return NotFound(new { error = "No se encontró vehículo/propietario para el identificador dado" });

        // 2) Buscar/crear obligación del período
        var obl = await _db.TaxObligations
            .Include(o => o.Vehicle)
            .FirstOrDefaultAsync(o => o.VehicleId == veh.Id && o.Periodo == req.Periodo, ct);

        if (obl == null)
        {
            var (imp1, recargo, v1, v2) = _billing.Calcular(veh, req.Periodo);
            obl = new TaxObligation
            {
                Id = Guid.NewGuid(),
                VehicleId = veh.Id,
                Periodo = req.Periodo,
                ImportePrimerVenc = imp1,
                RecargoSegundoVenc = recargo,
                FechaPrimerVenc = v1,
                FechaSegundoVenc = v2,
                Estado = "FACTURADA"
            };
            _db.TaxObligations.Add(obl);
            await _db.SaveChangesAsync(ct);
        }

        // 3) Ver si ya hay Invoice
        var inv = await _db.Invoices.FirstOrDefaultAsync(i => i.ObligationId == obl.Id, ct);
        if (inv != null && !req.Overwrite)
            return Ok(new EmitirResponse(inv, obl));

        // 4) Cliente14 + Barcode
        var cliente14 = BillingHelpers.BuildCliente14(obl, owner);
        var barcode = PagoFacilBarcode.Build(
            _empresa4,
            Math.Round(obl.ImportePrimerVenc, 2),
            obl.FechaPrimerVenc,
            cliente14,
            Math.Round(obl.RecargoSegundoVenc, 2),
            obl.FechaSegundoVenc
        );

        // 5) Crear/Reemplazar Invoice
        if (inv == null)
        {
            inv = new Invoice
            {
                Id = Guid.NewGuid(),
                ObligationId = obl.Id,
                Numero = BillingHelpers.MakeInvoiceNumber(_db),
                Importe = Math.Round(obl.ImportePrimerVenc, 2),
                Vto1 = obl.FechaPrimerVenc,
                Vto2 = obl.FechaSegundoVenc,
                Barcode = barcode,
                EmpresaPF4 = _empresa4,
                Cliente14 = cliente14,
                Moneda1 = "0",
                Estado = "EMITIDA"
            };
            _db.Invoices.Add(inv);
        }
        else
        {
            inv.EmpresaPF4 = _empresa4;
            inv.Cliente14 = cliente14;
            inv.Barcode = barcode;
            inv.Importe = Math.Round(obl.ImportePrimerVenc, 2);
            inv.Moneda1 = "0";
            inv.Vto1 = obl.FechaPrimerVenc;
            inv.Vto2 = obl.FechaSegundoVenc;
            inv.Estado = "EMITIDA";
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new EmitirResponse(inv, obl));
    }

    /// <summary>Obtiene una factura por Id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var inv = await _db.Invoices
            .Include(i => i.Obligation)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inv == null) return NotFound();
        return Ok(new InvoiceResponse(inv));
    }

    /// <summary>Descarga el PDF de una factura.</summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var inv = await _db.Invoices
            .Include(i => i.Obligation).ThenInclude(o => o.Vehicle).ThenInclude(v => v.Owner)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inv == null) return NotFound(new { error = "Factura no encontrada" });

        var v = inv.Obligation.Vehicle;
        var owner = v.Owner;

        var bytes = InvoicePdfService.BuildPdf(inv, v, owner);
        var fileName = $"Factura_{inv.Numero?.Replace("/", "-") ?? inv.Id.ToString()}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    // ======== DTOs / Requests ========

    public sealed class EmitirRequest
    {
        public string Identificador { get; set; } = "";     // "ABC123" o "20-123..." o GUID
        public string TipoIdentificador { get; set; } = ""; // "PATENTE" | "CUIT" | "OWNER_ID"
        public string Periodo { get; set; } = "";           // "YYYYMM"
        public bool Overwrite { get; set; } = false;
    }

    public record InvoiceResponse(Guid Id, string Numero, decimal Importe, DateOnly Vto1, DateOnly Vto2, string Barcode, string Cliente14)
    {
        public InvoiceResponse(Invoice i) : this(i.Id, i.Numero ?? "", i.Importe, i.Vto1, i.Vto2, i.Barcode, i.Cliente14 ?? "") { }
    }

    public sealed class EmitirResponse
    {
        public Guid InvoiceId { get; set; }
        public string Periodo { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Barcode42 { get; set; } = "";
        public string Cliente14 { get; set; } = "";
        public decimal ImportePrimerVenc { get; set; }
        public DateOnly FechaPrimerVenc { get; set; }
        public decimal RecargoSegundoVenc { get; set; }
        public DateOnly FechaSegundoVenc { get; set; }
        public string? PdfUrl { get; set; }

        public EmitirResponse(Invoice inv, TaxObligation obl)
        {
            InvoiceId = inv.Id;
            Periodo = obl.Periodo;
            Estado = inv.Estado;
            Barcode42 = inv.Barcode;
            Cliente14 = inv.Cliente14 ?? "";
            ImportePrimerVenc = obl.ImportePrimerVenc;
            FechaPrimerVenc = obl.FechaPrimerVenc;
            RecargoSegundoVenc = obl.RecargoSegundoVenc;
            FechaSegundoVenc = obl.FechaSegundoVenc;
            PdfUrl = null;
        }
    }
}

