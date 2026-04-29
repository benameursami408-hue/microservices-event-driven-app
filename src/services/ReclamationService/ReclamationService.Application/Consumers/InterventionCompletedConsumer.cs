using MassTransit;
using Microsoft.Extensions.Logging;
using ReclamationService.Application.Services;
using SharedEvents.Events;

namespace ReclamationService.Application.Consumers;

public class InterventionCompletedConsumer : IConsumer<InterventionCompletedEvent>
{
    private readonly InterventionProjectionService _service;
    private readonly ILogger<InterventionCompletedConsumer> _logger;

    public InterventionCompletedConsumer(InterventionProjectionService service, ILogger<InterventionCompletedConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InterventionCompletedEvent> context)
    {
        _logger.LogInformation("Consuming InterventionCompletedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _service.ApplyAsync(context.Message, context.CancellationToken);
    }
}
