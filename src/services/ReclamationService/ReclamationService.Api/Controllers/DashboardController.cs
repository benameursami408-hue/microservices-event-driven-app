using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReclamationService.Application.Services;
using System.Security.Claims;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "ADMIN,SAV")]
public class DashboardController : ControllerBase
{
    private readonly AdminReclamationStatsService _statsService;

    public DashboardController(AdminReclamationStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary([FromQuery] int days = 14, [FromQuery] int latest = 8)
    {
        var reclamations = await _statsService.GetStatsAsync(days, latest);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
        if (!string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            reclamations.WorkloadBySav = new();
        }

        var openReclamations = reclamations.Kpis.Open + reclamations.Kpis.Assigned + reclamations.Kpis.Planned + reclamations.Kpis.InProgress;
        var highRiskPriorities = reclamations.ByPriority
            .Where(x => string.Equals(x.Priority.ToString(), "HIGH", StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Priority.ToString(), "URGENT", StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Count);

        return Ok(new
        {
            generatedAt = DateTime.UtcNow,
            openReclamations,
            plannedVisits = reclamations.Kpis.Planned,
            activeInterventions = reclamations.Kpis.InProgress,
            slaRisk = highRiskPriorities,
            recentReclamations = reclamations.Latest,
            upcomingAppointments = Array.Empty<object>(),
            latestNotifications = Array.Empty<object>(),
            statusDistribution = reclamations.ByStatus,
            reclamations
        });
    }
}
