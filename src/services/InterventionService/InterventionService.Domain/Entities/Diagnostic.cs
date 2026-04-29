using System.ComponentModel.DataAnnotations;

namespace InterventionService.Domain.Entities;

public class Diagnostic
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InterventionId { get; set; }

    [MaxLength(80)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? RootCause { get; set; }

    public bool RequiresParts { get; set; }

    public bool RequiresFollowUp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
