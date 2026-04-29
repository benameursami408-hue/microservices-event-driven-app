using SharedEvents.Events;

namespace InterventionService.Application.Outbox;

public interface IOutboxWriter
{
    Task EnqueueAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default);
}
