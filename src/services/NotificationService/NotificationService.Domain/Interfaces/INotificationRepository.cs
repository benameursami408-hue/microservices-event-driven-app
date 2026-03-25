using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetLatestAsync(int take, CancellationToken cancellationToken = default);
}
