using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configuration;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using SharedEvents.Events;

namespace NotificationService.Application.Services;

public class NotificationWorkflow
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationSender _notificationSender;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationWorkflow> _logger;

    public NotificationWorkflow(
        INotificationRepository notificationRepository,
        INotificationSender notificationSender,
        IOptions<NotificationOptions> options,
        ILogger<NotificationWorkflow> logger)
    {
        _notificationRepository = notificationRepository;
        _notificationSender = notificationSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleUserCreatedAsync(UserCreatedEvent message, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Type = "WELCOME",
            Title = "Welcome",
            Message = $"Welcome {message.FirstName} {message.LastName}! Your account has been created.",
            UserId = message.UserId,
            RecipientEmail = message.Email,
            SourceEvent = nameof(UserCreatedEvent),
            CreatedAt = DateTime.UtcNow
        };

        await CreateSendAndPersistAsync(notification, cancellationToken);
    }

    public async Task HandleReclamationCreatedAsync(ReclamationCreatedEvent message, CancellationToken cancellationToken = default)
    {
        // Notify the user (if we have an email)
        var userNotification = new Notification
        {
            Type = "RECLAMATION_CREATED",
            Title = "Reclamation created",
            Message = $"Your reclamation '{message.Reference}' has been created with priority {message.Priority}.",
            UserId = message.ClientId,
            RecipientEmail = string.IsNullOrWhiteSpace(message.ClientEmail) ? null : message.ClientEmail,
            SourceEvent = nameof(ReclamationCreatedEvent),
            CreatedAt = DateTime.UtcNow
        };

        await CreateSendAndPersistAsync(userNotification, cancellationToken);

        // Notify an admin mailbox (configurable)
        if (!string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            var adminNotification = new Notification
            {
                Type = "ADMIN_ALERT",
                Title = "New reclamation",
                Message = $"New reclamation '{message.Reference}' created by {message.ClientName} (ClientId={message.ClientId}).",
                RecipientEmail = _options.AdminEmail,
                SourceEvent = nameof(ReclamationCreatedEvent),
                CreatedAt = DateTime.UtcNow
            };

            await CreateSendAndPersistAsync(adminNotification, cancellationToken);
        }
    }

    private async Task CreateSendAndPersistAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationSender.SendAsync(notification, cancellationToken);
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification Type={Type} Recipient={Recipient}", notification.Type, notification.RecipientEmail);
            notification.Status = NotificationStatus.Failed;
        }

        await _notificationRepository.AddAsync(notification, cancellationToken);
    }
}
