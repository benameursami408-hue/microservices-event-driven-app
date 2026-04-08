using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class ReclamationAssignedConsumer : IConsumer<ReclamationAssignedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<ReclamationAssignedConsumer> _logger;

    public ReclamationAssignedConsumer(
        NotificationWorkflow workflow,
        IdempotentConsumerRunner runner,
        ILogger<ReclamationAssignedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReclamationAssignedEvent> context)
    {
        _logger.LogInformation(
            "Consuming ReclamationAssignedEvent ReclamationId={ReclamationId} SavId={SavId}",
            context.Message.ReclamationId,
            context.Message.SavId);

        await _runner.RunAsync(
            context.Message,
            () => _workflow.HandleReclamationAssignedAsync(context.Message, context.CancellationToken),
            context.CancellationToken);
    }
}
