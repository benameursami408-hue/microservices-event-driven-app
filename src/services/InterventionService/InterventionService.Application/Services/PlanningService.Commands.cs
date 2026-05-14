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
    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN");
        var planningRequest = await _planningRequestRepository.GetByIdAsync(dto.PlanningRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Planning request not found.");

        if (planningRequest.Status == PlanningRequestStatus.Cancelled)
        {
            throw new InvalidOperationException("Planning request is cancelled.");
        }

        if (dto.EstimatedDurationMinutes <= 0)
        {
            throw new InvalidOperationException("Estimated duration must be positive.");
        }

        var startAt = EnsureUtc(dto.StartAt);
        var endAt = ResolveEndAt(startAt, dto.EndAt, dto.EstimatedDurationMinutes);
        var existingAppointments = await _appointmentRepository.QueryAsync(reclamationId: planningRequest.ReclamationId, cancellationToken: cancellationToken);

        var appointment = new Appointment
        {
            PlanningRequestId = planningRequest.Id,
            ReclamationId = planningRequest.ReclamationId,
            Reference = planningRequest.Reference,
            StartAt = startAt,
            EndAt = endAt,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone) ? "UTC" : dto.TimeZone,
            CustomerPresenceRequired = dto.CustomerPresenceRequired,
            PlanningNote = dto.PlanningNote,
            Status = AppointmentStatus.Proposed,
            Sequence = existingAppointments.Count == 0 ? 1 : existingAppointments.Max(x => x.Sequence) + 1
        };

        planningRequest.Status = PlanningRequestStatus.InProgress;

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _appointmentRepository.SaveChangesAsync(cancellationToken);
        await _planningRequestRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new AppointmentProposedEvent
        {
            CorrelationId = actor.CorrelationId,
            AppointmentId = appointment.Id,
            ReclamationId = appointment.ReclamationId,
            Reference = appointment.Reference,
            StartAt = appointment.StartAt,
            EndAt = appointment.EndAt,
            EstimatedDurationMinutes = appointment.EstimatedDurationMinutes,
            TechnicianId = appointment.TechnicianId,
            TechnicianName = appointment.TechnicianName,
            Sequence = appointment.Sequence,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(appointment, actor);
    }

    public async Task<AppointmentDto> AssignTechnicianAsync(Guid appointmentId, AssignTechnicianDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN");
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        var evaluation = await _capacityService.EvaluateAsync(
            dto.TechnicianId,
            appointment.StartAt,
            appointment.EndAt,
            appointment.EstimatedDurationMinutes,
            appointment.Id,
            cancellationToken);

        if (!evaluation.IsAvailable)
        {
            await PublishConflictAndThrowAsync(appointment, dto.TechnicianId, dto.TechnicianName, evaluation, actor, cancellationToken);
        }

        appointment.TechnicianId = dto.TechnicianId;
        appointment.TechnicianName = dto.TechnicianName;
        appointment.UpdatedAt = DateTime.UtcNow;
        var assignment = new Assignment
        {
            AppointmentId = appointment.Id,
            TechnicianId = dto.TechnicianId,
            TechnicianName = dto.TechnicianName,
            AssignedByUserId = actor.UserId,
            AssignedByRole = NormalizeRole(actor.Role),
            AssignedAt = DateTime.UtcNow,
            Status = AssignmentStatus.Assigned
        };
        await _appointmentRepository.AddAssignmentAsync(assignment, cancellationToken);

        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new TechnicianAssignedEvent
        {
            CorrelationId = actor.CorrelationId,
            AppointmentId = appointment.Id,
            ReclamationId = appointment.ReclamationId,
            Reference = appointment.Reference,
            TechnicianId = dto.TechnicianId,
            TechnicianName = dto.TechnicianName,
            AssignedAt = DateTime.UtcNow,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(appointment, actor);
    }

    public async Task<AppointmentDto> ConfirmAppointmentAsync(Guid appointmentId, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN");
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (!appointment.TechnicianId.HasValue || string.IsNullOrWhiteSpace(appointment.TechnicianName))
        {
            throw new InvalidOperationException("Appointment must have an assigned technician before confirmation.");
        }

        var active = await _appointmentRepository.GetActiveConfirmedByReclamationIdAsync(appointment.ReclamationId, cancellationToken);
        if (active is not null && active.Id != appointment.Id)
        {
            throw new InvalidOperationException("Another confirmed appointment already exists for this reclamation.");
        }

        var evaluation = await _capacityService.EvaluateAsync(
            appointment.TechnicianId.Value,
            appointment.StartAt,
            appointment.EndAt,
            appointment.EstimatedDurationMinutes,
            appointment.Id,
            cancellationToken);

        if (!evaluation.IsAvailable)
        {
            await PublishConflictAndThrowAsync(
                appointment,
                appointment.TechnicianId.Value,
                appointment.TechnicianName ?? string.Empty,
                evaluation,
                actor,
                cancellationToken);
        }

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.UpdatedAt = DateTime.UtcNow;

        var planningRequest = await _planningRequestRepository.GetByIdAsync(appointment.PlanningRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Planning request not found.");
        planningRequest.Status = PlanningRequestStatus.Satisfied;

        var intervention = await _interventionRepository.GetByAppointmentIdAsync(appointment.Id, cancellationToken);
        if (intervention is null)
        {
            intervention = new Domain.Entities.Intervention
            {
                AppointmentId = appointment.Id,
                ReclamationId = appointment.ReclamationId,
                ClientId = planningRequest.ClientId,
                Reference = appointment.Reference,
                TechnicianId = appointment.TechnicianId.Value,
                TechnicianName = appointment.TechnicianName ?? string.Empty,
                Status = InterventionStatus.Ready,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _interventionRepository.AddAsync(intervention, cancellationToken);
        }

        await _appointmentRepository.SaveChangesAsync(cancellationToken);
        await _planningRequestRepository.SaveChangesAsync(cancellationToken);
        await _interventionRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new AppointmentConfirmedEvent
        {
            CorrelationId = actor.CorrelationId,
            AppointmentId = appointment.Id,
            ReclamationId = appointment.ReclamationId,
            Reference = appointment.Reference,
            TechnicianId = appointment.TechnicianId.Value,
            TechnicianName = appointment.TechnicianName ?? string.Empty,
            StartAt = appointment.StartAt,
            EndAt = appointment.EndAt,
            EstimatedDurationMinutes = appointment.EstimatedDurationMinutes,
            Sequence = appointment.Sequence,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(appointment, actor);
    }

    public async Task<AppointmentDto> RescheduleAppointmentAsync(Guid appointmentId, RescheduleAppointmentDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN", "ST", "TECHNICIAN");
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (IsTechnicianRole(actor.Role) && appointment.TechnicianId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        var oldStart = appointment.StartAt;
        var oldEnd = appointment.EndAt;
        var estimatedDuration = dto.EstimatedDurationMinutes ?? appointment.EstimatedDurationMinutes;
        if (estimatedDuration <= 0)
        {
            throw new InvalidOperationException("Estimated duration must be positive.");
        }

        appointment.StartAt = EnsureUtc(dto.StartAt);
        appointment.EndAt = ResolveEndAt(appointment.StartAt, dto.EndAt, estimatedDuration);
        appointment.EstimatedDurationMinutes = estimatedDuration;

        if (appointment.TechnicianId.HasValue)
        {
            var evaluation = await _capacityService.EvaluateAsync(
                appointment.TechnicianId.Value,
                appointment.StartAt,
                appointment.EndAt,
                appointment.EstimatedDurationMinutes,
                appointment.Id,
                cancellationToken);

            if (!evaluation.IsAvailable)
            {
                await PublishConflictAndThrowAsync(
                    appointment,
                    appointment.TechnicianId.Value,
                    appointment.TechnicianName ?? string.Empty,
                    evaluation,
                    actor,
                    cancellationToken);
            }
        }

        appointment.Status = AppointmentStatus.Rescheduled;
        appointment.Sequence += 1;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new AppointmentRescheduledEvent
        {
            CorrelationId = actor.CorrelationId,
            AppointmentId = appointment.Id,
            ReclamationId = appointment.ReclamationId,
            Reference = appointment.Reference,
            OldStartAt = oldStart,
            OldEndAt = oldEnd,
            NewStartAt = appointment.StartAt,
            NewEndAt = appointment.EndAt,
            EstimatedDurationMinutes = appointment.EstimatedDurationMinutes,
            TechnicianId = appointment.TechnicianId ?? 0,
            TechnicianName = appointment.TechnicianName ?? string.Empty,
            ReasonCode = dto.ReasonCode,
            ReasonText = dto.ReasonText,
            Sequence = appointment.Sequence,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(appointment, actor);
    }

    public async Task<AppointmentDto> CancelAppointmentAsync(Guid appointmentId, CancelAppointmentDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN");
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelReasonCode = dto.ReasonCode;
        appointment.CancelReasonText = dto.ReasonText;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        await _outboxWriter.EnqueueAsync(new AppointmentCancelledEvent
        {
            CorrelationId = actor.CorrelationId,
            AppointmentId = appointment.Id,
            ReclamationId = appointment.ReclamationId,
            Reference = appointment.Reference,
            ReasonCode = dto.ReasonCode,
            ReasonText = dto.ReasonText,
            CancelledByRole = NormalizeRole(actor.Role),
            CancelledByUserId = actor.UserId,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(appointment, actor);
    }

}
