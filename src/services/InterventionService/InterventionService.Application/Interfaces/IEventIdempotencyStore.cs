namespace InterventionService.Application.Interfaces;

public interface IEventIdempotencyStore
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default);
}
