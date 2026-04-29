using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;
using InterventionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InterventionService.Infrastructure.Repositories;

public class PlanningRequestRepository : IPlanningRequestRepository
{
    private readonly AppDbContext _dbContext;

    public PlanningRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PlanningRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PlanningRequests
            .Include(x => x.Appointments.OrderByDescending(a => a.StartAt))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<PlanningRequest?> GetActiveByReclamationIdAsync(long reclamationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PlanningRequests
            .Include(x => x.Appointments.OrderByDescending(a => a.StartAt))
            .FirstOrDefaultAsync(
                x => x.ReclamationId == reclamationId
                    && x.Status != PlanningRequestStatus.Cancelled
                    && x.Status != PlanningRequestStatus.Satisfied,
                cancellationToken);
    }

    public Task<List<PlanningRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.PlanningRequests
            .AsNoTracking()
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlanningRequest entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlanningRequests.AddAsync(entity, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
