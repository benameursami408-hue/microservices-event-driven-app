using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ReclamationService.Infrastructure.Data;

namespace ReclamationService.Api.Infrastructure;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;
    private const int MaxRetries = 15;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reclamation outbox dispatcher loop failed.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                var clrType = Type.GetType(message.ClrType, throwOnError: false);
                if (clrType is null)
                {
                    throw new InvalidOperationException($"Unknown event CLR type: {message.ClrType}");
                }

                var payload = JsonSerializer.Deserialize(message.Payload, clrType);
                if (payload is null)
                {
                    throw new InvalidOperationException($"Failed to deserialize payload for {message.ClrType}");
                }

                await publishEndpoint.Publish(payload, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.RetryCount += 1;
                var error = ex.Message;
                message.LastError = error.Length > 1900 ? error[..1900] : error;
                _logger.LogWarning(
                    ex,
                    "Failed to dispatch reclamation outbox message {EventId} ({EventType}), retry {RetryCount}",
                    message.Id,
                    message.EventType,
                    message.RetryCount);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
