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

        public long? TechnicianId { get; set; }

        [StringLength(100)]
        public string? TechnicianName { get; set; }

        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }

        [StringLength(500)]
        public string? PlanningNote { get; set; }

        [StringLength(2000)]
        public string? ResolutionNote { get; set; }

        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public List<ReclamationHistory> History { get; set; } = new();

        // Constructeur vide requis par EF Core
        public Reclamation() { }
    }
}
