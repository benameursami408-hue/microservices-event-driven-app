using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class ReclamationDto
{
    public long Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NamePriority Priority { get; set; }
    public ReclamationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public long? SavId { get; set; }
    public string? SavName { get; set; }
    public DateTime? AssignedAt { get; set; }

    public long? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }
    public string? PlanningNote { get; set; }

    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public List<string> AllowedActions { get; set; } = new();
}
