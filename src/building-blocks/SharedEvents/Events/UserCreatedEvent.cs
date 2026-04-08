namespace SharedEvents.Events;

/// <summary>
/// Published by AuthService when a new user is successfully registered.
/// Consumed by ReclamationService to register the user as an internal client.
/// </summary>
public record UserCreatedEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = nameof(UserCreatedEvent);
    public int EventVersion { get; init; } = 1;
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Producer { get; init; } = "AuthService";

    /// <summary>The Auth DB primary key of the user.</summary>
    public long UserId { get; init; }

    public string FirstName { get; init; } = string.Empty;
    public string LastName  { get; init; } = string.Empty;
    public string Email     { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>The role string (CLIENT, SAV, ADMIN, ST).</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the event was raised.</summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
