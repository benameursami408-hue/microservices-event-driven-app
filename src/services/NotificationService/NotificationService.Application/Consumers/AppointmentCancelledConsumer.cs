using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class AppointmentCancelledConsumer : IConsumer<AppointmentCancelledEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<AppointmentCancelledConsumer> _logger;

    public AppointmentCancelledConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<AppointmentCancelledConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AppointmentCancelledEvent> context)
    {
        _logger.LogInformation("Consuming AppointmentCancelledEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleAppointmentCancelledAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
