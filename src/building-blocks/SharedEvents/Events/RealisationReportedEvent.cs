namespace SharedEvents.Events;

public record RealisationReportedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(RealisationReportedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "InterventionService";

    public Guid InterventionId { get; init; }
    public long ReclamationId { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public bool NeedsReplanning { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? NextStep { get; init; }
    public DateTime PublishedAt { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
