using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class PlanningConflictDetectedConsumer : IConsumer<PlanningConflictDetectedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<PlanningConflictDetectedConsumer> _logger;

    public PlanningConflictDetectedConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<PlanningConflictDetectedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PlanningConflictDetectedEvent> context)
    {
        _logger.LogInformation("Consuming PlanningConflictDetectedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandlePlanningConflictAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
