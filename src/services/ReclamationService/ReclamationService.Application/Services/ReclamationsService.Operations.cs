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
    public List<ReclamationHistoryDto> GetHistory(long id, CurrentUser actor)
    {
        // Ensures view permission
        _ = GetByIdVisible(id, actor);

        return _historyRepository
            .GetByReclamationId(id)
            .Select(h => h.ToDto())
            .ToList();
    }

    public async Task<ReclamationPriorityDto> GetPriorityAsync(long id, CurrentUser actor)
    {
        var reclamation = GetByIdVisible(id, actor);
        var before = CaptureOperationalState(reclamation);
        var beforeDerived = CaptureDerivedState(reclamation);
        ApplyDerivedState(reclamation);

        if (HasDerivedStateChanged(beforeDerived, reclamation))
        {
            var updated = _reclamationRepository.Update(reclamation);
            await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, NormalizeRole(actor.Role));
            return ToPriorityDto(updated);
        }

        return ToPriorityDto(reclamation);
    }

    public async Task<ReclamationSlaDto> GetSlaAsync(long id, CurrentUser actor)
    {
        var reclamation = GetByIdVisible(id, actor);
        var before = CaptureOperationalState(reclamation);
        var beforeDerived = CaptureDerivedState(reclamation);
        ApplyDerivedState(reclamation);

        if (HasDerivedStateChanged(beforeDerived, reclamation))
        {
            var updated = _reclamationRepository.Update(reclamation);
            await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, NormalizeRole(actor.Role));
            return ToSlaDto(updated);
        }

        return ToSlaDto(reclamation);
    }

    public async Task<int> SweepSlaAsync(CancellationToken cancellationToken = default)
    {
        var tracked = new[]
        {
            ReclamationStatus.Open,
            ReclamationStatus.Assigned,
            ReclamationStatus.Planned,
            ReclamationStatus.InProgress
        };

        var reclamations = tracked
            .SelectMany(_reclamationRepository.GetByStatus)
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .ToList();

        var updatedCount = 0;

        foreach (var reclamation in reclamations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var before = CaptureOperationalState(reclamation);
            var beforeDerived = CaptureDerivedState(reclamation);
            ApplyDerivedState(reclamation);

            if (!HasDerivedStateChanged(beforeDerived, reclamation))
            {
                continue;
            }

            var updated = _reclamationRepository.Update(reclamation);
            await QueueOperationalEventsAsync(
                updated,
                before,
                $"sla-sweep-{updated.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                0,
                "SYSTEM");
            updatedCount += 1;
        }

        return updatedCount;
    }

    public async Task<ReclamationPriorityDto> RecalculatePriorityAsync(long id, CurrentUser actor)
    {
        var reclamation = GetByIdVisible(id, actor);
        EnsurePriorityManagement(actor, reclamation);
        var before = CaptureOperationalState(reclamation);
        reclamation.ManualPriorityOverride = false;
        reclamation.ManualPriorityOverrideReason = null;
        ApplyDerivedState(reclamation);
        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = updated.Status,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = NormalizeRole(actor.Role),
            Comment = $"Priority recalculated to {updated.Priority} (score {updated.PriorityScore}).",
            OccurredAt = DateTime.UtcNow
        });

        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, NormalizeRole(actor.Role));
        return ToPriorityDto(updated);
    }

    public async Task<ReclamationPriorityDto> OverridePriorityAsync(long id, OverridePriorityDto dto, CurrentUser actor)
    {
        var reclamation = GetByIdVisible(id, actor);
        EnsurePriorityManagement(actor, reclamation);

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new BadRequestException("Override reason is required.");
        }

        var before = CaptureOperationalState(reclamation);
        reclamation.ManualPriorityOverride = true;
        reclamation.ManualPriorityOverrideReason = dto.Reason.Trim();
        reclamation.Priority = dto.Priority;
        reclamation.PrioritySource = PrioritySource.ManualOverride;
        reclamation.PriorityUpdatedAt = DateTime.UtcNow;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);
        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = updated.Status,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = NormalizeRole(actor.Role),
            Comment = $"Manual priority override to {updated.Priority}: {dto.Reason.Trim()}",
            OccurredAt = DateTime.UtcNow
        });

        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, NormalizeRole(actor.Role));
        return ToPriorityDto(updated);
    }

}
