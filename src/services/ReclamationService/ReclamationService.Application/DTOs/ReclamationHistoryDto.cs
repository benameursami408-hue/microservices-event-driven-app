using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class ReclamationHistoryDto
{
    public long Id { get; set; }
    public ReclamationStatus FromStatus { get; set; }
    public ReclamationStatus ToStatus { get; set; }
    public long ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime OccurredAt { get; set; }
}
