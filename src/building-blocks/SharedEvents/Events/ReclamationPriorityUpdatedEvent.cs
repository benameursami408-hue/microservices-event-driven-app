namespace SharedEvents.Events;

public record ReclamationPriorityUpdatedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(ReclamationPriorityUpdatedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "ReclamationService";

    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public int PriorityScore { get; init; }
    public string PrioritySource { get; init; } = string.Empty;
    public List<string> Reasons { get; init; } = new();
    public long ActorUserId { get; init; }
    public string ActorRole { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
