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
            SourceEvent = message.EventType,
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
            SourceEvent = message.EventType,
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
                SourceEvent = message.EventType,
                CreatedAt = DateTime.UtcNow
            };

            await CreateSendAndPersistAsync(adminNotification, cancellationToken);
        }
    }

    public async Task HandleReclamationAssignedAsync(ReclamationAssignedEvent message, CancellationToken cancellationToken = default)
    {
        var clientNotification = new Notification
        {
            Type = "RECLAMATION_ASSIGNED",
            Title = "Reclamation assigned",
            Message = $"Your reclamation '{message.Reference}' has been assigned to {message.SavName}.",
            UserId = message.ClientId,
            RecipientEmail = string.IsNullOrWhiteSpace(message.ClientEmail) ? null : message.ClientEmail,
            SourceEvent = message.EventType,
            CreatedAt = DateTime.UtcNow
        };

        await CreateSendAndPersistAsync(clientNotification, cancellationToken);

        var savNotification = new Notification
        {
            Type = "SAV_ASSIGNMENT",
            Title = "New assigned reclamation",
            Message = $"Reclamation '{message.Reference}' is now assigned to you.",
            UserId = message.SavId,
            RecipientEmail = null,
            SourceEvent = message.EventType,
            CreatedAt = DateTime.UtcNow
        };

        await CreateSendAndPersistAsync(savNotification, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            var adminNotification = new Notification
            {
                Type = "ADMIN_ALERT",
                Title = "Reclamation assigned",
                Message = $"Reclamation '{message.Reference}' assigned to {message.SavName} (SavId={message.SavId}).",
                RecipientEmail = _options.AdminEmail,
                SourceEvent = message.EventType,
                CreatedAt = DateTime.UtcNow
            };

            await CreateSendAndPersistAsync(adminNotification, cancellationToken);
        }
    }

    public async Task HandleReclamationPlannedAsync(ReclamationPlannedEvent message, CancellationToken cancellationToken = default)
    {
        var schedule = message.PlannedEndAt.HasValue
            ? $"{message.PlannedStartAt:u} - {message.PlannedEndAt:u}"
            : $"{message.PlannedStartAt:u}";

        var clientNotification = new Notification
        {
            Type = "RECLAMATION_PLANNED",
            Title = "Reclamation planned",
            Message = $"Your reclamation '{message.Reference}' has been planned at {schedule}.",
            UserId = message.ClientId,
            RecipientEmail = string.IsNullOrWhiteSpace(message.ClientEmail) ? null : message.ClientEmail,
            SourceEvent = message.EventType,
            CreatedAt = DateTime.UtcNow
        };

        await CreateSendAndPersistAsync(clientNotification, cancellationToken);

        var technicianNotification = new Notification
        {
            Type = "INTERVENTION_PLANNED",
            Title = "New intervention planned",
            Message = $"Intervention planned for reclamation '{message.Reference}' at {schedule}.",
            UserId = message.TechnicianId,
            RecipientEmail = null,
            SourceEvent = message.EventType,
            CreatedAt = DateTime.UtcNow
        };

        await CreateSendAndPersistAsync(technicianNotification, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            var adminNotification = new Notification
            {
                Type = "ADMIN_ALERT",
                Title = "Reclamation planned",
                Message = $"Reclamation '{message.Reference}' planned for TechnicianId={message.TechnicianId} at {schedule}.",
                RecipientEmail = _options.AdminEmail,
                SourceEvent = message.EventType,
                CreatedAt = DateTime.UtcNow
            };

            await CreateSendAndPersistAsync(adminNotification, cancellationToken);
        }
    }

    public async Task HandleReclamationStatusChangedAsync(ReclamationStatusChangedEvent message, CancellationToken cancellationToken = default)
    {
        // Avoid duplicate notifications for transitions that have dedicated events.
        if (string.Equals(message.ToStatus, "ASSIGNED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(message.ToStatus, "PLANNED", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var clientNotification = new Notification
        {
            Type = "RECLAMATION_STATUS_CHANGED",
            Title = "Reclamation updated",
            Message = $"Reclamation '{message.Reference}' status changed to {message.ToStatus}.",
            UserId = message.ClientId,
            RecipientEmail = string.IsNullOrWhiteSpace(message.ClientEmail) ? null : message.ClientEmail,
            SourceEvent = message.EventType,
            CreatedAt = DateTime.UtcNow
        };

        await CreateSendAndPersistAsync(clientNotification, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            var adminNotification = new Notification
            {
                Type = "ADMIN_ALERT",
                Title = "Reclamation status changed",
                Message = $"Reclamation '{message.Reference}' {message.FromStatus} -> {message.ToStatus} (Actor={message.ActorRole}#{message.ActorUserId}).",
                RecipientEmail = _options.AdminEmail,
                SourceEvent = message.EventType,
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

