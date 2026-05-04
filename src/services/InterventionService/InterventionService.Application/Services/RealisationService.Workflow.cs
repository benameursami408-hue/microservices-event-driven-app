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
    public async Task<InterventionDto> StartAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        if (intervention.Status is not (InterventionStatus.Ready or InterventionStatus.Paused))
        {
            throw new InvalidOperationException("Intervention cannot be started.");
        }

        intervention.Status = InterventionStatus.Started;
        intervention.StartedAt ??= DateTime.UtcNow;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new InterventionStartedEvent
        {
            CorrelationId = actor.CorrelationId,
            InterventionId = intervention.Id,
            AppointmentId = intervention.AppointmentId,
            ReclamationId = intervention.ReclamationId,
            TechnicianId = intervention.TechnicianId,
            TechnicianName = intervention.TechnicianName,
            StartedAt = intervention.StartedAt ?? DateTime.UtcNow,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> PauseAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        if (intervention.Status != InterventionStatus.Started)
        {
            throw new InvalidOperationException("Intervention cannot be paused.");
        }

        intervention.Status = InterventionStatus.Paused;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);
        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> AddDiagnosticAsync(Guid id, RecordDiagnosticDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        var diagnostic = new Diagnostic
        {
            InterventionId = intervention.Id,
            Category = dto.Category,
            Summary = dto.Summary,
            RootCause = dto.RootCause,
            RequiresParts = dto.RequiresParts,
            RequiresFollowUp = dto.RequiresFollowUp,
            CreatedAt = DateTime.UtcNow
        };
        await _interventionRepository.AddDiagnosticAsync(diagnostic, cancellationToken);
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new DiagnosticRecordedEvent
        {
            CorrelationId = actor.CorrelationId,
            InterventionId = intervention.Id,
            ReclamationId = intervention.ReclamationId,
            Category = dto.Category,
            RequiresParts = dto.RequiresParts,
            RequiresFollowUp = dto.RequiresFollowUp,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> AddRepairActionAsync(Guid id, AddRepairActionDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        var repairAction = new RepairAction
        {
            InterventionId = intervention.Id,
            ActionType = dto.ActionType,
            Description = dto.Description,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Success = dto.Success
        };
        await _interventionRepository.AddRepairActionAsync(repairAction, cancellationToken);
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);
        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> AddPartAsync(Guid id, AddPartUsedDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        var partUsed = new PartUsed
        {
            InterventionId = intervention.Id,
            PartCode = dto.PartCode,
            Label = dto.Label,
            Quantity = dto.Quantity,
            AvailabilityStatus = dto.AvailabilityStatus
        };
        await _interventionRepository.AddPartUsedAsync(partUsed, cancellationToken);
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);
        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> AddEvidenceAsync(Guid id, AddEvidenceDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        var evidence = new InterventionEvidence
        {
            InterventionId = intervention.Id,
            Kind = dto.Kind,
            Url = dto.Url,
            CapturedAt = DateTime.UtcNow,
            CapturedByRole = NormalizeRole(actor.Role),
            CapturedByUserId = actor.UserId
        };
        await _interventionRepository.AddEvidenceAsync(evidence, cancellationToken);
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);
        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> CompleteAsync(Guid id, CompleteInterventionDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        if (!intervention.Diagnostics.Any())
        {
            throw new InvalidOperationException("At least one diagnostic is required before completion.");
        }

        intervention.Status = InterventionStatus.Completed;
        intervention.Outcome = dto.Outcome;
        intervention.NeedsReplanning = dto.NeedsReplanning;
        intervention.EndedAt = DateTime.UtcNow;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new InterventionCompletedEvent
        {
            CorrelationId = actor.CorrelationId,
            InterventionId = intervention.Id,
            ReclamationId = intervention.ReclamationId,
            Outcome = dto.Outcome.ToString().ToUpperInvariant(),
            NeedsReplanning = dto.NeedsReplanning,
            CompletedAt = intervention.EndedAt ?? DateTime.UtcNow,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> PublishReportAsync(Guid id, PublishVisitReportDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await _interventionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Intervention not found.");

        EnsureRole(actor, "SAV", "ADMIN", "ST");
        if (NormalizeRole(actor.Role) == "ST" && intervention.TechnicianId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (intervention.Status != InterventionStatus.Completed)
        {
            throw new InvalidOperationException("Only completed interventions can publish reports.");
        }

        var report = new VisitReport
        {
            InterventionId = intervention.Id,
            Summary = dto.Summary,
            Outcome = dto.Outcome,
            CustomerPresent = dto.CustomerPresent,
            NextStep = dto.NextStep,
            Status = VisitReportStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _interventionRepository.AddVisitReportAsync(report, cancellationToken);
        intervention.Outcome = dto.Outcome;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new RealisationReportedEvent
        {
            CorrelationId = actor.CorrelationId,
            InterventionId = intervention.Id,
            ReclamationId = intervention.ReclamationId,
            Outcome = dto.Outcome.ToString().ToUpperInvariant(),
            NeedsReplanning = intervention.NeedsReplanning,
            Summary = dto.Summary,
            NextStep = dto.NextStep,
            PublishedAt = report.PublishedAt ?? DateTime.UtcNow,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        if (intervention.NeedsReplanning || dto.Outcome == InterventionOutcome.NeedsReplanning)
        {
            await _outboxWriter.EnqueueAsync(new ReplanningRequiredEvent
            {
                CorrelationId = actor.CorrelationId,
                InterventionId = intervention.Id,
                ReclamationId = intervention.ReclamationId,
                ReasonCode = "NEEDS_REPLANNING",
                ReasonText = dto.NextStep,
                OccurredAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return ToDto(intervention, actor);
    }

    public async Task<InterventionDto> RequestReplanningAsync(Guid id, RequestReplanningDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        intervention.NeedsReplanning = true;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new ReplanningRequiredEvent
        {
            CorrelationId = actor.CorrelationId,
            InterventionId = intervention.Id,
            ReclamationId = intervention.ReclamationId,
            ReasonCode = dto.ReasonCode,
            ReasonText = dto.ReasonText,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(intervention, actor);
    }

}
