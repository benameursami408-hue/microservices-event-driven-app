using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using SharedEvents.Events;

namespace NotificationService.Application.Services;

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
        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = message.CorrelationId,
            ["EventId"] = message.EventId,
            ["EventType"] = message.EventType,
            ["EventVersion"] = message.EventVersion,
            ["Producer"] = message.Producer
        });

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
        _logger.LogInformation("Processed event EventId={EventId} EventType={EventType}", message.EventId, message.EventType);
    }
}
