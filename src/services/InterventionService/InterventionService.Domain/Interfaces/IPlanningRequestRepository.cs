using InterventionService.Domain.Entities;

namespace InterventionService.Domain.Interfaces;

public interface IPlanningRequestRepository
{
    Task<PlanningRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PlanningRequest?> GetActiveByReclamationIdAsync(long reclamationId, CancellationToken cancellationToken = default);
    Task<List<PlanningRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PlanningRequest entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
