using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Services;

public class EventIdempotencyStore : IEventIdempotencyStore
{
    private readonly AppDbContext _dbContext;

    public EventIdempotencyStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessedIntegrationEvents
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default)
    {
        _dbContext.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEvent
        {
            EventId = eventId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
