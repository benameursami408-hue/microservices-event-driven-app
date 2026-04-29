using MassTransit;
using Microsoft.Extensions.Logging;
using ReclamationService.Application.Services;
using SharedEvents.Events;

namespace ReclamationService.Application.Consumers;

public class ReplanningRequiredConsumer : IConsumer<ReplanningRequiredEvent>
{
    private readonly InterventionProjectionService _service;
    private readonly ILogger<ReplanningRequiredConsumer> _logger;

    public ReplanningRequiredConsumer(InterventionProjectionService service, ILogger<ReplanningRequiredConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReplanningRequiredEvent> context)
    {
        _logger.LogInformation("Consuming ReplanningRequiredEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _service.ApplyAsync(context.Message, context.CancellationToken);
    }
}
