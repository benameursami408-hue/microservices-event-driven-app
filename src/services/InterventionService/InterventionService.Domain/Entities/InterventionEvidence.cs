using System.ComponentModel.DataAnnotations;

namespace InterventionService.Domain.Entities;

public class InterventionEvidence
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InterventionId { get; set; }

    [MaxLength(40)]
    public string Kind { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public long CapturedByUserId { get; set; }

    [MaxLength(30)]
    public string CapturedByRole { get; set; } = string.Empty;
}
