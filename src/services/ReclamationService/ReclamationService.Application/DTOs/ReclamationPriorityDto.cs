using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class ReclamationPriorityDto
{
    public long ReclamationId { get; set; }
    public NamePriority Priority { get; set; }
    public NamePriority Severity { get; set; }
    public int PriorityScore { get; set; }
    public List<string> PriorityReasons { get; set; } = new();
    public PrioritySource PrioritySource { get; set; }
    public DateTime? PriorityUpdatedAt { get; set; }
    public bool ManualPriorityOverride { get; set; }
    public string? ManualPriorityOverrideReason { get; set; }
}
