using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class ReclamationDto
{
    public long Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketCategory Category { get; set; }
    public string? CategoryReason { get; set; }
    public DateTime? CategoryUpdatedAt { get; set; }
    public NamePriority Priority { get; set; }
    public NamePriority Severity { get; set; }
    public int PriorityScore { get; set; }
    public List<string> PriorityReasons { get; set; } = new();
    public PrioritySource PrioritySource { get; set; }
    public DateTime? PriorityUpdatedAt { get; set; }
    public bool ManualPriorityOverride { get; set; }
    public string? ManualPriorityOverrideReason { get; set; }
    public bool IsBlocking { get; set; }
    public int FollowUpCount { get; set; }
    public DateTime? FirstResponseDeadline { get; set; }
    public DateTime? PlanningDeadline { get; set; }
    public DateTime? ResolutionDeadline { get; set; }
    public SlaStatus SlaStatus { get; set; }
    public DateTime? SlaBreachedAt { get; set; }
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
    public DateTime? NextAppointmentAt { get; set; }
    public DateTime? NextAppointmentEndAt { get; set; }
    public string? PlanningNote { get; set; }
    public bool RequiresReplanning { get; set; }
    public string? LastInterventionOutcome { get; set; }
    public string? LastInterventionReportSummary { get; set; }

    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? ProductName { get; set; }
    public string? Barcode { get; set; }
    public string? ProductImageUrl { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? ProductReference { get; set; }
    public string? SellerName { get; set; }
    public string? PurchaseProofUrl { get; set; }
    public List<string> AllowedActions { get; set; } = new();
}
