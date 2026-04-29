using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class RealisationReportedConsumer : IConsumer<RealisationReportedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<RealisationReportedConsumer> _logger;

    public RealisationReportedConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<RealisationReportedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RealisationReportedEvent> context)
    {
        _logger.LogInformation("Consuming RealisationReportedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleRealisationReportedAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
