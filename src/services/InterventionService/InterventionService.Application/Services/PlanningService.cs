using InterventionService.Application.DTOs;
using InterventionService.Application.Outbox;
using InterventionService.Application.Security;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;
using SharedEvents.Events;

namespace InterventionService.Application.Services;

public class PlanningService
{
    private readonly IPlanningRequestRepository _planningRequestRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IInterventionRepository _interventionRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly PlanningCapacityService _capacityService;

    public PlanningService(
        IPlanningRequestRepository planningRequestRepository,
        IAppointmentRepository appointmentRepository,
        IInterventionRepository interventionRepository,
        IOutboxWriter outboxWriter,
        PlanningCapacityService capacityService)
    {
        _planningRequestRepository = planningRequestRepository;
        _appointmentRepository = appointmentRepository;
        _interventionRepository = interventionRepository;
        _outboxWriter = outboxWriter;
        _capacityService = capacityService;
    }

    public async Task<List<PlanningRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _planningRequestRepository.GetAllAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<PlanningRequestDto?> GetRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _planningRequestRepository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<List<AppointmentDto>> QueryAppointmentsAsync(
        CurrentUser actor,
        long? reclamationId = null,
        long? technicianId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (NormalizeRole(actor.Role) == "ST" && !technicianId.HasValue)
        {
            technicianId = actor.UserId;
        }

        var items = await _appointmentRepository.QueryAsync(reclamationId, technicianId, from, to, cancellationToken);
        return items.Select(x => ToDto(x, actor)).ToList();
    }

    public async Task<AppointmentDto?> GetAppointmentAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var item = await _appointmentRepository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : ToDto(item, actor);
    }

    public async Task<AppointmentDto?> GetByReclamationAsync(long reclamationId, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var items = await _appointmentRepository.QueryAsync(reclamationId: reclamationId, cancellationToken: cancellationToken);
        var latest = items.OrderByDescending(x => x.Sequence).ThenByDescending(x => x.StartAt).FirstOrDefault();
        return latest is null ? null : ToDto(latest, actor);
    }

    public Task<TechnicianCapacityDto> GetTechnicianCapacityAsync(long technicianId, CurrentUser actor, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        EnsureTechnicianAccess(actor, technicianId);
        return _capacityService.GetCapacityAsync(technicianId, date, cancellationToken);
    }

    public Task<List<AppointmentDto>> GetTechnicianAgendaAsync(long technicianId, CurrentUser actor, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        EnsureTechnicianAccess(actor, technicianId);
        return QueryAppointmentsAsync(actor, technicianId: technicianId, from: from, to: to, cancellationToken: cancellationToken);
    }

    public async Task<PlanningRequestDto> SyncPlanningRequestedAsync(PlanningRequestedEvent message, CancellationToken cancellationToken = default)
    {
        var existing = await _planningRequestRepository.GetActiveByReclamationIdAsync(message.ReclamationId, cancellationToken);
        if (existing is not null)
        {
            return ToDto(existing);
        }

        var entity = new PlanningRequest
        {
            ReclamationId = message.ReclamationId,
            Reference = message.Reference,
            ClientId = message.ClientId,
            CustomerName = message.ClientName,
            CustomerEmail = message.ClientEmail,
            CustomerPhone = message.CustomerPhone,
            ServiceAddress = message.ServiceAddress,
            SavId = message.SavId,
            SavName = message.SavName,
            Priority = message.Priority,
            ProductName = message.ProductName,
            Brand = message.Brand,
            Model = message.Model,
            SerialNumber = message.SerialNumber,
            RequestedAt = message.OccurredAt,
            Status = PlanningRequestStatus.Pending
        };

        await _planningRequestRepository.AddAsync(entity, cancellationToken);
        await _planningRequestRepository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

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
        EnsureRole(actor, "SAV", "ADMIN", "ST");
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (NormalizeRole(actor.Role) == "ST" && appointment.TechnicianId != actor.UserId)
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
