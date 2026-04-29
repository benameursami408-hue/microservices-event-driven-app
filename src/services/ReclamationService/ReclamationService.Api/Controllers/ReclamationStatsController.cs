using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Services;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/reclamations/stats")]
[Authorize(Roles = "ADMIN")]
public class ReclamationStatsController : ControllerBase
{
    private readonly AdminReclamationStatsService _statsService;

    public ReclamationStatsController(AdminReclamationStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet]
    public async Task<ActionResult<ReclamationStatsDto>> Get([FromQuery] int days = 14, [FromQuery] int latest = 8)
    {
        var result = await _statsService.GetStatsAsync(days, latest);
        return Ok(result);
    }
}
