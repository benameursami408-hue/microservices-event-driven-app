using InterventionService.Application.DTOs;
using InterventionService.Application.Outbox;
using InterventionService.Application.Security;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;
using SharedEvents.Events;

namespace InterventionService.Application.Services;
public partial class PlanningService
{
    private static DateTime EnsureUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc) return dt;
        if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime();
        return DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();
    }

    private static PlanningRequestDto ToDto(PlanningRequest item) => new()
    {
        Id = item.Id,
        ReclamationId = item.ReclamationId,
        Reference = item.Reference,
        SavId = item.SavId,
        SavName = item.SavName,
        Priority = item.Priority,
        ClientId = item.ClientId,
        CustomerName = item.CustomerName,
        CustomerEmail = item.CustomerEmail,
        CustomerPhone = item.CustomerPhone,
        ServiceAddress = item.ServiceAddress,
        Status = item.Status,
        RequestedAt = item.RequestedAt
    };

    private static AppointmentDto ToDto(Appointment item, CurrentUser actor) => new()
    {
        Id = item.Id,
        PlanningRequestId = item.PlanningRequestId,
        ReclamationId = item.ReclamationId,
        Reference = item.Reference,
        StartAt = item.StartAt,
        EndAt = item.EndAt,
        EstimatedDurationMinutes = item.EstimatedDurationMinutes,
        TimeZone = item.TimeZone,
        TechnicianId = item.TechnicianId,
        TechnicianName = item.TechnicianName,
        CustomerPresenceRequired = item.CustomerPresenceRequired,
        Status = item.Status,
        Sequence = item.Sequence,
        PlanningNote = item.PlanningNote,
        ScheduleWarnings = new List<string>(),
        AllowedActions = PlanningActionPolicy.GetAllowedActions(actor, item)
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

    private static DateTime ResolveEndAt(DateTime startAt, DateTime? endAt, int estimatedDurationMinutes)
    {
        var effectiveEnd = endAt.HasValue ? EnsureUtc(endAt.Value) : startAt.AddMinutes(estimatedDurationMinutes);
        if (effectiveEnd <= startAt)
        {
            throw new InvalidOperationException("Appointment end time must be after start time.");
        }

        return effectiveEnd;
    }

    private static void EnsureTechnicianAccess(CurrentUser actor, long technicianId)
    {
        var role = NormalizeRole(actor.Role);
        if (role == "ADMIN" || role == "SAV")
        {
            return;
        }

        if (role == "ST" && actor.UserId == technicianId)
        {
            return;
        }

        throw new UnauthorizedAccessException();
    }

    private async Task PublishConflictAndThrowAsync(
        Appointment appointment,
        long technicianId,
        string technicianName,
        ScheduleEvaluation evaluation,
        CurrentUser actor,
        CancellationToken cancellationToken)
    {
        var message = string.Join(" ", evaluation.Conflicts);
        await _outboxWriter.EnqueueAsync(new PlanningConflictDetectedEvent
        {
            CorrelationId = actor.CorrelationId,
            AppointmentId = appointment.Id,
            ReclamationId = appointment.ReclamationId,
            Reference = appointment.Reference,
            TechnicianId = technicianId,
            TechnicianName = technicianName,
            ConflictType = "SCHEDULING_CONFLICT",
            Message = message,
            AttemptedStartAt = appointment.StartAt,
            AttemptedEndAt = appointment.EndAt,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        throw new InvalidOperationException(message);
    }
}
