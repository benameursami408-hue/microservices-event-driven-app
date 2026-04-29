using InterventionService.Domain.Entities;

namespace InterventionService.Domain.Interfaces;

public interface IInterventionRepository
{
    Task<Intervention?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Intervention?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<List<Intervention>> QueryAsync(long? reclamationId = null, long? technicianId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Intervention entity, CancellationToken cancellationToken = default);
    Task AddDiagnosticAsync(Diagnostic entity, CancellationToken cancellationToken = default);
    Task AddRepairActionAsync(RepairAction entity, CancellationToken cancellationToken = default);
    Task AddPartUsedAsync(PartUsed entity, CancellationToken cancellationToken = default);
    Task AddEvidenceAsync(InterventionEvidence entity, CancellationToken cancellationToken = default);
    Task AddVisitReportAsync(VisitReport entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
