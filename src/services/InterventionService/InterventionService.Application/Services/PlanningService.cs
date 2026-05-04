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

}
