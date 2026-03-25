using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
