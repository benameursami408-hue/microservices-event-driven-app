using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReclamationService.Domain.Entities;

public class AiPriorityAnalysis
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public long ReclamationId { get; set; }
    [MaxLength(50)] public string SuggestedPriority { get; set; } = "Low";
    public int ConfidenceScore { get; set; }
    [MaxLength(50)] public string SlaRisk { get; set; } = "Low";
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(1000)] public string RecommendedAction { get; set; } = string.Empty;
    public string DetectedKeywordsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public long? AcceptedByUserId { get; set; }
    public Reclamation? Reclamation { get; set; }
}
