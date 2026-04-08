using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Domain.Entities;

public class ReclamationHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public long ReclamationId { get; set; }

    [Required]
    public ReclamationStatus FromStatus { get; set; }

    [Required]
    public ReclamationStatus ToStatus { get; set; }

    [Required]
    public long ActorUserId { get; set; }

    [MaxLength(30)]
    public string ActorRole { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Comment { get; set; }

    [Required]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Reclamation? Reclamation { get; set; }
}
