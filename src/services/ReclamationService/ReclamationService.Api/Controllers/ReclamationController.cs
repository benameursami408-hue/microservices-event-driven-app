using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Security;
using ReclamationService.Application.Services;
using ReclamationService.Api.Infrastructure;
using ReclamationService.Domain.Enums;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReclamationsController : ControllerBase
{
    private readonly ReclamationsService _reclamationService;
    private readonly AiPriorityService _aiPriorityService;

    public ReclamationsController(ReclamationsService reclamationService, AiPriorityService aiPriorityService)
    {
        _reclamationService = reclamationService;
        _aiPriorityService = aiPriorityService;
    }

    [HttpGet]
    public ActionResult<List<ReclamationDto>> GetAll(
        [FromQuery] ReclamationStatus? status = null,
        [FromQuery] TicketCategory? category = null)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.GetVisible(actor, status, category));
    }

    [HttpGet("query")]
    public ActionResult<PagedResult<ReclamationDto>> Query(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReclamationStatus? status = null,
        [FromQuery] TicketCategory? category = null,
        [FromQuery] NamePriority? priority = null,
        [FromQuery] string? search = null)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.QueryVisible(actor, status, category, priority, search, page, pageSize));
    }

    [HttpPost("query")]
    public ActionResult<PagedResult<ReclamationDto>> QueryByBody([FromBody] ReclamationQueryRequestDto request)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.QueryVisible(
            actor,
            request.Status,
            request.Category,
            request.Priority,
            request.Search,
            request.Page,
            request.PageSize));
    }

    [HttpGet("{id}")]
    public ActionResult<ReclamationDto> GetById(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var reclamation = _reclamationService.GetById(id, actor);
        return Ok(reclamation);
    }

    [HttpGet("priority/{priority}")]
    public ActionResult<List<ReclamationDto>> GetByPriority(NamePriority priority)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.GetByPriority(priority, actor));
    }

    [HttpGet("category/{category}")]
    public ActionResult<List<ReclamationDto>> GetByCategory(TicketCategory category)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.GetByCategory(category, actor));
    }

    [HttpGet("reference/{reference}")]
    public ActionResult<ReclamationDto> GetByReference(string reference)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var reclamation = _reclamationService.GetByReference(reference, actor);
        return Ok(reclamation);
    }

    [HttpPost]
    public async Task<ActionResult<ReclamationDto>> Create([FromBody] CreateReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var created = await _reclamationService.CreateAsync(dto, actor);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public ActionResult<ReclamationDto> Update(long id, [FromBody] UpdateReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = _reclamationService.Update(id, dto, actor);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        _reclamationService.Delete(id, actor);
        return NoContent();
    }

    [HttpGet("{id}/history")]
    public ActionResult<List<ReclamationHistoryDto>> GetHistory(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.GetHistory(id, actor));
    }

    [HttpGet("{id}/priority")]
    public async Task<ActionResult<ReclamationPriorityDto>> GetPriority(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _reclamationService.GetPriorityAsync(id, actor));
    }

    [HttpGet("{id}/sla")]
    public async Task<ActionResult<ReclamationSlaDto>> GetSla(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _reclamationService.GetSlaAsync(id, actor));
    }


    [HttpGet("{id}/ai-priority-analysis/latest")]
    public async Task<ActionResult<AiPriorityAnalysisDto?>> GetLatestAiPriorityAnalysis(long id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        _ = _reclamationService.GetById(id, actor);
        var analysis = await _aiPriorityService.GetLatestAsync(id, cancellationToken);
        return analysis is null ? NoContent() : Ok(analysis);
    }

    [HttpPost("{id}/ai-priority-analysis/{analysisId:long}/apply")]
    public async Task<ActionResult<object>> ApplyAiPriorityAnalysis(long id, long analysisId, [FromBody] ApplyAiPriorityDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return await ApplyAiPriorityAnalysisInternalAsync(id, analysisId, dto, actor, cancellationToken);
    }

    [HttpPost("{id}/ai-priority/apply")]
    public async Task<ActionResult<object>> ApplyAiPrioritySuggestion(long id, [FromBody] ApplyAiPriorityDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        AiPriorityAnalysisDto? analysis;

        if (dto.AnalysisId.HasValue)
        {
            analysis = await _aiPriorityService.GetByIdAsync(dto.AnalysisId.Value, cancellationToken);
        }
        else
        {
            analysis = await _aiPriorityService.GetLatestAsync(id, cancellationToken);
        }

        if (analysis is null || analysis.ReclamationId != id || analysis.AnalysisId is null)
        {
            return NotFound();
        }

        return await ApplyAiPriorityAnalysisInternalAsync(id, analysis.AnalysisId.Value, dto, actor, cancellationToken);
    }

    private async Task<ActionResult<object>> ApplyAiPriorityAnalysisInternalAsync(long id, long analysisId, ApplyAiPriorityDto dto, CurrentUser actor, CancellationToken cancellationToken)
    {
        var analysis = await _aiPriorityService.GetByIdAsync(analysisId, cancellationToken);
        if (analysis is null || analysis.ReclamationId != id) return NotFound();

        var updatedPriority = await _reclamationService.ApplyAiPrioritySuggestionAsync(id, analysis, dto.Reason, actor);
        var accepted = await _aiPriorityService.MarkAcceptedAsync(id, analysisId, actor.UserId, cancellationToken);
        return Ok(new { priority = updatedPriority, analysis = accepted });
    }

    [HttpPost("{id}/recalculate-priority")]
    public async Task<ActionResult<ReclamationPriorityDto>> RecalculatePriority(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _reclamationService.RecalculatePriorityAsync(id, actor));
    }

    [HttpPost("{id}/override-priority")]
    public async Task<ActionResult<ReclamationPriorityDto>> OverridePriority(long id, [FromBody] OverridePriorityDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _reclamationService.OverridePriorityAsync(id, dto, actor));
    }

    [HttpPatch("{id}/assign")]
    public async Task<ActionResult<ReclamationDto>> Assign(long id, [FromBody] AssignReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.AssignAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/plan")]
    public async Task<ActionResult<ReclamationDto>> Plan(long id, [FromBody] PlanReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.PlanAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/request-planning")]
    public async Task<ActionResult<ReclamationDto>> RequestPlanning(long id, [FromBody] RequestPlanningDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.RequestPlanningAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/start")]
    public async Task<ActionResult<ReclamationDto>> Start(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.StartAsync(id, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/resolve")]
    public async Task<ActionResult<ReclamationDto>> Resolve(long id, [FromBody] ResolveReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.ResolveAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/close")]
    public async Task<ActionResult<ReclamationDto>> Close(long id, [FromBody] CloseReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.CloseAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/cancel")]
    public async Task<ActionResult<ReclamationDto>> Cancel(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.CancelAsync(id, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/reject")]
    public async Task<ActionResult<ReclamationDto>> Reject(long id, [FromBody] RejectReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.RejectAsync(id, dto, actor);
        return Ok(updated);
    }
}
