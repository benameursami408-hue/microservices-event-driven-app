using System.ComponentModel.DataAnnotations;
using InterventionService.Domain.Enums;

namespace InterventionService.Domain.Entities;

public class VisitReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InterventionId { get; set; }

    [MaxLength(2000)]
    public string Summary { get; set; } = string.Empty;

    public InterventionOutcome Outcome { get; set; }

    public bool CustomerPresent { get; set; }

    [MaxLength(500)]
    public string? NextStep { get; set; }

    public VisitReportStatus Status { get; set; } = VisitReportStatus.Draft;

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
