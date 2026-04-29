using System.ComponentModel.DataAnnotations;
using InterventionService.Domain.Enums;

namespace InterventionService.Domain.Entities;

public class Assignment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AppointmentId { get; set; }

    public Appointment? Appointment { get; set; }

    public long TechnicianId { get; set; }

    [MaxLength(100)]
    public string TechnicianName { get; set; } = string.Empty;

    public long AssignedByUserId { get; set; }

    [MaxLength(30)]
    public string AssignedByRole { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
}
