using MassTransit;
using Microsoft.Extensions.Logging;
using ReclamationService.Application.Services;
using SharedEvents.Events;

namespace ReclamationService.Application.Consumers;

public class AppointmentRescheduledConsumer : IConsumer<AppointmentRescheduledEvent>
{
    private readonly InterventionProjectionService _service;
    private readonly ILogger<AppointmentRescheduledConsumer> _logger;

    public AppointmentRescheduledConsumer(InterventionProjectionService service, ILogger<AppointmentRescheduledConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AppointmentRescheduledEvent> context)
    {
        _logger.LogInformation("Consuming AppointmentRescheduledEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _service.ApplyAsync(context.Message, context.CancellationToken);
    }
}
