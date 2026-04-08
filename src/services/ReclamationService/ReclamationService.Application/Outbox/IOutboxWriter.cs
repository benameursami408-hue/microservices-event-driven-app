using SharedEvents.Events;

namespace ReclamationService.Application.Outbox;

public interface IOutboxWriter
{
    Task EnqueueAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default);
}
