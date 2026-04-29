using MassTransit;
using Microsoft.Extensions.Logging;
using ReclamationService.Application.Services;
using SharedEvents.Events;

namespace ReclamationService.Application.Consumers;

public class InterventionStartedConsumer : IConsumer<InterventionStartedEvent>
{
    private readonly InterventionProjectionService _service;
    private readonly ILogger<InterventionStartedConsumer> _logger;

    public InterventionStartedConsumer(InterventionProjectionService service, ILogger<InterventionStartedConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InterventionStartedEvent> context)
    {
        _logger.LogInformation("Consuming InterventionStartedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _service.ApplyAsync(context.Message, context.CancellationToken);
    }
}
