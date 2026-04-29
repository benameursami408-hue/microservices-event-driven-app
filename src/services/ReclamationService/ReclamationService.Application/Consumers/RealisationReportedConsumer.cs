using MassTransit;
using Microsoft.Extensions.Logging;
using ReclamationService.Application.Services;
using SharedEvents.Events;

namespace ReclamationService.Application.Consumers;

public class RealisationReportedConsumer : IConsumer<RealisationReportedEvent>
{
    private readonly InterventionProjectionService _service;
    private readonly ILogger<RealisationReportedConsumer> _logger;

    public RealisationReportedConsumer(InterventionProjectionService service, ILogger<RealisationReportedConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RealisationReportedEvent> context)
    {
        _logger.LogInformation("Consuming RealisationReportedEvent ReclamationId={ReclamationId}", context.Message.ReclamationId);
        await _service.ApplyAsync(context.Message, context.CancellationToken);
    }
}
