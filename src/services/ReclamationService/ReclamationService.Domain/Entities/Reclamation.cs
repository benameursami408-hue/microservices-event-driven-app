using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Domain.Entities
{
    public class Reclamation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Reference { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
        public string Description { get; set; }

        [Required]
        [EnumDataType(typeof(NamePriority))]
        public NamePriority Priority { get; set; }

        [Required]
        [EnumDataType(typeof(NamePriority))]
        public NamePriority Severity { get; set; } = NamePriority.MEDUIM;

        [Required]
        [EnumDataType(typeof(TicketCategory))]
        public TicketCategory Category { get; set; } = TicketCategory.Other;

        [StringLength(250)]
        public string? CategoryReason { get; set; }

        public DateTime? CategoryUpdatedAt { get; set; }

        public int PriorityScore { get; set; }

        [StringLength(2000)]
        public string? PriorityReasons { get; set; }

        [Required]
        [EnumDataType(typeof(PrioritySource))]
        public PrioritySource PrioritySource { get; set; } = PrioritySource.Rules;

        public DateTime? PriorityUpdatedAt { get; set; }

        public bool ManualPriorityOverride { get; set; }

        [StringLength(500)]
        public string? ManualPriorityOverrideReason { get; set; }

        public bool IsBlocking { get; set; }

        public int FollowUpCount { get; set; }

        public DateTime? FirstResponseDeadline { get; set; }
        public DateTime? PlanningDeadline { get; set; }
        public DateTime? ResolutionDeadline { get; set; }

        [Required]
        [EnumDataType(typeof(SlaStatus))]
        public SlaStatus SlaStatus { get; set; } = SlaStatus.OnTrack;

        public DateTime? SlaBreachedAt { get; set; }

        [Required]
        [EnumDataType(typeof(ReclamationStatus))]
        public ReclamationStatus Status { get; set; } = ReclamationStatus.Open;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public long ClientId { get; set; }

        [Required]
        [StringLength(100)]
        public string ClientName { get; set; }

        public long? SAVId { get; set; }

        [StringLength(100)]
        public string? SAVName { get; set; }

        public DateTime? AssignedAt { get; set; }

        public long? ClaimedBySavId { get; set; }

        [StringLength(100)]
        public string? ClaimedBySavName { get; set; }

        public DateTime? ClaimedAt { get; set; }

        public DateTime? ReleasedAt { get; set; }

        public long? TechnicianId { get; set; }

        [StringLength(100)]
        public string? TechnicianName { get; set; }

        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }

        [StringLength(500)]
        public string? PlanningNote { get; set; }

        public DateTime? PlanningRequestedAt { get; set; }

        public bool RequiresReplanning { get; set; }

        [StringLength(40)]
        public string? LastInterventionOutcome { get; set; }

        [StringLength(2000)]
        public string? LastInterventionReportSummary { get; set; }

        [StringLength(2000)]
        public string? ResolutionNote { get; set; }

        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        [StringLength(150)]
        public string? ProductName { get; set; }

        [StringLength(64)]
        public string? Barcode { get; set; }

        [StringLength(500)]
        public string? ProductImageUrl { get; set; }

        public DateTime? PurchaseDate { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? ProductReference { get; set; }

        [StringLength(150)]
        public string? SellerName { get; set; }

        [StringLength(500)]
        public string? PurchaseProofUrl { get; set; }

        public List<ReclamationHistory> History { get; set; } = new();

        // Constructeur vide requis par EF Core
        public Reclamation() { }
    }
}
