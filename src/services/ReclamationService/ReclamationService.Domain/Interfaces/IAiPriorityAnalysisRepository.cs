using ReclamationService.Domain.Entities;

namespace ReclamationService.Domain.Interfaces;

public interface IAiPriorityAnalysisRepository
{
    Task<AiPriorityAnalysis> AddAsync(AiPriorityAnalysis analysis, CancellationToken cancellationToken = default);
    Task<AiPriorityAnalysis?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AiPriorityAnalysis?> GetLatestForReclamationAsync(long reclamationId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
