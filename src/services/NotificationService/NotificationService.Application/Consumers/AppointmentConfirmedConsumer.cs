using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class AppointmentConfirmedConsumer : IConsumer<AppointmentConfirmedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<AppointmentConfirmedConsumer> _logger;

    public AppointmentConfirmedConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<AppointmentConfirmedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AppointmentConfirmedEvent> context)
    {
        _logger.LogInformation("Consuming AppointmentConfirmedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleAppointmentConfirmedAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
