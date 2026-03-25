using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class ReclamationDto
{
    public long Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NamePriority Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public long SAVId { get; set; }
    public string SAVName { get; set; } = string.Empty;
}
