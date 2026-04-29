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

public class ReclamationsService
{
    private readonly IReclamationRepository _reclamationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IReclamationHistoryRepository _historyRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly TicketClassificationService _classificationService;
    private readonly ReclamationPriorityService _priorityService;
    private readonly ReclamationSlaService _slaService;

    public ReclamationsService(
        IReclamationRepository reclamationRepository,
        IClientRepository clientRepository,
        IReclamationHistoryRepository historyRepository,
        IOutboxWriter outboxWriter,
        TicketClassificationService classificationService,
        ReclamationPriorityService priorityService,
        ReclamationSlaService slaService)
    {
        _reclamationRepository = reclamationRepository;
        _clientRepository = clientRepository;
        _historyRepository = historyRepository;
        _outboxWriter = outboxWriter;
        _classificationService = classificationService;
        _priorityService = priorityService;
        _slaService = slaService;
    }

    public async Task<ReclamationDto> CreateAsync(CreateReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "CLIENT", "ADMIN");

        var clientName = string.IsNullOrWhiteSpace(actor.FullName) ? (actor.Email ?? string.Empty) : actor.FullName;

        var reclamation = dto.ToEntity(actor.UserId, clientName);
        var initialSnapshot = CaptureOperationalState(reclamation);
        ApplyDerivedState(reclamation);
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

        await QueueOperationalEventsAsync(created, initialSnapshot, actor.CorrelationId, actor.UserId, NormalizeRole(actor.Role));

        return ToDtoWithActions(created, actor);
    }

    public List<ReclamationDto> GetVisible(
        CurrentUser actor,
        ReclamationStatus? status = null,
        TicketCategory? category = null)
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

        if (category.HasValue)
        {
            items = items.Where(r => r.Category == category.Value).ToList();
        }

        return items.Select(r => ToDtoWithActions(r, actor)).ToList();
    }

    public PagedResult<ReclamationDto> QueryVisible(
        CurrentUser actor,
        ReclamationStatus? status = null,
        TicketCategory? category = null,
        NamePriority? priority = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        IEnumerable<ReclamationDto> query = GetVisible(actor, status, category);

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
                || x.Category.ToString().ToLowerInvariant().Contains(normalized)
                || (x.CategoryReason ?? string.Empty).ToLowerInvariant().Contains(normalized)
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

    public List<ReclamationDto> GetByCategory(TicketCategory category, CurrentUser actor)
    {
        return GetVisible(actor, category: category);
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



