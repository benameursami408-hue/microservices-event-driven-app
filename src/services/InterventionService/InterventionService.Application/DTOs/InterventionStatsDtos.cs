using InterventionService.Domain.Enums;

namespace InterventionService.Application.DTOs;

public class GlobalInterventionStatsDto
{
    public int TotalInterventions { get; set; }
    public int PlannedInterventions { get; set; }
    public int InProgressInterventions { get; set; }
    public int CompletedInterventions { get; set; }
    public int CancelledInterventions { get; set; }
    public List<InterventionStatusCountDto> ByStatus { get; set; } = new();
}

public class InterventionStatusCountDto
{
    public InterventionStatus Status { get; set; }
    public int Count { get; set; }
}

public class TechnicianStatsDto
{
    public long TechnicianId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int CompletedCount { get; set; }
    public int InProgressCount { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime? LastInterventionAt { get; set; }
}
