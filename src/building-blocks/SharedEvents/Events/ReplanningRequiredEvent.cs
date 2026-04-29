namespace SharedEvents.Events;

public record ReplanningRequiredEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(ReplanningRequiredEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "InterventionService";

    public Guid InterventionId { get; init; }
    public long ReclamationId { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string? ReasonText { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
