using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetLatestAsync(int take, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetLatestForUserAsync(long userId, int take, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(long notificationId, long userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default);
}
