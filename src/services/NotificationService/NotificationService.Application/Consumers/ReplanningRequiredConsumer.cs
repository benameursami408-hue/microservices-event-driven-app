using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class ReplanningRequiredConsumer : IConsumer<ReplanningRequiredEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<ReplanningRequiredConsumer> _logger;

    public ReplanningRequiredConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<ReplanningRequiredConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReplanningRequiredEvent> context)
    {
        _logger.LogInformation("Consuming ReplanningRequiredEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleReplanningRequiredAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
