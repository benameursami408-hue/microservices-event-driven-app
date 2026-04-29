using InterventionService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using SharedEvents.Events;

namespace InterventionService.Application.Services;

public class IdempotentConsumerRunner
{
    private readonly IEventIdempotencyStore _store;
    private readonly ILogger<IdempotentConsumerRunner> _logger;

    public IdempotentConsumerRunner(IEventIdempotencyStore store, ILogger<IdempotentConsumerRunner> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task RunAsync(IIntegrationEvent message, Func<Task> handler, CancellationToken cancellationToken = default)
    {
        if (await _store.HasProcessedAsync(message.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "Skipping already processed event EventId={EventId} EventType={EventType}",
                message.EventId,
                message.EventType);
            return;
        }

        await handler();
        await _store.MarkProcessedAsync(message.EventId, message.EventType, cancellationToken);
    }
}
