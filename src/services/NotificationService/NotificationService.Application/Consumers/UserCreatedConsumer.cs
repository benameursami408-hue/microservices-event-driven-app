using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using SharedEvents.Events;

namespace NotificationService.Application.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly NotificationWorkflow _workflow;
    private readonly IdempotentConsumerRunner _runner;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(
        NotificationWorkflow workflow,
        IdempotentConsumerRunner runner,
        ILogger<UserCreatedConsumer> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        _logger.LogInformation("Consuming UserCreatedEvent for UserId={UserId} Email={Email}", context.Message.UserId, context.Message.Email);
        await _runner.RunAsync(
            context.Message,
            () => _workflow.HandleUserCreatedAsync(context.Message, context.CancellationToken),
            context.CancellationToken);
    }
}
