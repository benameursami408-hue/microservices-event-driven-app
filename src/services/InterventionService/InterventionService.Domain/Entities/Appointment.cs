using System.ComponentModel.DataAnnotations;
using InterventionService.Domain.Enums;

namespace InterventionService.Domain.Entities;

public class Appointment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PlanningRequestId { get; set; }

    public PlanningRequest? PlanningRequest { get; set; }

    [Required]
    public long ReclamationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public int EstimatedDurationMinutes { get; set; } = 90;

    [MaxLength(100)]
    public string TimeZone { get; set; } = "UTC";

    public long? TechnicianId { get; set; }

    [MaxLength(100)]
    public string? TechnicianName { get; set; }

    public bool CustomerPresenceRequired { get; set; } = true;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Proposed;

    public int Sequence { get; set; } = 1;

    [MaxLength(50)]
    public string? CancelReasonCode { get; set; }

    [MaxLength(500)]
    public string? CancelReasonText { get; set; }

    [MaxLength(500)]
    public string? PlanningNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Assignment> Assignments { get; set; } = new();
}
