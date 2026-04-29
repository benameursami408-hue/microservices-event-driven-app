using System.ComponentModel.DataAnnotations;
using InterventionService.Domain.Enums;

namespace InterventionService.Domain.Entities;

public class Intervention
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AppointmentId { get; set; }

    [Required]
    public long ReclamationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    public long TechnicianId { get; set; }

    [MaxLength(100)]
    public string TechnicianName { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public InterventionStatus Status { get; set; } = InterventionStatus.Ready;

    public InterventionOutcome? Outcome { get; set; }

    public bool NeedsReplanning { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Diagnostic> Diagnostics { get; set; } = new();
    public List<RepairAction> RepairActions { get; set; } = new();
    public List<PartUsed> PartsUsed { get; set; } = new();
    public List<InterventionEvidence> Evidences { get; set; } = new();
    public List<VisitReport> VisitReports { get; set; } = new();
}
