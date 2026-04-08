namespace SharedEvents.Events;

/// <summary>
/// Published by ReclamationService when a reclamation is planned (scheduled) for intervention.
/// Consumed by NotificationService (and potentially other services) to react asynchronously.
/// </summary>
public record ReclamationPlannedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(ReclamationPlannedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "ReclamationService";

    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;

    public long ClientId { get; init; }
    public string ClientEmail { get; init; } = string.Empty;

    public long SavId { get; init; }
    public string SavName { get; init; } = string.Empty;

    public long TechnicianId { get; init; }
    public string TechnicianName { get; init; } = string.Empty;

    public DateTime PlannedStartAt { get; init; }
    public DateTime? PlannedEndAt { get; init; }
    public string? PlanningNote { get; init; }

    public long ActorUserId { get; init; }
    public string ActorRole { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
