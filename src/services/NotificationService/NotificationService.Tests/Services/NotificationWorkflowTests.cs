using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configuration;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using SharedEvents.Events;

namespace NotificationService.Tests.Services;

public class NotificationWorkflowTests
{
    [Fact]
    public async Task HandleUserCreatedAsync_SendsAndPersistsWelcomeNotification()
    {
        var repository = new FakeNotificationRepository();
        var sender = new FakeNotificationSender();
        var workflow = CreateWorkflow(repository, sender);

        await workflow.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = 12,
            FirstName = "Ali",
            LastName = "Mansour",
            Email = "ali@example.com",
            Role = "CLIENT"
        });

        var notification = Assert.Single(repository.Notifications);
        Assert.Equal("WELCOME", notification.Type);
        Assert.Equal(12, notification.UserId);
        Assert.Equal("ali@example.com", notification.RecipientEmail);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentAt);
        Assert.Single(sender.SentNotifications);
    }

    [Fact]
    public async Task HandleReclamationCreatedAsync_PersistsClientAndAdminNotifications_WhenAdminEmailConfigured()
    {
        var repository = new FakeNotificationRepository();
        var workflow = CreateWorkflow(repository, new FakeNotificationSender(), adminEmail: "admin@sav.local");

        await workflow.HandleReclamationCreatedAsync(new ReclamationCreatedEvent
        {
            ReclamationId = 100,
            Reference = "REC-20260430-0001",
            ClientId = 44,
            ClientName = "Client Test",
            ClientEmail = "client@example.com",
            Priority = "HIGH",
            Status = "OPEN",
            Description = "Machine issue"
        });

        Assert.Equal(2, repository.Notifications.Count);
        Assert.Contains(repository.Notifications, x => x.Type == "RECLAMATION_CREATED" && x.UserId == 44);
        Assert.Contains(repository.Notifications, x => x.Type == "ADMIN_ALERT" && x.RecipientEmail == "admin@sav.local");
    }

    [Theory]
    [InlineData("ASSIGNED")]
    [InlineData("PLANNED")]
    public async Task HandleReclamationStatusChangedAsync_DoesNotDuplicateDedicatedWorkflowEvents(string targetStatus)
    {
        var repository = new FakeNotificationRepository();
        var workflow = CreateWorkflow(repository, new FakeNotificationSender(), adminEmail: "admin@sav.local");

        await workflow.HandleReclamationStatusChangedAsync(new ReclamationStatusChangedEvent
        {
            ReclamationId = 100,
            Reference = "REC-20260430-0002",
            ClientId = 44,
            ClientEmail = "client@example.com",
            FromStatus = "OPEN",
            ToStatus = targetStatus,
            ActorUserId = 1,
            ActorRole = "SAV"
        });

        Assert.Empty(repository.Notifications);
    }

    [Fact]
    public async Task HandleUserCreatedAsync_PersistsFailedStatus_WhenSenderThrows()
    {
        var repository = new FakeNotificationRepository();
        var sender = new FakeNotificationSender { ThrowOnSend = true };
        var workflow = CreateWorkflow(repository, sender);

        await workflow.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = 12,
            FirstName = "Ali",
            LastName = "Mansour",
            Email = "ali@example.com",
            Role = "CLIENT"
        });

        var notification = Assert.Single(repository.Notifications);
        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Null(notification.SentAt);
    }

    private static NotificationWorkflow CreateWorkflow(
        FakeNotificationRepository repository,
        FakeNotificationSender sender,
        string adminEmail = "")
    {
        return new NotificationWorkflow(
            repository,
            sender,
            Options.Create(new NotificationOptions { AdminEmail = adminEmail }),
            NullLogger<NotificationWorkflow>.Instance);
    }

    private sealed class FakeNotificationSender : INotificationSender
    {
        public bool ThrowOnSend { get; init; }
        public List<Notification> SentNotifications { get; } = new();

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSend) throw new InvalidOperationException("SMTP unavailable");
            SentNotifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Notifications { get; } = new();
        private long _nextId = 1;

        public Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            notification.Id = _nextId++;
            Notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task<List<Notification>> GetLatestAsync(int take, CancellationToken cancellationToken = default)
            => Task.FromResult(Notifications.OrderByDescending(x => x.CreatedAt).Take(take).ToList());

        public Task<List<Notification>> GetLatestForUserAsync(long userId, int take, CancellationToken cancellationToken = default)
            => Task.FromResult(Notifications.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Take(take).ToList());

        public Task<bool> MarkAsReadAsync(long notificationId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var notification = Notifications.FirstOrDefault(x => x.Id == notificationId && (isAdmin || x.UserId == userId));
            if (notification == null) return Task.FromResult(false);
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<int> MarkAllAsReadAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var items = Notifications.Where(x => isAdmin || x.UserId == userId).ToList();
            foreach (var item in items)
            {
                item.IsRead = true;
                item.ReadAt = DateTime.UtcNow;
            }

            return Task.FromResult(items.Count);
        }

        public Task<bool> DeleteAsync(long notificationId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var notification = Notifications.FirstOrDefault(x => x.Id == notificationId && (isAdmin || x.UserId == userId));
            if (notification == null) return Task.FromResult(false);
            Notifications.Remove(notification);
            return Task.FromResult(true);
        }
    }
}
