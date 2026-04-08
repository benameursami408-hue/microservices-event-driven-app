using SharedEvents.Events;

namespace AuthService.Application.Outbox;

public interface IOutboxWriter
{
    Task EnqueueAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default);
}
