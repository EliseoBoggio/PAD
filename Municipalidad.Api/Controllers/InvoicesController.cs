using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Municipalidad.Api.Infrastructure.Persistence;
using Municipalidad.Api.Services;

namespace Municipalidad.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly BillingService _billingService;

    public InvoicesController(MunicipalidadDbContext dbContext, BillingService billingService)
    {
        _dbContext = dbContext;
        _billingService = billingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken)
    {
        var invoices = await _dbContext.Facturas.Include(f => f.Tributos).ToListAsync(cancellationToken);
        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.Facturas.Include(f => f.Tributos).Include(f => f.Payments).SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("generate/{tributoId:guid}")]
    public async Task<IActionResult> GenerateInvoice(Guid tributoId, CancellationToken cancellationToken)
    {
        var invoice = await _billingService.GenerateInvoiceAsync(tributoId, cancellationToken);
        return Ok(invoice);
    }
}
