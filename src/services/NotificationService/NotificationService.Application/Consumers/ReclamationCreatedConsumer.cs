using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class ReclamationCreatedConsumer : IConsumer<ReclamationCreatedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<ReclamationCreatedConsumer> _logger;

    public ReclamationCreatedConsumer(
        NotificationWorkflow workflow,
        IdempotentConsumerRunner runner,
        ILogger<ReclamationCreatedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReclamationCreatedEvent> context)
    {
        _logger.LogInformation(
            "Consuming ReclamationCreatedEvent ReclamationId={ReclamationId} Reference={Reference}",
            context.Message.ReclamationId,
            context.Message.Reference);

        await _runner.RunAsync(
            context.Message,
            () => _workflow.HandleReclamationCreatedAsync(context.Message, context.CancellationToken),
            context.CancellationToken);
    }
}
