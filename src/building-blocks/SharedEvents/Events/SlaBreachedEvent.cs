namespace SharedEvents.Events;

public record SlaBreachedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(SlaBreachedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "ReclamationService";

    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public long ClientId { get; init; }
    public string ClientEmail { get; init; } = string.Empty;
    public long? SavId { get; init; }
    public string? SavName { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string SlaTarget { get; init; } = string.Empty;
    public DateTime DeadlineAt { get; init; }
    public DateTime BreachedAt { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
