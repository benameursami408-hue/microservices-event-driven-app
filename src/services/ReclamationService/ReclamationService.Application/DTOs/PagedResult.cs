using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = new();
}

public class ReclamationQueryRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public ReclamationStatus? Status { get; set; }
    public TicketCategory? Category { get; set; }
    public NamePriority? Priority { get; set; }
    public string? Search { get; set; }
}
