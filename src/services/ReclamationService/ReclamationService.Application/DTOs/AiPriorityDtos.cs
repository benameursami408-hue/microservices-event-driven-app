using System.ComponentModel.DataAnnotations;

namespace ReclamationService.Application.DTOs;

public class AnalyzePriorityRequestDto
{
    [Required]
    [Range(1, long.MaxValue)]
    public long ReclamationId { get; set; }
    public string? Reference { get; set; }
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(150)] public string? ProductName { get; set; }
    [MaxLength(100)] public string? Brand { get; set; }
    [MaxLength(100)] public string? Model { get; set; }
    [MaxLength(50)] public string? CurrentPriority { get; set; }
    [MaxLength(100)] public string? ClientImpact { get; set; }
}

public class AiPriorityAnalysisDto
{
    public long? Id { get; set; }
    public long? AnalysisId { get; set; }
    [Required]
    [Range(1, long.MaxValue)]
    public long ReclamationId { get; set; }
    public string SuggestedPriority { get; set; } = "Low";
    public int ConfidenceScore { get; set; }
    public string SlaRisk { get; set; } = "Low";
    public string Reason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> DetectedKeywords { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public long? AcceptedByUserId { get; set; }
}

public class ApplyAiPriorityDto
{
    [Range(1, long.MaxValue)]
    public long? AnalysisId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}
