using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task<List<Notification>> GetLatestAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetLatestForUserAsync(long userId, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(long notificationId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var item = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        if (!isAdmin && item.UserId != userId)
        {
            return false;
        }

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => !n.IsRead);
        if (!isAdmin)
        {
            query = query.Where(n => n.UserId == userId);
        }

        var items = await query.ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return items.Count;
    }
    public async Task<bool> DeleteAsync(long notificationId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var item = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        if (!isAdmin && item.UserId != userId)
        {
            return false;
        }

        _context.Notifications.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

}
