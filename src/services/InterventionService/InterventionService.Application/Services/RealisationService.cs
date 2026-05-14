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
    private readonly IInterventionRepository _interventionRepository;
    private readonly IOutboxWriter _outboxWriter;

    public RealisationService(IInterventionRepository interventionRepository, IOutboxWriter outboxWriter)
    {
        _interventionRepository = interventionRepository;
        _outboxWriter = outboxWriter;
    }

    public async Task<List<InterventionDto>> QueryInterventionsAsync(CurrentUser actor, long? reclamationId = null, long? technicianId = null, CancellationToken cancellationToken = default)
    {
        if (IsTechnicianRole(actor.Role))
        {
            technicianId = actor.UserId;
        }

        var items = await _interventionRepository.QueryAsync(reclamationId, technicianId, cancellationToken);
        return items.Select(x => ToDto(x, actor)).ToList();
    }

    public Task<List<InterventionDto>> QueryMyInterventionsAsync(CurrentUser actor, CancellationToken cancellationToken = default)
    {
        return QueryInterventionsAsync(actor, technicianId: actor.UserId, cancellationToken: cancellationToken);
    }

    public async Task<InterventionDto?> GetInterventionAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var item = await _interventionRepository.GetByIdAsync(id, cancellationToken);
        if (item is null) return null;

        if (IsTechnicianRole(actor.Role) && item.TechnicianId != actor.UserId)
        {
            return null;
        }

        return ToDto(item, actor);
    }

    public async Task<InterventionDto> UpdateStatusAsync(Guid id, UpdateInterventionStatusDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var intervention = await GetOwnedAsync(id, actor, cancellationToken);
        intervention.Status = dto.Status;
        if (dto.Status == InterventionStatus.Started) intervention.StartedAt ??= DateTime.UtcNow;
        if (dto.Status == InterventionStatus.Completed) intervention.EndedAt ??= DateTime.UtcNow;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);
        return ToDto(intervention, actor);
    }

}
