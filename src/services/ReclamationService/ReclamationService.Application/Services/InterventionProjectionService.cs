using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;
using SharedEvents.Events;

namespace ReclamationService.Application.Services;

public class InterventionProjectionService
{
    private readonly IReclamationRepository _reclamationRepository;
    private readonly IReclamationHistoryRepository _historyRepository;

    public InterventionProjectionService(
        IReclamationRepository reclamationRepository,
        IReclamationHistoryRepository historyRepository)
    {
        _reclamationRepository = reclamationRepository;
        _historyRepository = historyRepository;
    }

    public Task ApplyAsync(TechnicianAssignedEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        reclamation.TechnicianId = message.TechnicianId;
        reclamation.TechnicianName = message.TechnicianName;
        reclamation.UpdatedAt = DateTime.UtcNow;
        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, reclamation.Status, reclamation.Status, 0, "SYSTEM", $"Technician assigned: {message.TechnicianName}");
        return Task.CompletedTask;
    }

    public Task ApplyAsync(AppointmentConfirmedEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        var fromStatus = reclamation.Status;
        reclamation.TechnicianId = message.TechnicianId;
        reclamation.TechnicianName = message.TechnicianName;
        reclamation.PlannedStartAt = message.StartAt;
        reclamation.PlannedEndAt = message.EndAt;
        reclamation.RequiresReplanning = false;
        reclamation.Status = ReclamationStatus.Planned;
        reclamation.UpdatedAt = DateTime.UtcNow;
        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, fromStatus, reclamation.Status, 0, "SYSTEM", $"Appointment confirmed for {message.StartAt:u}");
        return Task.CompletedTask;
    }

    public Task ApplyAsync(AppointmentRescheduledEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        reclamation.TechnicianId = message.TechnicianId == 0 ? reclamation.TechnicianId : message.TechnicianId;
        reclamation.TechnicianName = string.IsNullOrWhiteSpace(message.TechnicianName) ? reclamation.TechnicianName : message.TechnicianName;
        reclamation.PlannedStartAt = message.NewStartAt;
        reclamation.PlannedEndAt = message.NewEndAt;
        reclamation.RequiresReplanning = false;
        reclamation.Status = ReclamationStatus.Planned;
        reclamation.UpdatedAt = DateTime.UtcNow;
        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, reclamation.Status, reclamation.Status, 0, "SYSTEM", $"Appointment rescheduled ({message.ReasonCode})");
        return Task.CompletedTask;
    }

    public Task ApplyAsync(AppointmentCancelledEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        var fromStatus = reclamation.Status;
        reclamation.PlannedStartAt = null;
        reclamation.PlannedEndAt = null;
        reclamation.PlanningNote = message.ReasonText;
        reclamation.RequiresReplanning = false;
        if (reclamation.Status == ReclamationStatus.Planned)
        {
            reclamation.Status = ReclamationStatus.Assigned;
        }
        reclamation.UpdatedAt = DateTime.UtcNow;
        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, fromStatus, reclamation.Status, 0, "SYSTEM", $"Appointment cancelled ({message.ReasonCode})");
        return Task.CompletedTask;
    }

    public Task ApplyAsync(InterventionStartedEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        var fromStatus = reclamation.Status;
        reclamation.Status = ReclamationStatus.InProgress;
        reclamation.TechnicianId = message.TechnicianId;
        reclamation.TechnicianName = message.TechnicianName;
        reclamation.UpdatedAt = DateTime.UtcNow;
        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, fromStatus, reclamation.Status, message.TechnicianId, "ST", "Intervention started");
        return Task.CompletedTask;
    }

    public Task ApplyAsync(InterventionCompletedEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        reclamation.LastInterventionOutcome = message.Outcome;
        reclamation.RequiresReplanning = message.NeedsReplanning;
        reclamation.UpdatedAt = DateTime.UtcNow;
        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, reclamation.Status, reclamation.Status, 0, "SYSTEM", $"Intervention completed: {message.Outcome}");
        return Task.CompletedTask;
    }

    public Task ApplyAsync(RealisationReportedEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        var fromStatus = reclamation.Status;
        reclamation.LastInterventionOutcome = message.Outcome;
        reclamation.LastInterventionReportSummary = message.Summary;
        reclamation.ResolutionNote = message.Summary;
        reclamation.RequiresReplanning = message.NeedsReplanning;
        reclamation.UpdatedAt = DateTime.UtcNow;

        if (message.NeedsReplanning)
        {
            reclamation.Status = ReclamationStatus.Assigned;
            reclamation.PlannedStartAt = null;
            reclamation.PlannedEndAt = null;
        }
        else
        {
            reclamation.Status = ReclamationStatus.Resolved;
            reclamation.ResolvedAt = DateTime.UtcNow;
        }

        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, fromStatus, reclamation.Status, 0, "SYSTEM", $"Intervention report published: {message.Outcome}");
        return Task.CompletedTask;
    }

    public Task ApplyAsync(ReplanningRequiredEvent message, CancellationToken cancellationToken = default)
    {
        var reclamation = Get(message.ReclamationId);
        var fromStatus = reclamation.Status;
        reclamation.RequiresReplanning = true;
        reclamation.Status = ReclamationStatus.Assigned;
        reclamation.PlannedStartAt = null;
        reclamation.PlannedEndAt = null;
        reclamation.UpdatedAt = DateTime.UtcNow;
        _reclamationRepository.Update(reclamation);
        AppendHistory(reclamation, fromStatus, reclamation.Status, 0, "SYSTEM", $"Replanning required: {message.ReasonCode}");
        return Task.CompletedTask;
    }

    private Reclamation Get(long id)
    {
        return _reclamationRepository.GetById(id)
            ?? throw new InvalidOperationException($"Reclamation {id} not found for intervention projection.");
    }

    private void AppendHistory(Reclamation reclamation, ReclamationStatus from, ReclamationStatus to, long actorUserId, string actorRole, string comment)
    {
        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = reclamation.Id,
            FromStatus = from,
            ToStatus = to,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Comment = comment,
            OccurredAt = DateTime.UtcNow
        });
    }
}
