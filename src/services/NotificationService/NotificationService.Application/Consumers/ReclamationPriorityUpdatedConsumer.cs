using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class ReclamationPriorityUpdatedConsumer : IConsumer<ReclamationPriorityUpdatedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<ReclamationPriorityUpdatedConsumer> _logger;

    public ReclamationPriorityUpdatedConsumer(
        NotificationWorkflow workflow,
        IdempotentConsumerRunner runner,
        ILogger<ReclamationPriorityUpdatedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReclamationPriorityUpdatedEvent> context)
    {
        _logger.LogInformation(
            "Consuming ReclamationPriorityUpdatedEvent ReclamationId={ReclamationId} Priority={Priority}",
            context.Message.ReclamationId,
            context.Message.Priority);

        await _runner.RunAsync(
            context.Message,
            () => _workflow.HandleReclamationPriorityUpdatedAsync(context.Message, context.CancellationToken),
            context.CancellationToken);
    }
}
