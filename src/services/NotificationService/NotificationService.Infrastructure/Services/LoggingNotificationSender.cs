using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Services;

public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _logger;

    public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending notification Type={Type} Recipient={Recipient} Title={Title}",
            notification.Type,
            notification.RecipientEmail,
            notification.Title);

        _logger.LogInformation("Message: {Message}", notification.Message);
        return Task.CompletedTask;
    }
}
