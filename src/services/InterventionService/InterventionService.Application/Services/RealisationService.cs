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
        if (NormalizeRole(actor.Role) == "ST" && !technicianId.HasValue)
        {
            technicianId = actor.UserId;
        }

        var items = await _interventionRepository.QueryAsync(reclamationId, technicianId, cancellationToken);
        return items.Select(x => ToDto(x, actor)).ToList();
    }

    public async Task<InterventionDto?> GetInterventionAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var item = await _interventionRepository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : ToDto(item, actor);
    }

}
