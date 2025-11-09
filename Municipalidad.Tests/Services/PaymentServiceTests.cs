using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Municipalidad.Api.Clients;
using Municipalidad.Api.Domain.Models;
using Municipalidad.Api.Infrastructure.Persistence;
using Municipalidad.Api.Services;

namespace Municipalidad.Tests.Services;

public class PaymentServiceTests
{
    [Fact]
    public async Task PreparePaymentMethodsAsync_StoresPaymentRecord()
    {
        var options = new DbContextOptionsBuilder<MunicipalidadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new MunicipalidadDbContext(options);
        var invoice = new Factura
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "FAC-TEST",
            Amount = 1234.56m,
            IssuedOn = DateTime.UtcNow,
            Status = "Authorized"
        };
        context.Facturas.Add(invoice);
        await context.SaveChangesAsync();

        var pagoFacilClient = new Mock<IPagoFacilClient>();
        pagoFacilClient.Setup(c => c.GenerateBarcodeAsync(invoice.InvoiceNumber, invoice.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1234567890");

        var mercadoPagoClient = new Mock<IMercadoPagoClient>();
        mercadoPagoClient.Setup(c => c.CreatePaymentButtonAsync(invoice.InvoiceNumber, invoice.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://mpago.la/test");
        mercadoPagoClient.Setup(c => c.ValidateWebhookSignature(It.IsAny<string>())).Returns(true);

        var service = new PaymentService(
            context,
            pagoFacilClient.Object,
            mercadoPagoClient.Object,
            NullLogger<PaymentService>.Instance);

        var result = await service.PreparePaymentMethodsAsync(invoice.Id, CancellationToken.None);

        Assert.NotNull(result.barcode);
        Assert.NotNull(result.checkoutUrl);
        Assert.Equal(1, await context.Pagos.CountAsync());
    }
}
