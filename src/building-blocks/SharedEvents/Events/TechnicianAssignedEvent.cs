namespace SharedEvents.Events;

public record TechnicianAssignedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(TechnicianAssignedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "InterventionService";

    public Guid AppointmentId { get; init; }
    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public long TechnicianId { get; init; }
    public string TechnicianName { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
