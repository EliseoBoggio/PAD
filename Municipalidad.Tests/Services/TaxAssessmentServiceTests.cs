using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Municipalidad.Api.Domain.Models;
using Municipalidad.Api.Infrastructure.Persistence;
using Municipalidad.Api.Services;

namespace Municipalidad.Tests.Services;

public class TaxAssessmentServiceTests
{
    [Fact]
    public async Task AssessMonthlyTaxAsync_CreatesNewTribute()
    {
        var options = new DbContextOptionsBuilder<MunicipalidadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new MunicipalidadDbContext(options);
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = "TEST123",
            Brand = "Ford",
            Model = "Fiesta",
            Year = 2019,
            OwnerId = Guid.NewGuid()
        };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var service = new TaxAssessmentService(context, NullLogger<TaxAssessmentService>.Instance);
        var period = new DateOnly(2024, 1, 1);

        var tributo = await service.AssessMonthlyTaxAsync(vehicle.Id, period, CancellationToken.None);

        Assert.NotNull(tributo);
        Assert.Equal(vehicle.Id, tributo.VehicleId);
        Assert.Equal(period, tributo.Period);
        Assert.True(tributo.BaseAmount >= 1000m);
    }
}
