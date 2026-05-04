using InterventionService.Application.DTOs;
using InterventionService.Application.Outbox;
using InterventionService.Application.Security;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;
using SharedEvents.Events;

namespace InterventionService.Application.Services;
public partial class RealisationService
{
    private async Task<Intervention> GetOwnedAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken)
    {
        var intervention = await _interventionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Intervention not found.");

        EnsureRole(actor, "ST", "ADMIN");
        if (NormalizeRole(actor.Role) == "ST" && intervention.TechnicianId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        return intervention;
    }

    private static InterventionDto ToDto(Intervention item, CurrentUser actor) => new()
    {
        Id = item.Id,
        AppointmentId = item.AppointmentId,
        ReclamationId = item.ReclamationId,
        Reference = item.Reference,
        TechnicianId = item.TechnicianId,
        TechnicianName = item.TechnicianName,
        StartedAt = item.StartedAt,
        EndedAt = item.EndedAt,
        Status = item.Status,
        Outcome = item.Outcome,
        NeedsReplanning = item.NeedsReplanning,
        LatestReportSummary = item.VisitReports.OrderByDescending(r => r.CreatedAt).Select(r => r.Summary).FirstOrDefault(),
        AllowedActions = RealisationActionPolicy.GetAllowedActions(actor, item)
    };

    private static void EnsureRole(CurrentUser actor, params string[] roles)
    {
        var current = NormalizeRole(actor.Role);
        if (!roles.Any(x => NormalizeRole(x) == current))
        {
            throw new UnauthorizedAccessException();
        }
    }

    private static string NormalizeRole(string role) => (role ?? string.Empty).Trim().ToUpperInvariant();
}
