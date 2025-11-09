using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Municipalidad.Api.Clients;
using Municipalidad.Api.Domain.Models;
using Municipalidad.Api.Infrastructure.Persistence;

namespace Municipalidad.Api.Services;

public class VehicleRegistryService
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly DnrpaClient _dnrpaClient;
    private readonly ILogger<VehicleRegistryService> _logger;

    public VehicleRegistryService(MunicipalidadDbContext dbContext, DnrpaClient dnrpaClient, ILogger<VehicleRegistryService> logger)
    {
        _dbContext = dbContext;
        _dnrpaClient = dnrpaClient;
        _logger = logger;
    }

    public async Task<Vehicle?> SyncVehicleAsync(string licensePlate, CancellationToken cancellationToken)
    {
        var externalVehicle = await _dnrpaClient.GetVehicleAsync(licensePlate, cancellationToken);
        if (externalVehicle is null)
        {
            return null;
        }

        var existingOwner = await _dbContext.Titulares.SingleOrDefaultAsync(t => t.Id == externalVehicle.OwnerId, cancellationToken);
        if (existingOwner is null && externalVehicle.Owner is not null)
        {
            _dbContext.Titulares.Add(externalVehicle.Owner);
        }

        var existingVehicle = await _dbContext.Vehicles.Include(v => v.Owner).SingleOrDefaultAsync(v => v.LicensePlate == licensePlate, cancellationToken);
        if (existingVehicle is null)
        {
            _dbContext.Vehicles.Add(externalVehicle);
        }
        else
        {
            existingVehicle.Brand = externalVehicle.Brand;
            existingVehicle.Model = externalVehicle.Model;
            existingVehicle.Year = externalVehicle.Year;
            existingVehicle.OwnerId = externalVehicle.OwnerId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Vehicle {LicensePlate} synchronized", licensePlate);
        return await _dbContext.Vehicles.Include(v => v.Owner).SingleAsync(v => v.LicensePlate == licensePlate, cancellationToken);
    }
}
