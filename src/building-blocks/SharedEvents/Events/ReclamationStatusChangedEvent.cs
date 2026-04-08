namespace SharedEvents.Events;

/// <summary>
/// Published by ReclamationService when a reclamation status changes (start/resolve/close/cancel/reject...).
/// Consumed by NotificationService (and potentially other services) to react asynchronously.
/// </summary>
public record ReclamationStatusChangedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(ReclamationStatusChangedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "ReclamationService";

    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;

    public long ClientId { get; init; }
    public string ClientEmail { get; init; } = string.Empty;

    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;

    public string? Comment { get; init; }

    public long ActorUserId { get; init; }
    public string ActorRole { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
