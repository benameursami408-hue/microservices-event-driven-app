using Microsoft.EntityFrameworkCore;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;
using ReclamationService.Infrastructure.Data;

namespace ReclamationService.Infrastructure.Repositories;

public class AiPriorityAnalysisRepository : IAiPriorityAnalysisRepository
{
    private readonly AppDbContext _context;
    public AiPriorityAnalysisRepository(AppDbContext context) => _context = context;

    public async Task<AiPriorityAnalysis> AddAsync(AiPriorityAnalysis analysis, CancellationToken cancellationToken = default)
    {
        await _context.Set<AiPriorityAnalysis>().AddAsync(analysis, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return analysis;
    }

    public Task<AiPriorityAnalysis?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Set<AiPriorityAnalysis>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<AiPriorityAnalysis?> GetLatestForReclamationAsync(long reclamationId, CancellationToken cancellationToken = default)
    {
        return _context.Set<AiPriorityAnalysis>()
            .Where(x => x.ReclamationId == reclamationId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
}
