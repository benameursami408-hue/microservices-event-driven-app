using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class TechnicianAssignedConsumer : IConsumer<TechnicianAssignedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<TechnicianAssignedConsumer> _logger;

    public TechnicianAssignedConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<TechnicianAssignedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TechnicianAssignedEvent> context)
    {
        _logger.LogInformation("Consuming TechnicianAssignedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleTechnicianAssignedAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
