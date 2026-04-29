using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;
using InterventionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InterventionService.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _dbContext;

    public AppointmentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Appointments
            .Include(x => x.Assignments.OrderByDescending(a => a.AssignedAt))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<Appointment>> QueryAsync(
        long? reclamationId = null,
        long? technicianId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Appointments
            .AsNoTracking()
            .AsQueryable();

        if (reclamationId.HasValue)
        {
            query = query.Where(x => x.ReclamationId == reclamationId.Value);
        }

        if (technicianId.HasValue)
        {
            query = query.Where(x => x.TechnicianId == technicianId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.StartAt <= to.Value);
        }

        return query.OrderBy(x => x.StartAt).ToListAsync(cancellationToken);
    }

    public Task<List<Appointment>> GetTechnicianActiveAppointmentsAsync(
        long technicianId,
        DateTime from,
        DateTime to,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Appointments
            .AsNoTracking()
            .Where(x =>
                x.TechnicianId == technicianId
                && x.Status != AppointmentStatus.Cancelled
                && (!excludeAppointmentId.HasValue || x.Id != excludeAppointmentId.Value)
                && x.StartAt < to
                && (x.EndAt ?? x.StartAt.AddMinutes(x.EstimatedDurationMinutes)) > from);

        return query.OrderBy(x => x.StartAt).ToListAsync(cancellationToken);
    }

    public Task<Appointment?> GetActiveConfirmedByReclamationIdAsync(long reclamationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Appointments
            .FirstOrDefaultAsync(
                x => x.ReclamationId == reclamationId
                    && x.Status == AppointmentStatus.Confirmed,
                cancellationToken);
    }

    public async Task AddAsync(Appointment entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Appointments.AddAsync(entity, cancellationToken);
    }

    public async Task AddAssignmentAsync(Assignment entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Assignments.AddAsync(entity, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
