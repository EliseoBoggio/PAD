using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Municipalidad.Api.Infrastructure.Persistence;
using Municipalidad.Api.Services;

namespace Municipalidad.Api.Controllers;

[ApiController]
[Route("api/taxes")]
[Authorize]
public class TaxesController : ControllerBase
{
    private readonly MunicipalidadDbContext _dbContext;
    private readonly TaxAssessmentService _taxAssessmentService;

    public TaxesController(MunicipalidadDbContext dbContext, TaxAssessmentService taxAssessmentService)
    {
        _dbContext = dbContext;
        _taxAssessmentService = taxAssessmentService;
    }

    [HttpPost("assess")]
    public async Task<IActionResult> AssessTax([FromBody] AssessTaxRequest request, CancellationToken cancellationToken)
    {
        var period = new DateOnly(request.Year, request.Month, 1);
        var tributo = await _taxAssessmentService.AssessMonthlyTaxAsync(request.VehicleId, period, cancellationToken);
        return Ok(tributo);
    }

    [HttpGet("vehicle/{vehicleId:guid}")]
    public async Task<IActionResult> GetVehicleTaxes(Guid vehicleId, CancellationToken cancellationToken)
    {
        var tributos = await _dbContext.TributosMensuales.Where(t => t.VehicleId == vehicleId).ToListAsync(cancellationToken);
        return Ok(tributos);
    }

    public sealed record AssessTaxRequest(Guid VehicleId, int Year, int Month);
}
