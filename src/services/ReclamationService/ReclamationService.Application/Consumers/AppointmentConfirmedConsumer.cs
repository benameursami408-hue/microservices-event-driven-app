using MassTransit;
using Microsoft.Extensions.Logging;
using ReclamationService.Application.Services;
using SharedEvents.Events;

namespace ReclamationService.Application.Consumers;

public class AppointmentConfirmedConsumer : IConsumer<AppointmentConfirmedEvent>
{
    private readonly InterventionProjectionService _service;
    private readonly ILogger<AppointmentConfirmedConsumer> _logger;

    public AppointmentConfirmedConsumer(InterventionProjectionService service, ILogger<AppointmentConfirmedConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AppointmentConfirmedEvent> context)
    {
        _logger.LogInformation("Consuming AppointmentConfirmedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _service.ApplyAsync(context.Message, context.CancellationToken);
    }
}
