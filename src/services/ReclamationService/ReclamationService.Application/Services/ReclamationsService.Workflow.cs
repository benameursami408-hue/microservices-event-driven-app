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
    public ReclamationDto Update(long id, UpdateReclamationDto dto, CurrentUser actor)
    {
        var existing = GetByIdVisible(id, actor);
        var before = CaptureOperationalState(existing);

        var role = NormalizeRole(actor.Role);
        if (role == "CLIENT")
        {
            if (existing.Status != ReclamationStatus.Open)
            {
                throw new BadRequestException("You can only update an OPEN reclamation.");
            }
        }
        else if (role == "SAV")
        {
            if (existing.SAVId != actor.UserId)
            {
                throw new UnauthorizedAccessException();
            }

            if (existing.Status is ReclamationStatus.Closed or ReclamationStatus.Cancelled or ReclamationStatus.Rejected)
            {
                throw new BadRequestException("Cannot update a closed/cancelled/rejected reclamation.");
            }
        }
        else if (role != "ADMIN")
        {
            throw new UnauthorizedAccessException();
        }

        existing.ApplyUpdate(dto);
        ApplyDerivedState(existing);
        var updated = _reclamationRepository.Update(existing);
        QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, NormalizeRole(actor.Role)).GetAwaiter().GetResult();
        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> AssignAsync(long id, AssignReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "SAV", "ADMIN");

        var reclamation = GetByIdInternal(id);

        if (reclamation.Status != ReclamationStatus.Open)
        {
            throw new BadRequestException("Only OPEN reclamations can be assigned.");
        }

        var role = NormalizeRole(actor.Role);
        long savId;
        string savName;

        if (role == "ADMIN" && dto.SavId.HasValue)
        {
            savId = dto.SavId.Value;
            savName = string.IsNullOrWhiteSpace(dto.SavName) ? $"SAV#{savId}" : dto.SavName;
        }
        else
        {
            savId = actor.UserId;
            savName = actor.FullName;
            if (string.IsNullOrWhiteSpace(savName))
            {
                savName = actor.Email;
            }
        }

        if (savId <= 0)
        {
            throw new BadRequestException("Invalid SAV id.");
        }

        var fromStatus = reclamation.Status;
        var before = CaptureOperationalState(reclamation);
        reclamation.SAVId = savId;
        reclamation.SAVName = savName;
        reclamation.AssignedAt = DateTime.UtcNow;
        reclamation.Status = ReclamationStatus.Assigned;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = fromStatus,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        });

        var clientEmail = _clientRepository.GetById(updated.ClientId)?.Email ?? string.Empty;

        await _outboxWriter.EnqueueAsync(new ReclamationAssignedEvent
        {
            CorrelationId = actor.CorrelationId,
            ReclamationId = updated.Id,
            Reference = updated.Reference,
            ClientId = updated.ClientId,
            ClientEmail = clientEmail,
            SavId = savId,
            SavName = savName,
            ActorUserId = actor.UserId,
            ActorRole = role,
            OccurredAt = DateTime.UtcNow
        });

        await QueueStatusChangedAsync(updated, fromStatus, updated.Status, actor, dto.Comment);
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);

        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> PlanAsync(long id, PlanReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "SAV", "ADMIN");

        if (dto.PlannedEndAt.HasValue && dto.PlannedEndAt.Value <= dto.PlannedStartAt)
        {
            throw new BadRequestException("PlannedEndAt must be after PlannedStartAt.");
        }

        var reclamation = GetByIdInternal(id);
        var role = NormalizeRole(actor.Role);

        if (reclamation.Status != ReclamationStatus.Assigned)
        {
            throw new BadRequestException("Only ASSIGNED reclamations can be planned.");
        }

        if (role == "SAV" && reclamation.SAVId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (reclamation.SAVId is null || reclamation.SAVId <= 0)
        {
            throw new BadRequestException("Reclamation must be assigned to a SAV before planning.");
        }

        var fromStatus = reclamation.Status;
        var before = CaptureOperationalState(reclamation);
        reclamation.TechnicianId = dto.TechnicianId;
        reclamation.TechnicianName = string.IsNullOrWhiteSpace(dto.TechnicianName) ? null : dto.TechnicianName;
        reclamation.PlannedStartAt = EnsureUtc(dto.PlannedStartAt);
        reclamation.PlannedEndAt = dto.PlannedEndAt.HasValue ? EnsureUtc(dto.PlannedEndAt.Value) : null;
        reclamation.PlanningNote = dto.PlanningNote;
        reclamation.Status = ReclamationStatus.Planned;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = fromStatus,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = dto.PlanningNote,
            OccurredAt = DateTime.UtcNow
        });

        var clientEmail = _clientRepository.GetById(updated.ClientId)?.Email ?? string.Empty;

        var savId = updated.SAVId ?? throw new InvalidOperationException("Reclamation is planned but has no SAV assigned.");
        var plannedStartAt = updated.PlannedStartAt ?? throw new InvalidOperationException("Reclamation is planned but PlannedStartAt is null.");

        await _outboxWriter.EnqueueAsync(new ReclamationPlannedEvent
        {
            CorrelationId = actor.CorrelationId,
            ReclamationId = updated.Id,
            Reference = updated.Reference,
            ClientId = updated.ClientId,
            ClientEmail = clientEmail,
            SavId = savId,
            SavName = updated.SAVName ?? string.Empty,
            TechnicianId = dto.TechnicianId,
            TechnicianName = dto.TechnicianName ?? string.Empty,
            PlannedStartAt = plannedStartAt,
            PlannedEndAt = updated.PlannedEndAt,
            PlanningNote = dto.PlanningNote,
            ActorUserId = actor.UserId,
            ActorRole = role,
            OccurredAt = DateTime.UtcNow
        });

        await QueueStatusChangedAsync(updated, fromStatus, updated.Status, actor, dto.PlanningNote);
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);
        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> RequestPlanningAsync(long id, RequestPlanningDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "SAV", "ADMIN");

        var reclamation = GetByIdInternal(id);
        var role = NormalizeRole(actor.Role);

        if (reclamation.Status != ReclamationStatus.Assigned)
        {
            throw new BadRequestException("Only ASSIGNED reclamations can request planning.");
        }

        if (role == "SAV" && reclamation.SAVId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        var before = CaptureOperationalState(reclamation);
        var client = _clientRepository.GetById(reclamation.ClientId);
        await _outboxWriter.EnqueueAsync(new PlanningRequestedEvent
        {
            CorrelationId = actor.CorrelationId,
            ReclamationId = reclamation.Id,
            Reference = reclamation.Reference,
            ClientId = reclamation.ClientId,
            ClientName = reclamation.ClientName,
            ClientEmail = client?.Email ?? string.Empty,
            CustomerPhone = client?.PhoneNumber,
            ServiceAddress = null,
            SavId = reclamation.SAVId ?? actor.UserId,
            SavName = reclamation.SAVName ?? actor.FullName ?? string.Empty,
            Priority = reclamation.Priority.ToString().ToUpperInvariant(),
            ProductName = reclamation.ProductName,
            Brand = reclamation.Brand,
            Model = reclamation.Model,
            SerialNumber = reclamation.SerialNumber,
            OccurredAt = DateTime.UtcNow
        });

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = reclamation.Id,
            FromStatus = reclamation.Status,
            ToStatus = reclamation.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? "Planning requested" : dto.Comment,
            OccurredAt = DateTime.UtcNow
        });

        ApplyDerivedState(reclamation);
        var updated = _reclamationRepository.Update(reclamation);
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);
        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> StartAsync(long id, CurrentUser actor)
    {
        EnsureRole(actor, "ST", "ADMIN");

        var reclamation = GetByIdInternal(id);
        var role = NormalizeRole(actor.Role);

        if (reclamation.Status != ReclamationStatus.Planned)
        {
            throw new BadRequestException("Only PLANNED reclamations can be started.");
        }

        if (role == "ST" && reclamation.TechnicianId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        var fromStatus = reclamation.Status;
        var before = CaptureOperationalState(reclamation);
        reclamation.Status = ReclamationStatus.InProgress;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = fromStatus,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = "Started",
            OccurredAt = DateTime.UtcNow
        });

        await QueueStatusChangedAsync(updated, fromStatus, updated.Status, actor, "Started");
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);
        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> ResolveAsync(long id, ResolveReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "ST", "ADMIN");

        var reclamation = GetByIdInternal(id);
        var role = NormalizeRole(actor.Role);

        if (reclamation.Status != ReclamationStatus.InProgress)
        {
            throw new BadRequestException("Only IN_PROGRESS reclamations can be resolved.");
        }

        if (role == "ST" && reclamation.TechnicianId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        var fromStatus = reclamation.Status;
        var before = CaptureOperationalState(reclamation);
        reclamation.ResolutionNote = dto.ResolutionNote;
        reclamation.ResolvedAt = DateTime.UtcNow;
        reclamation.Status = ReclamationStatus.Resolved;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = fromStatus,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = dto.ResolutionNote,
            OccurredAt = DateTime.UtcNow
        });

        await QueueStatusChangedAsync(updated, fromStatus, updated.Status, actor, dto.ResolutionNote);
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);
        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> CloseAsync(long id, CloseReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "SAV", "ADMIN");

        var reclamation = GetByIdInternal(id);
        var role = NormalizeRole(actor.Role);

        if (reclamation.Status != ReclamationStatus.Resolved)
        {
            throw new BadRequestException("Only RESOLVED reclamations can be closed.");
        }

        if (role == "SAV" && reclamation.SAVId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        var fromStatus = reclamation.Status;
        var before = CaptureOperationalState(reclamation);
        reclamation.Status = ReclamationStatus.Closed;
        reclamation.ClosedAt = DateTime.UtcNow;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = fromStatus,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        });

        await QueueStatusChangedAsync(updated, fromStatus, updated.Status, actor, dto.Comment);
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);
        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> CancelAsync(long id, CurrentUser actor)
    {
        EnsureRole(actor, "CLIENT", "ADMIN");

        var reclamation = GetByIdInternal(id);
        var role = NormalizeRole(actor.Role);

        if (role == "CLIENT" && reclamation.ClientId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (reclamation.Status != ReclamationStatus.Open)
        {
            throw new BadRequestException("Only OPEN reclamations can be cancelled.");
        }

        var fromStatus = reclamation.Status;
        var before = CaptureOperationalState(reclamation);
        reclamation.Status = ReclamationStatus.Cancelled;
        reclamation.CancelledAt = DateTime.UtcNow;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = fromStatus,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = "Cancelled",
            OccurredAt = DateTime.UtcNow
        });

        await QueueStatusChangedAsync(updated, fromStatus, updated.Status, actor, "Cancelled");
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);
        return ToDtoWithActions(updated, actor);
    }

    public async Task<ReclamationDto> RejectAsync(long id, RejectReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "SAV", "ADMIN");

        var reclamation = GetByIdInternal(id);
        var role = NormalizeRole(actor.Role);

        if (reclamation.Status is not (ReclamationStatus.Open or ReclamationStatus.Assigned))
        {
            throw new BadRequestException("Only OPEN or ASSIGNED reclamations can be rejected.");
        }

        if (role == "SAV" && reclamation.SAVId != actor.UserId)
        {
            // SAV can only reject items assigned to them; unassigned items can be rejected by ADMIN.
            throw new UnauthorizedAccessException();
        }

        var fromStatus = reclamation.Status;
        var before = CaptureOperationalState(reclamation);
        reclamation.Status = ReclamationStatus.Rejected;
        reclamation.RejectedAt = DateTime.UtcNow;
        reclamation.RejectionReason = dto.Reason;
        ApplyDerivedState(reclamation);

        var updated = _reclamationRepository.Update(reclamation);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = fromStatus,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = dto.Reason,
            OccurredAt = DateTime.UtcNow
        });

        await QueueStatusChangedAsync(updated, fromStatus, updated.Status, actor, dto.Reason);
        await QueueOperationalEventsAsync(updated, before, actor.CorrelationId, actor.UserId, role);
        return ToDtoWithActions(updated, actor);
    }

}
