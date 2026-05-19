using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReclamationService.Api.Infrastructure;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Services;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/ai/reclamations")]
[Authorize]
public class AiPriorityController : ControllerBase
{
    private readonly AiPriorityService _aiPriorityService;
    private readonly ReclamationsService _reclamationsService;

    public AiPriorityController(AiPriorityService aiPriorityService, ReclamationsService reclamationsService)
    {
        _aiPriorityService = aiPriorityService;
        _reclamationsService = reclamationsService;
    }

    [HttpPost("analyze-priority")]
    public async Task<ActionResult<AiPriorityAnalysisDto>> AnalyzePriority([FromBody] AnalyzePriorityRequestDto request, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        _reclamationsService.EnsureCanWorkOnReclamation(request.ReclamationId, actor);
        return Ok(await _aiPriorityService.AnalyzeAsync(request, cancellationToken));
    }
}
