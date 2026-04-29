namespace SharedEvents.Events;

public record AppointmentCancelledEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(AppointmentCancelledEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "InterventionService";

    public Guid AppointmentId { get; init; }
    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = string.Empty;
    public string? ReasonText { get; init; }
    public long CancelledByUserId { get; init; }
    public string CancelledByRole { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
