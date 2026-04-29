using InterventionService.Application.Interfaces;
using InterventionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InterventionService.Infrastructure.Services;

public class EventIdempotencyStore : IEventIdempotencyStore
{
    private readonly AppDbContext _dbContext;

    public EventIdempotencyStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProcessedIntegrationEvents
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
