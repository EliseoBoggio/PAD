using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muni.Infrastructure;
using Muni.Domain;

namespace Muni.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly MuniDbContext _db;
    public AdminController(MuniDbContext db) => _db = db;

    /// <summary>
    /// Crea datos de prueba: 1 Owner + 1 Vehicle (Activo).
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        // Evitá duplicados si ya corriste el seed
        var exists = await _db.Vehicles.AnyAsync(v => v.Patente == "ABC123", ct);
        if (exists) return Ok(new { created = false, message = "Ya existe ABC123" });

        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            CuitCuil = "20-12345678-3",
            Nombre = "Juan Perez",
            Domicilio = "Av. Siempreviva 742"
        };
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Patente = "ABC123",
            Marca = "Honda",
            Modelo = "CG150",
            Anio = 2022,
            Categoria = "MOTO_<=150",
            OwnerId = owner.Id,
            Owner = owner,
            Activo = true
        };
        _db.Owners.Add(owner);
        _db.Vehicles.Add(vehicle);

        // (Opcional) historial de titularidad
        _db.OwnershipHistories.Add(new OwnershipHistory
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.Id,
            OwnerId = owner.Id,
            Desde = DateOnly.FromDateTime(DateTime.Today),
            Motivo = "ALTA"
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { created = true, owner = owner.Id, vehicle = vehicle.Id, patente = vehicle.Patente });
    }
}
