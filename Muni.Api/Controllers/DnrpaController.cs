using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muni.Infrastructure;
using Muni.Infrastructure.Integrations.Dnrpa;

namespace Muni.Api.Controllers;

[ApiController]
[Route("api/v1/dnrpa")]
public class DnrpaController : ControllerBase
{
    private readonly IDnrpaImporter _importer;
    private readonly MuniDbContext _db;

    public DnrpaController(IDnrpaImporter importer, MuniDbContext db)
    {
        _importer = importer;
        _db = db;
    }
    public sealed class DnrpaSearchResponse
    {
        public Guid OwnerId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Domicilio { get; set; }
        public List<VehicleDto> Vehiculos { get; set; } = new();
    }

    public sealed class VehicleDto
    {
        public Guid VehicleId { get; set; }
        public string Patente { get; set; } = "";
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int Anio { get; set; }
        public string Categoria { get; set; } = "";
        public bool EsTitularActual { get; set; }
    }

    // existente: por rango
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] DnrpaQuery q, CancellationToken ct)
    {
        var r = await _importer.ImportAsync(q, ct);
        return Ok(r);
    }

    // nuevo: por DNI
    public class DniRequest
    {
        public string Dni { get; set; } = default!;
    }

    [HttpPost("sync-dni")]
    public async Task<IActionResult> SyncByDni([FromBody] DniRequest req, CancellationToken ct)
    {
        var r = await _importer.ImportByDniAsync(req.Dni, ct);
        return Ok(r);
    }

    // Mock para probar formato (no obligatorio, pero útil)
    [HttpGet("_mock")]
    public IActionResult Mock([FromQuery] int count = 3)
    {
        var list = new List<DnrpaTransaccionDto>();
        var now = DateTime.Now;
        for (int i = 0; i < count; i++)
        {
            var alta = i % 2 == 0;
            list.Add(new DnrpaTransaccionDto
            {
                FechaTransaccion = now.AddMinutes(-10 * i),
                TitularOrigen = alta ? null : $"ORIG_{i}",
                TitularDestino = $"DEST_{i}",
                CostoOperacion = 15000 + i,
                TipoTransaccion = alta ? "ALTA" : "TRANSFERENCIA",
                Marca = "Honda",
                Modelo = $"Wave{i}",
                AnioFabricacion = 2021 + (i % 3),
                NumeroMotor = $"MOT{i:000000}",
                CategoriaVehiculo = (i % 2 == 0) ? "MOTO_<=150" : "MOTO_>150",
                NumeroPatente = $"AA{i:000}BB",
                EjemplarPatente = "A"
            });
        }
        return Ok(list);
    }
    [HttpGet("buscar-por-dni")]
    public async Task<IActionResult> BuscarPorDni([FromQuery] string dni, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dni))
            return BadRequest(new { error = "dni requerido" });

        // 1) Importar eventos del DNI desde DNRPA
        await _importer.ImportByDniAsync(dni, ct);

        // 2) Buscar Owner por DNI
        var owner = await _db.Owners
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.CuitCuil == dni, ct);

        if (owner is null)
            return NotFound(new { error = "No se encontraron registros para ese DNI" });

        // 3) Vehículos donde este owner fue ALGUNA VEZ titular (histórico)
        var vehiculos = await _db.OwnershipHistories
            .Where(h => h.OwnerId == owner.Id)
            .Join(
                _db.Vehicles,
                h => h.VehicleId,
                v => v.Id,
                (h, v) => v
            )
            .Distinct()
            .AsNoTracking()
            .Select(v => new VehicleDto
            {
                VehicleId = v.Id,
                Patente = v.Patente,
                Marca = v.Marca,
                Modelo = v.Modelo,
                Anio = v.Anio,
                Categoria = v.Categoria,
                // 👇 Titular actual = el Owner que estás consultando coincide con el Owner actual del vehículo
                EsTitularActual = (v.OwnerId == owner.Id)
            })
            .ToListAsync(ct);

        var resp = new DnrpaSearchResponse
        {
            OwnerId = owner.Id,
            Nombre = owner.Nombre,
            Domicilio = owner.Domicilio,
            Vehiculos = vehiculos
        };

        return Ok(resp);
    }
}
