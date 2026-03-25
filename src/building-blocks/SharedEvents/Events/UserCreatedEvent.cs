namespace SharedEvents.Events;

/// <summary>
/// Published by AuthService when a new user is successfully registered.
/// Consumed by ReclamationService to register the user as an internal client.
/// </summary>
public record UserCreatedEvent
{
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
