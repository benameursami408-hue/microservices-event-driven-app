namespace SharedEvents.Events;

public record InterventionCompletedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(InterventionCompletedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "InterventionService";

    public Guid InterventionId { get; init; }
    public long ReclamationId { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public bool NeedsReplanning { get; init; }
    public DateTime CompletedAt { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
