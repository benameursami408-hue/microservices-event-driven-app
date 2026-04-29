using InterventionService.Api.Infrastructure;
using InterventionService.Application.DTOs;
using InterventionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterventionService.Api.Controllers;

[ApiController]
[Route("api/realisations")]
[Authorize]
public class RealisationsController : ControllerBase
{
    private readonly RealisationService _realisationService;

    public RealisationsController(RealisationService realisationService)
    {
        _realisationService = realisationService;
    }

    [HttpGet("interventions")]
    public async Task<ActionResult<List<InterventionDto>>> GetInterventions([FromQuery] long? reclamationId, [FromQuery] long? technicianId, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.QueryInterventionsAsync(actor, reclamationId, technicianId, cancellationToken));
    }

    [HttpGet("interventions/{id:guid}")]
    public async Task<ActionResult<InterventionDto>> GetIntervention(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var item = await _realisationService.GetInterventionAsync(id, actor, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("interventions/by-reclamation/{reclamationId:long}")]
    public async Task<ActionResult<List<InterventionDto>>> GetByReclamation(long reclamationId, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.QueryInterventionsAsync(actor, reclamationId: reclamationId, cancellationToken: cancellationToken));
    }

    [HttpGet("interventions/mine")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<List<InterventionDto>>> GetMine(CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.QueryInterventionsAsync(actor, technicianId: actor.UserId, cancellationToken: cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/start")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> Start(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.StartAsync(id, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/pause")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> Pause(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.PauseAsync(id, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/diagnostic")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddDiagnostic(Guid id, [FromBody] RecordDiagnosticDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddDiagnosticAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/repair-actions")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddRepairAction(Guid id, [FromBody] AddRepairActionDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddRepairActionAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/parts-used")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddPart(Guid id, [FromBody] AddPartUsedDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddPartAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/evidences")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> AddEvidence(Guid id, [FromBody] AddEvidenceDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.AddEvidenceAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/complete")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> Complete(Guid id, [FromBody] CompleteInterventionDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.CompleteAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/publish-report")]
    [Authorize(Roles = "ST,SAV,ADMIN")]
    public async Task<ActionResult<InterventionDto>> PublishReport(Guid id, [FromBody] PublishVisitReportDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.PublishReportAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("interventions/{id:guid}/request-replanning")]
    [Authorize(Roles = "ST,ADMIN")]
    public async Task<ActionResult<InterventionDto>> RequestReplanning(Guid id, [FromBody] RequestReplanningDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _realisationService.RequestReplanningAsync(id, dto, actor, cancellationToken));
    }
}
