using ReclamationService.Application.Outbox;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Application.Mappers;
using ReclamationService.Application.Security;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;
using SharedEvents.Events;

namespace ReclamationService.Application.Services;

public class ReclamationsService
{
    private readonly IReclamationRepository _reclamationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IReclamationHistoryRepository _historyRepository;
    private readonly IOutboxWriter _outboxWriter;

    public ReclamationsService(
        IReclamationRepository reclamationRepository,
        IClientRepository clientRepository,
        IReclamationHistoryRepository historyRepository,
        IOutboxWriter outboxWriter)
    {
        _reclamationRepository = reclamationRepository;
        _clientRepository = clientRepository;
        _historyRepository = historyRepository;
        _outboxWriter = outboxWriter;
    }

    public async Task<ReclamationDto> CreateAsync(CreateReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "CLIENT", "ADMIN");

        var clientName = string.IsNullOrWhiteSpace(actor.FullName) ? (actor.Email ?? string.Empty) : actor.FullName;

        var reclamation = dto.ToEntity(actor.UserId, clientName);
        var created = _reclamationRepository.Create(reclamation);

        var clientEmail = actor.Email;
        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            clientEmail = _clientRepository.GetById(created.ClientId)?.Email ?? string.Empty;
        }

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = created.Id,
            FromStatus = ReclamationStatus.Open,
            ToStatus = ReclamationStatus.Open,
            ActorUserId = actor.UserId,
            ActorRole = NormalizeRole(actor.Role),
            Comment = "Created",
            OccurredAt = DateTime.UtcNow
        });

        await _outboxWriter.EnqueueAsync(new ReclamationCreatedEvent
        {
            CorrelationId = actor.CorrelationId,
            ReclamationId = created.Id,
            Reference = created.Reference,
            ClientId = created.ClientId,
            ClientName = created.ClientName,
            ClientEmail = clientEmail,
            Priority = created.Priority.ToString(),
            Status = ToStatusCode(created.Status),
            Description = created.Description,
            OccurredAt = DateTime.UtcNow
        });

        return ToDtoWithActions(created, actor);
    }

    public List<ReclamationDto> GetVisible(CurrentUser actor, ReclamationStatus? status = null)
    {
        var role = NormalizeRole(actor.Role);
        List<Reclamation> items;

        if (role == "ADMIN")
        {
            items = status.HasValue
                ? _reclamationRepository.GetByStatus(status.Value)
                : _reclamationRepository.GetAll();
        }
        else if (role == "SAV")
        {
            var backlog = _reclamationRepository.GetOpenBacklog();
            var mine = _reclamationRepository.GetForSav(actor.UserId);
            items = backlog.Concat(mine)
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            if (status.HasValue)
            {
                items = items.Where(r => r.Status == status.Value).ToList();
            }
        }
        else if (role == "ST")
        {
            items = _reclamationRepository.GetForTechnician(actor.UserId);
            if (status.HasValue)
            {
                items = items.Where(r => r.Status == status.Value).ToList();
            }
        }
        else
        {
            items = _reclamationRepository.GetForClient(actor.UserId);
            if (status.HasValue)
            {
                items = items.Where(r => r.Status == status.Value).ToList();
            }
        }

        return items.Select(r => ToDtoWithActions(r, actor)).ToList();
    }

    public PagedResult<ReclamationDto> QueryVisible(
        CurrentUser actor,
        ReclamationStatus? status = null,
        NamePriority? priority = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        IEnumerable<ReclamationDto> query = GetVisible(actor, status);

        if (priority.HasValue)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.Reference ?? string.Empty).ToLowerInvariant().Contains(normalized)
                || (x.ClientName ?? string.Empty).ToLowerInvariant().Contains(normalized)
                || (x.Description ?? string.Empty).ToLowerInvariant().Contains(normalized)
                || x.Status.ToString().ToLowerInvariant().Contains(normalized));
        }

        var total = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ReclamationDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public void Delete(long id, CurrentUser actor)
    {
        EnsureRole(actor, "ADMIN");

        var existing = GetByIdInternal(id);
        if (existing.Status is not (ReclamationStatus.Open or ReclamationStatus.Cancelled))
        {
            throw new BadRequestException("Only OPEN or CANCELLED reclamations can be deleted.");
        }

        _reclamationRepository.Delete(id);
    }

    public ReclamationDto GetById(long id, CurrentUser actor)
    {
        var reclamation = GetByIdVisible(id, actor);
        return ToDtoWithActions(reclamation, actor);
    }

    public List<ReclamationDto> GetByPriority(NamePriority priority, CurrentUser actor)
    {
        return GetVisible(actor)
            .Where(r => r.Priority == priority)
            .ToList();
    }

    public ReclamationDto GetByReference(string reference, CurrentUser actor)
    {
        var reclamation = _reclamationRepository.GetByRefernce(reference);
        if (reclamation == null)
            throw new NotFoundException($"Reclamation with reference '{reference}' not found.");

        EnsureCanView(actor, reclamation);
        return ToDtoWithActions(reclamation, actor);
    }

    public ReclamationDto Update(long id, UpdateReclamationDto dto, CurrentUser actor)
    {
        var existing = GetByIdVisible(id, actor);

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
        var updated = _reclamationRepository.Update(existing);
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
        reclamation.SAVId = savId;
        reclamation.SAVName = savName;
        reclamation.AssignedAt = DateTime.UtcNow;
        reclamation.Status = ReclamationStatus.Assigned;
        reclamation.UpdatedAt = DateTime.UtcNow;

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
        reclamation.TechnicianId = dto.TechnicianId;
        reclamation.TechnicianName = string.IsNullOrWhiteSpace(dto.TechnicianName) ? null : dto.TechnicianName;
        reclamation.PlannedStartAt = EnsureUtc(dto.PlannedStartAt);
        reclamation.PlannedEndAt = dto.PlannedEndAt.HasValue ? EnsureUtc(dto.PlannedEndAt.Value) : null;
        reclamation.PlanningNote = dto.PlanningNote;
        reclamation.Status = ReclamationStatus.Planned;
        reclamation.UpdatedAt = DateTime.UtcNow;

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
        reclamation.Status = ReclamationStatus.InProgress;
        reclamation.UpdatedAt = DateTime.UtcNow;

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
        reclamation.ResolutionNote = dto.ResolutionNote;
        reclamation.ResolvedAt = DateTime.UtcNow;
        reclamation.Status = ReclamationStatus.Resolved;
        reclamation.UpdatedAt = DateTime.UtcNow;

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
        reclamation.Status = ReclamationStatus.Closed;
        reclamation.ClosedAt = DateTime.UtcNow;
        reclamation.UpdatedAt = DateTime.UtcNow;

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
        reclamation.Status = ReclamationStatus.Cancelled;
        reclamation.CancelledAt = DateTime.UtcNow;
        reclamation.UpdatedAt = DateTime.UtcNow;

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
        reclamation.Status = ReclamationStatus.Rejected;
        reclamation.RejectedAt = DateTime.UtcNow;
        reclamation.RejectionReason = dto.Reason;
        reclamation.UpdatedAt = DateTime.UtcNow;

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
        return ToDtoWithActions(updated, actor);
    }

    public List<ReclamationHistoryDto> GetHistory(long id, CurrentUser actor)
    {
        // Ensures view permission
        _ = GetByIdVisible(id, actor);

        return _historyRepository
            .GetByReclamationId(id)
            .Select(h => h.ToDto())
            .ToList();
    }

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
}



