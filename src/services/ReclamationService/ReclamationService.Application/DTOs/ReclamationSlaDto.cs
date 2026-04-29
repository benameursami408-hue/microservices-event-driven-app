using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class ReclamationSlaDto
{
    public long ReclamationId { get; set; }
    public SlaStatus SlaStatus { get; set; }
    public DateTime? FirstResponseDeadline { get; set; }
    public DateTime? PlanningDeadline { get; set; }
    public DateTime? ResolutionDeadline { get; set; }
    public DateTime? SlaBreachedAt { get; set; }
    public string? ActiveTarget { get; set; }
    public DateTime? ActiveDeadline { get; set; }
}
