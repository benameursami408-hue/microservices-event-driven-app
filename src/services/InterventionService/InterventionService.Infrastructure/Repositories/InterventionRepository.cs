using InterventionService.Domain.Entities;
using InterventionService.Domain.Interfaces;
using InterventionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InterventionService.Infrastructure.Repositories;

public class InterventionRepository : IInterventionRepository
{
    private readonly AppDbContext _dbContext;

    public InterventionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Intervention?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Interventions
            .Include(x => x.Diagnostics.OrderByDescending(d => d.CreatedAt))
            .Include(x => x.RepairActions)
            .Include(x => x.PartsUsed)
            .Include(x => x.Evidences.OrderByDescending(e => e.CapturedAt))
            .Include(x => x.VisitReports.OrderByDescending(r => r.CreatedAt))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Intervention?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Interventions
            .Include(x => x.Diagnostics.OrderByDescending(d => d.CreatedAt))
            .Include(x => x.RepairActions)
            .Include(x => x.PartsUsed)
            .Include(x => x.Evidences.OrderByDescending(e => e.CapturedAt))
            .Include(x => x.VisitReports.OrderByDescending(r => r.CreatedAt))
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
    }

    public Task<List<Intervention>> QueryAsync(long? reclamationId = null, long? technicianId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Interventions
            .AsNoTracking()
            .Include(x => x.VisitReports.OrderByDescending(r => r.CreatedAt))
            .AsQueryable();

        if (reclamationId.HasValue)
        {
            query = query.Where(x => x.ReclamationId == reclamationId.Value);
        }

        if (technicianId.HasValue)
        {
            query = query.Where(x => x.TechnicianId == technicianId.Value);
        }

        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Intervention entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Interventions.AddAsync(entity, cancellationToken);
    }

    public async Task AddDiagnosticAsync(Diagnostic entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Diagnostics.AddAsync(entity, cancellationToken);
    }

    public async Task AddRepairActionAsync(RepairAction entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.RepairActions.AddAsync(entity, cancellationToken);
    }

    public async Task AddPartUsedAsync(PartUsed entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.PartsUsed.AddAsync(entity, cancellationToken);
    }

    public async Task AddEvidenceAsync(InterventionEvidence entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.InterventionEvidences.AddAsync(entity, cancellationToken);
    }

    public async Task AddVisitReportAsync(VisitReport entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.VisitReports.AddAsync(entity, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
