using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Municipalidad.Api.Clients;
using Municipalidad.Api.Domain.Models;
using Municipalidad.Api.Infrastructure.Persistence;

namespace Municipalidad.Api.Services;

public class BillingService
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly ArcaClient _arcaClient;
    private readonly ILogger<BillingService> _logger;

    public BillingService(MunicipalidadDbContext dbContext, ArcaClient arcaClient, ILogger<BillingService> logger)
    {
        _dbContext = dbContext;
        _arcaClient = arcaClient;
        _logger = logger;
    }

    public async Task<Factura> GenerateInvoiceAsync(Guid tributoId, CancellationToken cancellationToken)
    {
        var tributo = await _dbContext.TributosMensuales.Include(t => t.Invoice).SingleOrDefaultAsync(t => t.Id == tributoId, cancellationToken)
            ?? throw new InvalidOperationException($"Tributo {tributoId} not found");

        if (tributo.Invoice is not null)
        {
            return tributo.Invoice;
        }

        var invoice = new Factura
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"FAC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            IssuedOn = DateTime.UtcNow,
            Amount = tributo.TotalAmount,
            Status = "Pending"
        };

        tributo.Invoice = invoice;
        _dbContext.Facturas.Add(invoice);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var authorizationCode = await _arcaClient.AuthorizeInvoiceAsync(invoice, cancellationToken);
        if (!string.IsNullOrWhiteSpace(authorizationCode))
        {
            invoice.Status = "Authorized";
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Invoice {InvoiceNumber} authorized with code {AuthorizationCode}", invoice.InvoiceNumber, authorizationCode);
        }
        else
        {
            _logger.LogWarning("Invoice {InvoiceNumber} could not be authorized", invoice.InvoiceNumber);
        }

        return invoice;
    }
}
