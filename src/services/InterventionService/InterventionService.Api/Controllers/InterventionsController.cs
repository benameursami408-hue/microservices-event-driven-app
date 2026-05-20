using InterventionService.Api.Infrastructure;
using InterventionService.Application.DTOs;
using InterventionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterventionService.Api.Controllers;

[ApiController]
[Route("api/interventions")]
[Authorize]
public class InterventionsController : ControllerBase
{
    private readonly RealisationService _realisationService;
    private readonly AdminInterventionStatsService _adminStatsService;

    public InterventionsController(RealisationService realisationService, AdminInterventionStatsService adminStatsService)
    {
        _realisationService = realisationService;
        _adminStatsService = adminStatsService;
    }

    [HttpGet]
    [Authorize(Roles = "SAV,ADMIN")]
    public async Task<ActionResult<List<InterventionDto>>> GetAll([FromQuery] long? reclamationId, [FromQuery] long? technicianId, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.QueryInterventionsAsync(actor, reclamationId, technicianId, cancellationToken));
    }

    [HttpGet("admin/statistics")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<GlobalInterventionStatsDto>> GetAdminStatistics()
    {
        return Ok(await _adminStatsService.GetGlobalStatisticsAsync());
    }

    [HttpGet("admin/statistics/technicians")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<List<TechnicianStatsDto>>> GetTechnicianStatistics()
    {
        return Ok(await _adminStatsService.GetTechnicianStatisticsAsync());
    }

    [HttpGet("my")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<List<InterventionDto>>> GetMy(CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.QueryMyInterventionsAsync(actor, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ST,TECHNICIAN,SAV,ADMIN")]
    public async Task<ActionResult<InterventionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var item = await _realisationService.GetInterventionAsync(id, actor, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<InterventionDto>> UpdateStatus(Guid id, [FromBody] UpdateInterventionStatusDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.UpdateStatusAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<InterventionDto>> Start(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.StartAsync(id, actor, cancellationToken));
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<InterventionDto>> Complete(Guid id, [FromBody] CompleteInterventionDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.CompleteAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("{id:guid}/diagnostic")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddDiagnostic(Guid id, [FromBody] RecordDiagnosticDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddDiagnosticAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("{id:guid}/repair-actions")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddRepairAction(Guid id, [FromBody] AddRepairActionDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddRepairActionAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("{id:guid}/parts-used")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddPart(Guid id, [FromBody] AddPartUsedDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddPartAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("{id:guid}/evidences")]
    [Authorize(Roles = "ST,TECHNICIAN,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddEvidence(Guid id, [FromBody] AddEvidenceDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddEvidenceAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("{id:guid}/publish-report")]
    [Authorize(Roles = "ST,TECHNICIAN,SAV,ADMIN")]
    public async Task<ActionResult<InterventionDto>> PublishReport(Guid id, [FromBody] PublishVisitReportDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.PublishReportAsync(id, dto, actor, cancellationToken));
    }
}
