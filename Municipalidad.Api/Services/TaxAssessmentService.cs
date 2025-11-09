using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Municipalidad.Api.Domain.Models;
using Municipalidad.Api.Infrastructure.Persistence;

namespace Municipalidad.Api.Services;

public class TaxAssessmentService
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly ILogger<TaxAssessmentService> _logger;

    public TaxAssessmentService(MunicipalidadDbContext dbContext, ILogger<TaxAssessmentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TributoMensual> AssessMonthlyTaxAsync(Guid vehicleId, DateOnly period, CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles.SingleOrDefaultAsync(v => v.Id == vehicleId, cancellationToken)
            ?? throw new InvalidOperationException($"Vehicle {vehicleId} not found");

        var baseAmount = CalculateBaseAmount(vehicle);
        var surcharges = CalculateSurcharges(period);

        var existing = await _dbContext.TributosMensuales.SingleOrDefaultAsync(t => t.VehicleId == vehicleId && t.Period == period, cancellationToken);
        if (existing is not null)
        {
            existing.BaseAmount = baseAmount;
            existing.Surcharges = surcharges;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Tributo updated for vehicle {VehicleId} period {Period}", vehicleId, period);
            return existing;
        }

        var tributo = new TributoMensual
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            Period = period,
            BaseAmount = baseAmount,
            Surcharges = surcharges
        };

        _dbContext.TributosMensuales.Add(tributo);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tributo created for vehicle {VehicleId} period {Period}", vehicleId, period);
        return tributo;
    }

    private static decimal CalculateBaseAmount(Vehicle vehicle)
    {
        var age = DateTime.UtcNow.Year - vehicle.Year;
        return Math.Max(1000m, 5000m - age * 150m);
    }

    private static decimal CalculateSurcharges(DateOnly period)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalMonths = (today.Year - period.Year) * 12 + today.Month - period.Month;
        if (totalMonths < 0)
        {
            totalMonths = 0;
        }

        return totalMonths * 50m;
    }
}
