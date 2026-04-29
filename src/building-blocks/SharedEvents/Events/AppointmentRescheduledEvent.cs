namespace SharedEvents.Events;

public record AppointmentRescheduledEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(AppointmentRescheduledEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "InterventionService";

    public Guid AppointmentId { get; init; }
    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public DateTime OldStartAt { get; init; }
    public DateTime? OldEndAt { get; init; }
    public DateTime NewStartAt { get; init; }
    public DateTime? NewEndAt { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public long TechnicianId { get; init; }
    public string TechnicianName { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = string.Empty;
    public string? ReasonText { get; init; }
    public int Sequence { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
