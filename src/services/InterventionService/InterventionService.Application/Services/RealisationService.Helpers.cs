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

        EnsureRole(actor, "ST", "TECHNICIAN", "ADMIN");
        if (IsTechnicianRole(actor.Role) && intervention.TechnicianId != actor.UserId)
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
        ClientId = item.ClientId,
        ClientName = item.Appointment?.PlanningRequest?.CustomerName ?? string.Empty,
        Reference = item.Reference,
        Priority = item.Appointment?.PlanningRequest?.Priority,
        ServiceAddress = item.Appointment?.PlanningRequest?.ServiceAddress,
        ProductName = item.Appointment?.PlanningRequest?.ProductName,
        Brand = item.Appointment?.PlanningRequest?.Brand,
        Model = item.Appointment?.PlanningRequest?.Model,
        SerialNumber = item.Appointment?.PlanningRequest?.SerialNumber,
        ScheduledAt = item.Appointment?.StartAt,
        Description = item.Appointment?.PlanningRequest?.ProductName is { Length: > 0 } productName
            ? $"Intervention planifiee pour {productName}."
            : "Intervention SAV assignee.",
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
        if (!roles.Any(role => RoleMatches(actor.Role, role)))
        {
            throw new UnauthorizedAccessException();
        }
    }

    private static bool RoleMatches(string currentRole, string expectedRole)
    {
        var current = NormalizeRole(currentRole);
        var expected = NormalizeRole(expectedRole);
        if (expected == "ST" || expected == "TECHNICIAN")
        {
            return current is "ST" or "TECHNICIAN";
        }

        return current == expected;
    }

    private static bool IsTechnicianRole(string role) => NormalizeRole(role) is "ST" or "TECHNICIAN";

    private static string NormalizeRole(string role) => (role ?? string.Empty).Trim().Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
}
