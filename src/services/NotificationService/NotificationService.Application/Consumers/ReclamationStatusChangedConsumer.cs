using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class ReclamationStatusChangedConsumer : IConsumer<ReclamationStatusChangedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<ReclamationStatusChangedConsumer> _logger;

    public ReclamationStatusChangedConsumer(
        NotificationWorkflow workflow,
        IdempotentConsumerRunner runner,
        ILogger<ReclamationStatusChangedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReclamationStatusChangedEvent> context)
    {
        _logger.LogInformation(
            "Consuming ReclamationStatusChangedEvent ReclamationId={ReclamationId} {From}->{To}",
            context.Message.ReclamationId,
            context.Message.FromStatus,
            context.Message.ToStatus);

        await _runner.RunAsync(
            context.Message,
            () => _workflow.HandleReclamationStatusChangedAsync(context.Message, context.CancellationToken),
            context.CancellationToken);
    }
}
