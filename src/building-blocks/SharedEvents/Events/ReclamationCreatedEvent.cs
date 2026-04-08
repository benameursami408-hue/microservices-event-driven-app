namespace SharedEvents.Events;

/// <summary>
/// Published by ReclamationService when a new reclamation (complaint) is created.
/// Consumed by NotificationService (and potentially other services) to react asynchronously.
/// </summary>
public record ReclamationCreatedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(ReclamationCreatedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "ReclamationService";

    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;

    public long ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string ClientEmail { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
