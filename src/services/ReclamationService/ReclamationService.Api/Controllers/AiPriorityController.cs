using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Services;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/ai/reclamations")]
[Authorize]
public class AiPriorityController : ControllerBase
{
    private readonly AiPriorityService _aiPriorityService;

    public AiPriorityController(AiPriorityService aiPriorityService)
    {
        _aiPriorityService = aiPriorityService;
    }

    [HttpPost("analyze-priority")]
    public async Task<ActionResult<AiPriorityAnalysisDto>> AnalyzePriority([FromBody] AnalyzePriorityRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _aiPriorityService.AnalyzeAsync(request, cancellationToken));
    }
}
