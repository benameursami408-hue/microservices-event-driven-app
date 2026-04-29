using InterventionService.Application.Outbox;
using InterventionService.Infrastructure.Data;
using SharedEvents.Events;

namespace InterventionService.Infrastructure.Outbox;

public class EfOutboxWriter : IOutboxWriter
{
    private readonly AppDbContext _dbContext;

    public EfOutboxWriter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default)
    {
        _dbContext.OutboxMessages.Add(OutboxMessage.FromIntegrationEvent(evt));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
