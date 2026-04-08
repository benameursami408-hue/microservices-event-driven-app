using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SharedEvents.Events;

namespace ReclamationService.Infrastructure.Outbox;

public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(1024)]
    public string ClrType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public int EventVersion { get; set; } = 1;

    [Required]
    [MaxLength(200)]
    public string CorrelationId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CausationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Producer { get; set; } = string.Empty;

    [Required]
    public DateTime OccurredAt { get; set; }

    [Required]
    public string Payload { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    [Required]
    public int RetryCount { get; set; }

    [MaxLength(2000)]
    public string? LastError { get; set; }

    public static OutboxMessage FromIntegrationEvent(IIntegrationEvent evt)
    {
        var runtimeType = evt.GetType();
        return new OutboxMessage
        {
            Id = evt.EventId,
            ClrType = runtimeType.AssemblyQualifiedName ?? runtimeType.FullName ?? runtimeType.Name,
            EventType = evt.EventType,
            EventVersion = evt.EventVersion,
            CorrelationId = string.IsNullOrWhiteSpace(evt.CorrelationId) ? Guid.NewGuid().ToString("N") : evt.CorrelationId,
            CausationId = evt.CausationId,
            Producer = evt.Producer,
            OccurredAt = evt.OccurredAt,
            Payload = JsonSerializer.Serialize(evt, runtimeType),
            CreatedAt = DateTime.UtcNow
        };
    }
}
