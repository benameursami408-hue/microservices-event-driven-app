namespace SharedEvents.Events;

public record PlanningRequestedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(PlanningRequestedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "ReclamationService";

    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public long ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string ClientEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? ServiceAddress { get; init; }
    public long SavId { get; init; }
    public string SavName { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string? ProductName { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
