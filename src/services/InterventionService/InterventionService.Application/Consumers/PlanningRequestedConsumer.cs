using InterventionService.Application.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedEvents.Events;

namespace InterventionService.Application.Consumers;

public class PlanningRequestedConsumer : IConsumer<PlanningRequestedEvent>
{
    private readonly PlanningService _planningService;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<PlanningRequestedConsumer> _logger;

    public PlanningRequestedConsumer(
        PlanningService planningService,
        IdempotentConsumerRunner runner,
        ILogger<PlanningRequestedConsumer> logger)
    {
        _planningService = planningService;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PlanningRequestedEvent> context)
    {
        _logger.LogInformation(
            "Consuming PlanningRequestedEvent ReclamationId={ReclamationId} Reference={Reference}",
            context.Message.ReclamationId,
            context.Message.Reference);

        await _runner.RunAsync(
            context.Message,
            async () => { await _planningService.SyncPlanningRequestedAsync(context.Message, context.CancellationToken); },
            context.CancellationToken);
    }
}
