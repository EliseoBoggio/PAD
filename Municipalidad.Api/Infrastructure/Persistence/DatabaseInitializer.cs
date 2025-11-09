using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Municipalidad.Api.Domain.Models;

namespace Municipalidad.Api.Infrastructure.Persistence;

public class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MunicipalidadDbContext>();
        var hasMigrations = (await context.Database.GetMigrationsAsync(cancellationToken)).Any();
        if (hasMigrations)
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!await context.Titulares.AnyAsync(cancellationToken))
        {
            var titular = new Titular
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "20123456789",
                FirstName = "Juan",
                LastName = "Pérez",
                Email = "juan.perez@example.com",
                Phone = "+541112223344"
            };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                LicensePlate = "AA123BB",
                Brand = "Ford",
                Model = "Focus",
                Year = 2020,
                Owner = titular
            };

            context.Titulares.Add(titular);
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
