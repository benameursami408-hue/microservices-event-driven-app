using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class InterventionStartedConsumer : IConsumer<InterventionStartedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<InterventionStartedConsumer> _logger;

    public InterventionStartedConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<InterventionStartedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InterventionStartedEvent> context)
    {
        _logger.LogInformation("Consuming InterventionStartedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleInterventionStartedAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
