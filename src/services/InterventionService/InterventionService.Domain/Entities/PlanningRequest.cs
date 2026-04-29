using System.ComponentModel.DataAnnotations;
using InterventionService.Domain.Enums;

namespace InterventionService.Domain.Entities;

public class PlanningRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public long ReclamationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    public long SavId { get; set; }

    [MaxLength(100)]
    public string SavName { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Priority { get; set; } = string.Empty;

    public long ClientId { get; set; }

    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CustomerEmail { get; set; }

    [MaxLength(50)]
    public string? CustomerPhone { get; set; }

    [MaxLength(300)]
    public string? ServiceAddress { get; set; }

    [MaxLength(150)]
    public string? ProductName { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public PlanningRequestStatus Status { get; set; } = PlanningRequestStatus.Pending;

    public List<Appointment> Appointments { get; set; } = new();
}
