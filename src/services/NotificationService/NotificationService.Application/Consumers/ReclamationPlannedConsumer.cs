using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class ReclamationPlannedConsumer : IConsumer<ReclamationPlannedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<ReclamationPlannedConsumer> _logger;

    public ReclamationPlannedConsumer(
        NotificationWorkflow workflow,
        IdempotentConsumerRunner runner,
        ILogger<ReclamationPlannedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReclamationPlannedEvent> context)
    {
        _logger.LogInformation(
            "Consuming ReclamationPlannedEvent ReclamationId={ReclamationId} TechnicianId={TechnicianId}",
            context.Message.ReclamationId,
            context.Message.TechnicianId);

        await _runner.RunAsync(
            context.Message,
            () => _workflow.HandleReclamationPlannedAsync(context.Message, context.CancellationToken),
            context.CancellationToken);
    }
}
