using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class SlaBreachedConsumer : IConsumer<SlaBreachedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<SlaBreachedConsumer> _logger;

    public SlaBreachedConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<SlaBreachedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SlaBreachedEvent> context)
    {
        _logger.LogInformation("Consuming SlaBreachedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleSlaBreachedAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
