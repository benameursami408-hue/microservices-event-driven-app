using MassTransit;
using Microsoft.Extensions.Logging;
using ReclamationService.Application.Services;
using SharedEvents.Events;

namespace ReclamationService.Application.Consumers;

public class TechnicianAssignedConsumer : IConsumer<TechnicianAssignedEvent>
{
    private readonly InterventionProjectionService _service;
    private readonly ILogger<TechnicianAssignedConsumer> _logger;

    public TechnicianAssignedConsumer(InterventionProjectionService service, ILogger<TechnicianAssignedConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TechnicianAssignedEvent> context)
    {
        _logger.LogInformation("Consuming TechnicianAssignedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _service.ApplyAsync(context.Message, context.CancellationToken);
    }
}
