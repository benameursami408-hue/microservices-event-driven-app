namespace SharedEvents.Events;

/// <summary>
/// Published by ReclamationService when a new reclamation (complaint) is created.
/// Consumed by NotificationService (and potentially other services) to react asynchronously.
/// </summary>
public record ReclamationCreatedEvent
{
    public long ReclamationId { get; init; }
    public string Reference { get; init; } = string.Empty;

    public long ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string ClientEmail { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
