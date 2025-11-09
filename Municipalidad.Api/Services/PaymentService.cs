using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Municipalidad.Api.Clients;
using Municipalidad.Api.Domain.Models;
using Municipalidad.Api.Infrastructure.Persistence;

namespace Municipalidad.Api.Services;

public class PaymentService
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly IPagoFacilClient _pagoFacilClient;
    private readonly IMercadoPagoClient _mercadoPagoClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        MunicipalidadDbContext dbContext,
        IPagoFacilClient pagoFacilClient,
        IMercadoPagoClient mercadoPagoClient,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _pagoFacilClient = pagoFacilClient;
        _mercadoPagoClient = mercadoPagoClient;
        _logger = logger;
    }

    public async Task<(string? barcode, string? checkoutUrl)> PreparePaymentMethodsAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.Facturas.SingleOrDefaultAsync(f => f.Id == invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        var barcode = await _pagoFacilClient.GenerateBarcodeAsync(invoice.InvoiceNumber, invoice.Amount, cancellationToken);
        var checkoutUrl = await _mercadoPagoClient.CreatePaymentButtonAsync(invoice.InvoiceNumber, invoice.Amount, cancellationToken);

        if (barcode is null && checkoutUrl is null)
        {
            throw new InvalidOperationException("Unable to prepare payment methods");
        }

        var payment = new Pago
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Provider = checkoutUrl is not null ? "MercadoPago" : "PagoFacil",
            ExternalReference = checkoutUrl ?? barcode ?? string.Empty,
            Amount = invoice.Amount,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        _dbContext.Pagos.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (barcode, checkoutUrl);
    }

    public async Task<bool> ProcessMercadoPagoCallbackAsync(string signature, string externalReference, CancellationToken cancellationToken)
    {
        if (!_mercadoPagoClient.ValidateWebhookSignature(signature))
        {
            _logger.LogWarning("Invalid MercadoPago signature");
            return false;
        }

        var payment = await _dbContext.Pagos.SingleOrDefaultAsync(p => p.ExternalReference == externalReference, cancellationToken);
        if (payment is null)
        {
            return false;
        }

        payment.Status = "Paid";
        payment.ConfirmedAt = DateTime.UtcNow;

        var invoice = await _dbContext.Facturas.SingleAsync(f => f.Id == payment.InvoiceId, cancellationToken);
        invoice.Status = "Paid";

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
