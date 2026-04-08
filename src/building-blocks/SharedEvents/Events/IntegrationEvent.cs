namespace SharedEvents.Events;

/// <summary>
/// Common metadata for all integration events exchanged between services.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; init; }
    string EventType { get; init; }
    int EventVersion { get; init; }
    string CorrelationId { get; init; }
    string? CausationId { get; init; }
    string Producer { get; init; }
    DateTime OccurredAt { get; init; }
}
