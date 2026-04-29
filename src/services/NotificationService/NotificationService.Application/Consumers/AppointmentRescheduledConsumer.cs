using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class AppointmentRescheduledConsumer : IConsumer<AppointmentRescheduledEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<AppointmentRescheduledConsumer> _logger;

    public AppointmentRescheduledConsumer(NotificationWorkflow workflow, IdempotentConsumerRunner runner, ILogger<AppointmentRescheduledConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AppointmentRescheduledEvent> context)
    {
        _logger.LogInformation("Consuming AppointmentRescheduledEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _runner.RunAsync(context.Message, () => _workflow.HandleAppointmentRescheduledAsync(context.Message, context.CancellationToken), context.CancellationToken);
    }
}
