using System.ComponentModel.DataAnnotations;

namespace NotificationService.Infrastructure.Data;

public class ProcessedIntegrationEvent
{
    [Key]
    public Guid EventId { get; set; }

    [Required]
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
