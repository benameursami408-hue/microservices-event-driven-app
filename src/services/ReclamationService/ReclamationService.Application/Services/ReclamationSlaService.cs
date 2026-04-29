using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.Services;

public class ReclamationSlaService
{
    public SlaComputation Compute(Reclamation reclamation, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var firstResponseDeadline = reclamation.CreatedAt.Add(GetFirstResponseWindow(reclamation.Priority));
        var planningBase = reclamation.AssignedAt ?? reclamation.CreatedAt;
        var planningDeadline = planningBase.Add(GetPlanningWindow(reclamation.Priority));
        var resolutionBase = reclamation.AssignedAt ?? reclamation.CreatedAt;
        var resolutionDeadline = resolutionBase.Add(GetResolutionWindow(reclamation.Priority));

        if (IsTerminal(reclamation.Status))
        {
            return new SlaComputation(
                firstResponseDeadline,
                planningDeadline,
                resolutionDeadline,
                SlaStatus.Completed,
                reclamation.SlaBreachedAt,
                null,
                null);
        }

        var (target, deadline) = GetActiveTarget(reclamation, firstResponseDeadline, planningDeadline, resolutionDeadline);
        if (!deadline.HasValue)
        {
            return new SlaComputation(
                firstResponseDeadline,
                planningDeadline,
                resolutionDeadline,
                SlaStatus.OnTrack,
                reclamation.SlaBreachedAt,
                null,
                null);
        }

        if (now > deadline.Value)
        {
            return new SlaComputation(
                firstResponseDeadline,
                planningDeadline,
                resolutionDeadline,
                SlaStatus.Breached,
                reclamation.SlaBreachedAt ?? now,
                target,
                deadline);
        }

        var remaining = deadline.Value - now;
        if (remaining <= GetNearBreachWindow(reclamation.Priority))
        {
            return new SlaComputation(
                firstResponseDeadline,
                planningDeadline,
                resolutionDeadline,
                SlaStatus.NearBreach,
                reclamation.SlaBreachedAt,
                target,
                deadline);
        }

        return new SlaComputation(
            firstResponseDeadline,
            planningDeadline,
            resolutionDeadline,
            SlaStatus.OnTrack,
            reclamation.SlaBreachedAt,
            target,
            deadline);
    }

    public void Apply(Reclamation reclamation, SlaComputation computation)
    {
        reclamation.FirstResponseDeadline = computation.FirstResponseDeadline;
        reclamation.PlanningDeadline = computation.PlanningDeadline;
        reclamation.ResolutionDeadline = computation.ResolutionDeadline;
        reclamation.SlaStatus = computation.Status;
        reclamation.SlaBreachedAt = computation.BreachedAt;
    }

    private static bool IsTerminal(ReclamationStatus status)
    {
        return status is ReclamationStatus.Resolved or ReclamationStatus.Closed or ReclamationStatus.Cancelled or ReclamationStatus.Rejected;
    }

    private static (string? target, DateTime? deadline) GetActiveTarget(
        Reclamation reclamation,
        DateTime firstResponseDeadline,
        DateTime planningDeadline,
        DateTime resolutionDeadline)
    {
        return reclamation.Status switch
        {
            ReclamationStatus.Open => ("FIRST_RESPONSE", firstResponseDeadline),
            ReclamationStatus.Assigned => ("PLANNING", planningDeadline),
            ReclamationStatus.Planned => ("RESOLUTION", resolutionDeadline),
            ReclamationStatus.InProgress => ("RESOLUTION", resolutionDeadline),
            _ => (null, null)
        };
    }

    private static TimeSpan GetFirstResponseWindow(NamePriority priority) => priority switch
    {
        NamePriority.URGENT => TimeSpan.FromHours(1),
        NamePriority.HIGH => TimeSpan.FromHours(4),
        NamePriority.MEDUIM => TimeSpan.FromHours(8),
        _ => TimeSpan.FromHours(16)
    };

    private static TimeSpan GetPlanningWindow(NamePriority priority) => priority switch
    {
        NamePriority.URGENT => TimeSpan.FromHours(8),
        NamePriority.HIGH => TimeSpan.FromDays(1),
        NamePriority.MEDUIM => TimeSpan.FromDays(3),
        _ => TimeSpan.FromDays(5)
    };

    private static TimeSpan GetResolutionWindow(NamePriority priority) => priority switch
    {
        NamePriority.URGENT => TimeSpan.FromDays(1),
        NamePriority.HIGH => TimeSpan.FromDays(3),
        NamePriority.MEDUIM => TimeSpan.FromDays(7),
        _ => TimeSpan.FromDays(10)
    };

    private static TimeSpan GetNearBreachWindow(NamePriority priority) => priority switch
    {
        NamePriority.URGENT => TimeSpan.FromMinutes(30),
        NamePriority.HIGH => TimeSpan.FromHours(4),
        NamePriority.MEDUIM => TimeSpan.FromHours(12),
        _ => TimeSpan.FromHours(24)
    };
}

public sealed record SlaComputation(
    DateTime FirstResponseDeadline,
    DateTime PlanningDeadline,
    DateTime ResolutionDeadline,
    SlaStatus Status,
    DateTime? BreachedAt,
    string? ActiveTarget,
    DateTime? ActiveDeadline);
