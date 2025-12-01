using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muni.Infrastructure;
using Muni.Domain;

namespace Muni.Api.Controllers;

[ApiController]
[Route("api/v1/owners")]
public class OwnersController : ControllerBase
{
    private readonly MuniDbContext _db;
    public OwnersController(MuniDbContext db) => _db = db;

    public sealed class OwnerVehicleDto
    {
        public Guid VehicleId { get; set; }
        public string Patente { get; set; } = "";
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int Anio { get; set; }
        public string Categoria { get; set; } = "";
    }

    public sealed class OwnerDto
    {
        public Guid OwnerId { get; set; }
        public string Nombre { get; set; } = "";
        public string CuitCuil { get; set; } = "";
        public string? Domicilio { get; set; }
        public List<OwnerVehicleDto> Vehicles { get; set; } = new();
    }

    /// <summary>
    /// Lista propietarios con sus vehículos (opcionalmente filtrado por nombre/CUIT).
    /// Datos provenientes de DNRPA (vía importer).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken ct)
    {
        var ownersQuery = _db.Owners.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            ownersQuery = ownersQuery.Where(o =>
                o.Nombre.Contains(term) || o.CuitCuil.Contains(term));
        }

        var owners = await ownersQuery
            .OrderBy(o => o.Nombre)
            .Take(100)
            .ToListAsync(ct);

        var ownerIds = owners.Select(o => o.Id).ToList();

        var vehicles = await _db.Vehicles
            .Where(v => ownerIds.Contains(v.OwnerId))
            .ToListAsync(ct);

        var result = owners.Select(o => new OwnerDto
        {
            OwnerId = o.Id,
            Nombre = o.Nombre,
            CuitCuil = o.CuitCuil,
            Domicilio = o.Domicilio,
            Vehicles = vehicles
                .Where(v => v.OwnerId == o.Id)
                .Select(v => new OwnerVehicleDto
                {
                    VehicleId = v.Id,
                    Patente = v.Patente,
                    Marca = v.Marca,
                    Modelo = v.Modelo,
                    Anio = v.Anio,
                    Categoria = v.Categoria
                })
                .ToList()
        }).ToList();

        return Ok(result);
    }
}

