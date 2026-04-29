namespace SharedEvents.Events;

public record ReclamationClassifiedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(ReclamationClassifiedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "ReclamationService";

    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public long ActorUserId { get; init; }
    public string ActorRole { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
