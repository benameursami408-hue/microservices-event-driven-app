using System.ComponentModel.DataAnnotations;

namespace InterventionService.Domain.Entities;

public class RepairAction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InterventionId { get; set; }

    [MaxLength(80)]
    public string ActionType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool Success { get; set; }
}
