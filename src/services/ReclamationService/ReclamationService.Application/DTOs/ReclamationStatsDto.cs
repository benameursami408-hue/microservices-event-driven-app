using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class ReclamationStatsDto
{
    public ReclamationKpiDto Kpis { get; set; } = new();
    public List<StatusCountDto> ByStatus { get; set; } = new();
    public List<PriorityCountDto> ByPriority { get; set; } = new();
    public List<CategoryCountDto> ByCategory { get; set; } = new();
    public List<TrendPointDto> Trend { get; set; } = new();
    public List<LatestReclamationDto> Latest { get; set; } = new();
}

public class ReclamationKpiDto
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int Assigned { get; set; }
    public int Planned { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Cancelled { get; set; }
    public int Rejected { get; set; }
}

public class StatusCountDto
{
    public ReclamationStatus Status { get; set; }
    public int Count { get; set; }
}

public class PriorityCountDto
{
    public NamePriority Priority { get; set; }
    public int Count { get; set; }
}

public class CategoryCountDto
{
    public TicketCategory Category { get; set; }
    public int Count { get; set; }
}

public class TrendPointDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class LatestReclamationDto
{
    public long Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public TicketCategory Category { get; set; }
    public NamePriority Priority { get; set; }
    public ReclamationStatus Status { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? SavName { get; set; }
    public string? TechnicianName { get; set; }
}
