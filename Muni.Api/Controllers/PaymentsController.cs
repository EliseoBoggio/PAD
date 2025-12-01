using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muni.Infrastructure;
using Muni.Domain;

namespace Muni.Api.Controllers;

public sealed class PaymentDto
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime FechaAcreditacion { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceEstado { get; set; } = "";
    public string? InvoicePeriodo { get; set; }
    public string? Patente { get; set; }
}

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly MuniDbContext _db;

    public PaymentsController(MuniDbContext db) => _db = db;

    /// <summary>
    /// Historial de pagos (últimos N).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> Get([FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (take <= 0 || take > 500) take = 50;

        var query =
            from p in _db.Payments
            join i in _db.Invoices on p.InvoiceId equals i.Id
            join o in _db.TaxObligations on i.ObligationId equals o.Id
            join v in _db.Vehicles on o.VehicleId equals v.Id
            orderby p.FechaAcreditacion descending
            select new PaymentDto
            {
                Id = p.Id,
                Provider = p.Provider,
                ExternalId = p.ExternalId,
                Monto = p.Monto,
                FechaAcreditacion = p.FechaAcreditacion,
                InvoiceId = i.Id,
                InvoiceEstado = i.Estado,
                InvoicePeriodo = o.Periodo,
                Patente = v.Patente
            };

        var list = await query.Take(take).ToListAsync(ct);
        return Ok(list);
    }
}

