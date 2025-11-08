using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Municipalidad.Api.Infrastructure.Persistence;
using Municipalidad.Api.Services;

namespace Municipalidad.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly PaymentService _paymentService;

    public PaymentsController(MunicipalidadDbContext dbContext, PaymentService paymentService)
    {
        _dbContext = dbContext;
        _paymentService = paymentService;
    }

    [HttpPost("prepare/{invoiceId:guid}")]
    public async Task<IActionResult> PreparePayment(Guid invoiceId, CancellationToken cancellationToken)
    {
        var methods = await _paymentService.PreparePaymentMethodsAsync(invoiceId, cancellationToken);
        return Ok(new { methods.barcode, methods.checkoutUrl });
    }

    [AllowAnonymous]
    [HttpPost("mercadopago/callback")]
    public async Task<IActionResult> MercadoPagoCallback([FromHeader(Name = "x-signature")] string signature, [FromBody] MercadoPagoCallbackRequest request, CancellationToken cancellationToken)
    {
        var processed = await _paymentService.ProcessMercadoPagoCallbackAsync(signature, request.ExternalReference, cancellationToken);
        return processed ? Ok() : BadRequest();
    }

    [HttpGet("invoice/{invoiceId:guid}")]
    public async Task<IActionResult> GetPayments(Guid invoiceId, CancellationToken cancellationToken)
    {
        var payments = await _dbContext.Pagos.Where(p => p.InvoiceId == invoiceId).ToListAsync(cancellationToken);
        return Ok(payments);
    }

    public sealed record MercadoPagoCallbackRequest(string ExternalReference);
}
