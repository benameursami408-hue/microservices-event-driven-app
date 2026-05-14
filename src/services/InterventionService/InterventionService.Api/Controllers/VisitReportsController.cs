using InterventionService.Api.Infrastructure;
using InterventionService.Application.DTOs;
using InterventionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterventionService.Api.Controllers;

[ApiController]
[Route("api/visit-reports")]
[Authorize]
public class VisitReportsController : ControllerBase
{
    private readonly VisitReportsService _service;
    public VisitReportsController(VisitReportsService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "SAV,ADMIN,ST,TECHNICIAN")]
    public async Task<ActionResult<List<VisitReportDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _service.QueryAsync(User.ToCurrentUser(HttpContext), cancellationToken));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<VisitReportDto>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _service.QueryAsync(User.ToCurrentUser(HttpContext), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VisitReportDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var report = await _service.GetAsync(id, User.ToCurrentUser(HttpContext), cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<VisitReportDto>> Update(Guid id, [FromBody] UpdateVisitReportDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateAsync(id, dto, User.ToCurrentUser(HttpContext), cancellationToken));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<VisitReportDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.PublishAsync(id, User.ToCurrentUser(HttpContext), cancellationToken));
    }
}
