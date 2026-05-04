using ReclamationService.Application.Outbox;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Application.Mappers;
using ReclamationService.Application.Security;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;
using SharedEvents.Events;
using System.Text.Json;

namespace ReclamationService.Application.Services;

public partial class ReclamationsService
{
    private Reclamation GetByIdInternal(long id)
    {
        var reclamation = _reclamationRepository.GetById(id);
        if (reclamation == null)
        {
            throw new NotFoundException($"Reclamation with id {id} not found.");
        }

        return reclamation;
    }

    private Reclamation GetByIdVisible(long id, CurrentUser actor)
    {
        var reclamation = GetByIdInternal(id);
        EnsureCanView(actor, reclamation);
        return reclamation;
    }

    private void EnsureCanView(CurrentUser actor, Reclamation reclamation)
    {
        var role = NormalizeRole(actor.Role);

        if (role == "ADMIN")
        {
            return;
        }

        if (role == "CLIENT")
        {
            if (reclamation.ClientId != actor.UserId)
            {
                throw new UnauthorizedAccessException();
            }

            return;
        }

        if (role == "SAV")
        {
            var isBacklog = reclamation.Status == ReclamationStatus.Open;
            var isMine = reclamation.SAVId == actor.UserId;
            if (!isBacklog && !isMine)
            {
                throw new UnauthorizedAccessException();
            }

            return;
        }

        if (role == "ST")
        {
            if (reclamation.TechnicianId != actor.UserId)
            {
                throw new UnauthorizedAccessException();
            }

            return;
        }

        throw new UnauthorizedAccessException();
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
        {
            return dt;
        }

        if (dt.Kind == DateTimeKind.Local)
        {
            return dt.ToUniversalTime();
        }

        // Unspecified: assume local and convert to UTC for storage.
        return DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();
    }

    private static string NormalizeRole(string role)
    {
        return (role ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static void EnsureRole(CurrentUser actor, params string[] allowedRoles)
    {
        var role = NormalizeRole(actor.Role);
        if (allowedRoles.Any(r => NormalizeRole(r) == role))
        {
            return;
        }

        throw new UnauthorizedAccessException();
    }

    private static string ToStatusCode(ReclamationStatus status)
    {
        return status.ToString().ToUpperInvariant();
    }

    private static ReclamationDto ToDtoWithActions(Reclamation item, CurrentUser actor)
    {
        var dto = item.ToDto();
        dto.AllowedActions = ReclamationActionPolicy.GetAllowedActions(actor, item);
        return dto;
    }

    private async Task QueueStatusChangedAsync(
        Reclamation reclamation,
        ReclamationStatus from,
        ReclamationStatus to,
        CurrentUser actor,
        string? comment)
    {
        var clientEmail = _clientRepository.GetById(reclamation.ClientId)?.Email ?? string.Empty;

        await _outboxWriter.EnqueueAsync(new ReclamationStatusChangedEvent
        {
            CorrelationId = actor.CorrelationId,
            ReclamationId = reclamation.Id,
            Reference = reclamation.Reference,
            ClientId = reclamation.ClientId,
            ClientEmail = clientEmail,
            FromStatus = ToStatusCode(from),
            ToStatus = ToStatusCode(to),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment,
            ActorUserId = actor.UserId,
            ActorRole = NormalizeRole(actor.Role),
            OccurredAt = DateTime.UtcNow
        });
    }

    private void ApplyDerivedState(Reclamation reclamation)
    {
        var beforeDerived = CaptureDerivedState(reclamation);

        var classification = _classificationService.Compute(reclamation);
        reclamation.Category = classification.Category;
        reclamation.CategoryReason = classification.Reason;

        var sla = _slaService.Compute(reclamation);
        _slaService.Apply(reclamation, sla);

        var auto = _priorityService.Compute(reclamation);
        if (reclamation.ManualPriorityOverride)
        {
            var reasons = auto.Reasons.ToList();
            if (!string.IsNullOrWhiteSpace(reclamation.ManualPriorityOverrideReason))
            {
                reasons.Add($"Override manuel: {reclamation.ManualPriorityOverrideReason}");
            }

            reclamation.PriorityScore = auto.Score;
            reclamation.PriorityReasons = SerializeReasons(reasons);
            reclamation.PrioritySource = PrioritySource.ManualOverride;
            reclamation.PriorityUpdatedAt ??= DateTime.UtcNow;
        }
        else
        {
            reclamation.Priority = auto.Level;
            reclamation.PriorityScore = auto.Score;
            reclamation.PriorityReasons = SerializeReasons(auto.Reasons);
            reclamation.PrioritySource = PrioritySource.Rules;
        }

        var priorityChanged = beforeDerived.Priority != reclamation.Priority
            || beforeDerived.PriorityScore != reclamation.PriorityScore
            || beforeDerived.PrioritySource != reclamation.PrioritySource
            || !string.Equals(beforeDerived.PriorityReasons, reclamation.PriorityReasons, StringComparison.Ordinal);

        var categoryChanged = beforeDerived.Category != reclamation.Category
            || !string.Equals(beforeDerived.CategoryReason, reclamation.CategoryReason, StringComparison.Ordinal);

        var derivedChanged = categoryChanged
            || priorityChanged
            || beforeDerived.FirstResponseDeadline != reclamation.FirstResponseDeadline
            || beforeDerived.PlanningDeadline != reclamation.PlanningDeadline
            || beforeDerived.ResolutionDeadline != reclamation.ResolutionDeadline
            || beforeDerived.SlaStatus != reclamation.SlaStatus
            || beforeDerived.SlaBreachedAt != reclamation.SlaBreachedAt;

        if (categoryChanged)
        {
            reclamation.CategoryUpdatedAt = DateTime.UtcNow;
        }

        if (priorityChanged)
        {
            reclamation.PriorityUpdatedAt = DateTime.UtcNow;
        }

        if (derivedChanged)
        {
            reclamation.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task QueueOperationalEventsAsync(
        Reclamation reclamation,
        OperationalState before,
        string correlationId,
        long actorUserId,
        string actorRole)
    {
        var categoryChanged = before.Category != reclamation.Category
            || !string.Equals(before.CategoryReason, reclamation.CategoryReason, StringComparison.Ordinal);
        var reasonsChanged = !string.Equals(before.PriorityReasons, reclamation.PriorityReasons, StringComparison.Ordinal);
        var priorityChanged = before.Priority != reclamation.Priority
            || before.PriorityScore != reclamation.PriorityScore
            || before.PrioritySource != reclamation.PrioritySource
            || reasonsChanged;

        if (categoryChanged)
        {
            _historyRepository.Add(new ReclamationHistory
            {
                ReclamationId = reclamation.Id,
                FromStatus = reclamation.Status,
                ToStatus = reclamation.Status,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Comment = $"Category classified as {reclamation.Category}.",
                OccurredAt = DateTime.UtcNow
            });

            await _outboxWriter.EnqueueAsync(new ReclamationClassifiedEvent
            {
                CorrelationId = correlationId,
                ReclamationId = reclamation.Id,
                Reference = reclamation.Reference,
                Category = reclamation.Category.ToString().ToUpperInvariant(),
                Reason = reclamation.CategoryReason,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                OccurredAt = DateTime.UtcNow
            });
        }

        if (priorityChanged)
        {
            _historyRepository.Add(new ReclamationHistory
            {
                ReclamationId = reclamation.Id,
                FromStatus = reclamation.Status,
                ToStatus = reclamation.Status,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Comment = $"Priority updated to {reclamation.Priority} (score {reclamation.PriorityScore}).",
                OccurredAt = DateTime.UtcNow
            });

            await _outboxWriter.EnqueueAsync(new ReclamationPriorityUpdatedEvent
            {
                CorrelationId = correlationId,
                ReclamationId = reclamation.Id,
                Reference = reclamation.Reference,
                Priority = reclamation.Priority.ToString().ToUpperInvariant(),
                Severity = reclamation.Severity.ToString().ToUpperInvariant(),
                PriorityScore = reclamation.PriorityScore,
                PrioritySource = reclamation.PrioritySource.ToString().ToUpperInvariant(),
                Reasons = DeserializeReasons(reclamation.PriorityReasons),
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                OccurredAt = DateTime.UtcNow
            });
        }

        var clientEmail = _clientRepository.GetById(reclamation.ClientId)?.Email ?? string.Empty;
        var slaTarget = GetActiveSlaTarget(reclamation);
        var slaDeadline = GetActiveSlaDeadline(reclamation);

        if (before.SlaStatus != reclamation.SlaStatus && reclamation.SlaStatus == SlaStatus.NearBreach && slaTarget is not null && slaDeadline.HasValue)
        {
            _historyRepository.Add(new ReclamationHistory
            {
                ReclamationId = reclamation.Id,
                FromStatus = reclamation.Status,
                ToStatus = reclamation.Status,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Comment = $"SLA near breach on {slaTarget} ({slaDeadline.Value:u}).",
                OccurredAt = DateTime.UtcNow
            });

            await _outboxWriter.EnqueueAsync(new SlaNearBreachDetectedEvent
            {
                CorrelationId = correlationId,
                ReclamationId = reclamation.Id,
                Reference = reclamation.Reference,
                ClientId = reclamation.ClientId,
                ClientEmail = clientEmail,
                SavId = reclamation.SAVId,
                SavName = reclamation.SAVName,
                Priority = reclamation.Priority.ToString().ToUpperInvariant(),
                SlaTarget = slaTarget,
                DeadlineAt = slaDeadline.Value,
                OccurredAt = DateTime.UtcNow
            });
        }

        if (before.SlaStatus != reclamation.SlaStatus && reclamation.SlaStatus == SlaStatus.Breached && slaTarget is not null && slaDeadline.HasValue)
        {
            _historyRepository.Add(new ReclamationHistory
            {
                ReclamationId = reclamation.Id,
                FromStatus = reclamation.Status,
                ToStatus = reclamation.Status,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Comment = $"SLA breached on {slaTarget} ({slaDeadline.Value:u}).",
                OccurredAt = DateTime.UtcNow
            });

            await _outboxWriter.EnqueueAsync(new SlaBreachedEvent
            {
                CorrelationId = correlationId,
                ReclamationId = reclamation.Id,
                Reference = reclamation.Reference,
                ClientId = reclamation.ClientId,
                ClientEmail = clientEmail,
                SavId = reclamation.SAVId,
                SavName = reclamation.SAVName,
                Priority = reclamation.Priority.ToString().ToUpperInvariant(),
                SlaTarget = slaTarget,
                DeadlineAt = slaDeadline.Value,
                BreachedAt = reclamation.SlaBreachedAt ?? DateTime.UtcNow,
                OccurredAt = DateTime.UtcNow
            });
        }
    }

    private static OperationalState CaptureOperationalState(Reclamation reclamation)
    {
        return new OperationalState(
            reclamation.Category,
            reclamation.CategoryReason,
            reclamation.Priority,
            reclamation.PriorityScore,
            reclamation.PrioritySource,
            reclamation.PriorityReasons,
            reclamation.SlaStatus);
    }

    private static DerivedState CaptureDerivedState(Reclamation reclamation)
    {
        return new DerivedState(
            reclamation.Category,
            reclamation.CategoryReason,
            reclamation.Priority,
            reclamation.PriorityScore,
            reclamation.PrioritySource,
            reclamation.PriorityReasons,
            reclamation.FirstResponseDeadline,
            reclamation.PlanningDeadline,
            reclamation.ResolutionDeadline,
            reclamation.SlaStatus,
            reclamation.SlaBreachedAt);
    }

    private static bool HasDerivedStateChanged(DerivedState before, Reclamation reclamation)
    {
        return before.Category != reclamation.Category
            || !string.Equals(before.CategoryReason, reclamation.CategoryReason, StringComparison.Ordinal)
            || before.Priority != reclamation.Priority
            || before.PriorityScore != reclamation.PriorityScore
            || before.PrioritySource != reclamation.PrioritySource
            || !string.Equals(before.PriorityReasons, reclamation.PriorityReasons, StringComparison.Ordinal)
            || before.FirstResponseDeadline != reclamation.FirstResponseDeadline
            || before.PlanningDeadline != reclamation.PlanningDeadline
            || before.ResolutionDeadline != reclamation.ResolutionDeadline
            || before.SlaStatus != reclamation.SlaStatus
            || before.SlaBreachedAt != reclamation.SlaBreachedAt;
    }

    private static string SerializeReasons(IEnumerable<string> reasons)
    {
        return JsonSerializer.Serialize(reasons.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList());
    }

    private static List<string> DeserializeReasons(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
        }
        catch
        {
            return new List<string> { value };
        }
    }

    private static string? GetActiveSlaTarget(Reclamation reclamation)
    {
        return reclamation.Status switch
        {
            ReclamationStatus.Open => "FIRST_RESPONSE",
            ReclamationStatus.Assigned => "PLANNING",
            ReclamationStatus.Planned => "RESOLUTION",
            ReclamationStatus.InProgress => "RESOLUTION",
            _ => null
        };
    }

    private static DateTime? GetActiveSlaDeadline(Reclamation reclamation)
    {
        return reclamation.Status switch
        {
            ReclamationStatus.Open => reclamation.FirstResponseDeadline,
            ReclamationStatus.Assigned => reclamation.PlanningDeadline,
            ReclamationStatus.Planned => reclamation.ResolutionDeadline,
            ReclamationStatus.InProgress => reclamation.ResolutionDeadline,
            _ => null
        };
    }

    private static ReclamationPriorityDto ToPriorityDto(Reclamation reclamation)
    {
        return new ReclamationPriorityDto
        {
            ReclamationId = reclamation.Id,
            Priority = reclamation.Priority,
            Severity = reclamation.Severity,
            PriorityScore = reclamation.PriorityScore,
            PriorityReasons = DeserializeReasons(reclamation.PriorityReasons),
            PrioritySource = reclamation.PrioritySource,
            PriorityUpdatedAt = reclamation.PriorityUpdatedAt,
            ManualPriorityOverride = reclamation.ManualPriorityOverride,
            ManualPriorityOverrideReason = reclamation.ManualPriorityOverrideReason
        };
    }

    private static ReclamationSlaDto ToSlaDto(Reclamation reclamation)
    {
        return new ReclamationSlaDto
        {
            ReclamationId = reclamation.Id,
            SlaStatus = reclamation.SlaStatus,
            FirstResponseDeadline = reclamation.FirstResponseDeadline,
            PlanningDeadline = reclamation.PlanningDeadline,
            ResolutionDeadline = reclamation.ResolutionDeadline,
            SlaBreachedAt = reclamation.SlaBreachedAt,
            ActiveTarget = GetActiveSlaTarget(reclamation),
            ActiveDeadline = GetActiveSlaDeadline(reclamation)
        };
    }

    private static void EnsurePriorityManagement(CurrentUser actor, Reclamation reclamation)
    {
        var role = NormalizeRole(actor.Role);
        if (role == "ADMIN")
        {
            return;
        }

        if (role == "SAV" && (reclamation.Status == ReclamationStatus.Open || reclamation.SAVId == actor.UserId))
        {
            return;
        }

        throw new UnauthorizedAccessException();
    }

    private sealed record OperationalState(
        TicketCategory Category,
        string? CategoryReason,
        NamePriority Priority,
        int PriorityScore,
        PrioritySource PrioritySource,
        string? PriorityReasons,
        SlaStatus SlaStatus);

    private sealed record DerivedState(
        TicketCategory Category,
        string? CategoryReason,
        NamePriority Priority,
        int PriorityScore,
        PrioritySource PrioritySource,
        string? PriorityReasons,
        DateTime? FirstResponseDeadline,
        DateTime? PlanningDeadline,
        DateTime? ResolutionDeadline,
        SlaStatus SlaStatus,
        DateTime? SlaBreachedAt);
}
