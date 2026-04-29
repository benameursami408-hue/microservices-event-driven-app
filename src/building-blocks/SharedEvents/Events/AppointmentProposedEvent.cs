namespace SharedEvents.Events;

public record AppointmentProposedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(AppointmentProposedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "InterventionService";

    public Guid AppointmentId { get; init; }
    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public DateTime StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public long? TechnicianId { get; init; }
    public string? TechnicianName { get; init; }
    public int Sequence { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
