using AuthService.Application.Outbox;
using AuthService.Infrastructure.Data;
using SharedEvents.Events;

namespace AuthService.Infrastructure.Outbox;

public class EfOutboxWriter : IOutboxWriter
{
    private readonly AppDbContext _dbContext;

    public EfOutboxWriter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default)
    {
        var entry = OutboxMessage.FromIntegrationEvent(evt);
        _dbContext.OutboxMessages.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
