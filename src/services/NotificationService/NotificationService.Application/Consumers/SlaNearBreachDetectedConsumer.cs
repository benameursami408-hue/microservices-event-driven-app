using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class SlaNearBreachDetectedConsumer : IConsumer<SlaNearBreachDetectedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<SlaNearBreachDetectedConsumer> _logger;

    public SlaNearBreachDetectedConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<SlaNearBreachDetectedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SlaNearBreachDetectedEvent> context)
    {
        _logger.LogInformation("Consuming SlaNearBreachDetectedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleSlaNearBreachAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
