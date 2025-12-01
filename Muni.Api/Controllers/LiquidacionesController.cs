// Muni.Api/Controllers/LiquidacionesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muni.Infrastructure;
using Muni.Application;
using Muni.Domain;

namespace Muni.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LiquidacionesController : ControllerBase
{
    private readonly MuniDbContext _db;
    private readonly IBillingService _billing;

    public LiquidacionesController(MuniDbContext db, IBillingService billing)
    {
        _db = db;
        _billing = billing;
    }

    /// <summary>
    /// Genera obligaciones para todos los vehículos activos del período YYYYMM.
    /// Idempotente: no duplica si ya existe (VehicleId+Periodo).
    /// </summary>
    [HttpPost("generar")]
    public async Task<IActionResult> Generar([FromQuery] string periodo, [FromQuery] bool overwrite = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(periodo) || periodo.Length != 6 || !periodo.All(char.IsDigit))
            return BadRequest("Periodo inválido. Formato esperado: YYYYMM");

        var vehicles = await _db.Vehicles
            .Where(v => v.Activo)
            .Include(v => v.Owner)
            .ToListAsync(ct);

        int creadas = 0, omitidas = 0, actualizadas = 0;

        foreach (var v in vehicles)
        {
            var existing = await _db.TaxObligations
                .FirstOrDefaultAsync(o => o.VehicleId == v.Id && o.Periodo == periodo, ct);

            var (imp1, recargo, v1, v2) = _billing.Calcular(v, periodo);

            if (existing == null)
            {
                var o = new TaxObligation
                {
                    Id = Guid.NewGuid(),
                    VehicleId = v.Id,
                    Periodo = periodo,
                    ImportePrimerVenc = imp1,
                    RecargoSegundoVenc = recargo,
                    FechaPrimerVenc = v1,
                    FechaSegundoVenc = v2,
                    Estado = "GENERADA"
                };
                _db.TaxObligations.Add(o);
                creadas++;
            }
            else
            {
                if (!overwrite)
                {
                    omitidas++;
                }
                else
                {
                    // Solo permitir overwrite si no está PAGADA
                    if (existing.Estado == "PAGADA")
                    {
                        omitidas++;
                    }
                    else
                    {
                        existing.ImportePrimerVenc = imp1;
                        existing.RecargoSegundoVenc = recargo;
                        existing.FechaPrimerVenc = v1;
                        existing.FechaSegundoVenc = v2;
                        if (existing.Estado == "FACTURADA") existing.Estado = "GENERADA";
                        actualizadas++;
                    }
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { periodo, creadas, actualizadas, omitidas });
    }
}
