using System.ComponentModel.DataAnnotations;
using InterventionService.Domain.Enums;

namespace InterventionService.Domain.Entities;

public class RescheduleRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AppointmentId { get; set; }

    [MaxLength(50)]
    public string ReasonCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ReasonText { get; set; }

    public long RequestedByUserId { get; set; }

    [MaxLength(30)]
    public string RequestedByRole { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public RescheduleRequestStatus Status { get; set; } = RescheduleRequestStatus.Requested;
}
