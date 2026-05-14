using InterventionService.Domain.Entities;

namespace InterventionService.Domain.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Appointment>> QueryAsync(long? reclamationId = null, long? technicianId = null, long? clientId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<List<Appointment>> GetTechnicianActiveAppointmentsAsync(long technicianId, DateTime from, DateTime to, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default);
    Task<Appointment?> GetActiveConfirmedByReclamationIdAsync(long reclamationId, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment entity, CancellationToken cancellationToken = default);
    Task AddAssignmentAsync(Assignment entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
