using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Municipalidad.Api.Infrastructure.Persistence;
using Municipalidad.Api.Services;

namespace Municipalidad.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly VehicleRegistryService _vehicleRegistryService;

    public VehiclesController(MunicipalidadDbContext dbContext, VehicleRegistryService vehicleRegistryService)
    {
        _dbContext = dbContext;
        _vehicleRegistryService = vehicleRegistryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicles(CancellationToken cancellationToken)
    {
        var vehicles = await _dbContext.Vehicles.Include(v => v.Owner).ToListAsync(cancellationToken);
        return Ok(vehicles);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetVehicle(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles.Include(v => v.Owner).SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpPost("sync/{licensePlate}")]
    public async Task<IActionResult> SyncVehicle(string licensePlate, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRegistryService.SyncVehicleAsync(licensePlate, cancellationToken);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }
}
