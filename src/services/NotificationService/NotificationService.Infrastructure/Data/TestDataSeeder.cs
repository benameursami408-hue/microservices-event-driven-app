using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Data;

public static class TestDataSeeder
{
    private sealed record SeedNotification(
        string Type,
        string Title,
        string Message,
        long? UserId,
        string? RecipientEmail,
        NotificationStatus Status,
        bool IsRead,
        DateTime? ReadAt,
        DateTime CreatedAt,
        DateTime? SentAt,
        string? SourceEvent);

    private static readonly DateTime BaseUtc = new(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc);

    private static readonly SeedNotification[] Notifications =
    [
        new(
            Type: "WELCOME",
            Title: "Bienvenue sur le portail SAV",
            Message: "Votre compte client est actif. Vous pouvez creer et suivre vos dossiers SAV.",
            UserId: 1001,
            RecipientEmail: "sami.benameur.client@sav.local",
            Status: NotificationStatus.Sent,
            IsRead: true,
            ReadAt: BaseUtc.AddMinutes(15),
            CreatedAt: BaseUtc,
            SentAt: BaseUtc.AddMinutes(1),
            SourceEvent: "UserCreated"),
        new(
            Type: "RECLAMATION_CREATED",
            Title: "Dossier SAV cree",
            Message: "Votre dossier SAV-260401-0002 a ete enregistre avec succes.",
            UserId: 1002,
            RecipientEmail: "leila.mansour.client@sav.local",
            Status: NotificationStatus.Sent,
            IsRead: false,
            ReadAt: null,
            CreatedAt: BaseUtc.AddHours(1),
            SentAt: BaseUtc.AddHours(1).AddMinutes(1),
            SourceEvent: "ReclamationCreated"),
        new(
            Type: "RECLAMATION_ASSIGNED",
            Title: "Dossier affecte",
            Message: "Votre dossier SAV-260401-0002 est affecte a un agent SAV.",
            UserId: 1002,
            RecipientEmail: "leila.mansour.client@sav.local",
            Status: NotificationStatus.Sent,
            IsRead: false,
            ReadAt: null,
            CreatedAt: BaseUtc.AddHours(2),
            SentAt: BaseUtc.AddHours(2).AddMinutes(1),
            SourceEvent: "ReclamationAssigned"),
        new(
            Type: "RECLAMATION_PLANNED",
            Title: "Intervention planifiee",
            Message: "Le technicien interviendra sur votre dossier SAV-260401-0003 demain entre 08h00 et 10h00.",
            UserId: 1003,
            RecipientEmail: "amine.khelifi.client@sav.local",
            Status: NotificationStatus.Sent,
            IsRead: false,
            ReadAt: null,
            CreatedAt: BaseUtc.AddHours(3),
            SentAt: BaseUtc.AddHours(3).AddMinutes(1),
            SourceEvent: "ReclamationPlanned"),
        new(
            Type: "RECLAMATION_CLOSED",
            Title: "Dossier cloture",
            Message: "Votre dossier SAV-260401-0005 est cloture. Merci pour votre confiance.",
            UserId: 1005,
            RecipientEmail: "karim.bensalah.client@sav.local",
            Status: NotificationStatus.Pending,
            IsRead: false,
            ReadAt: null,
            CreatedAt: BaseUtc.AddHours(4),
            SentAt: null,
            SourceEvent: "ReclamationStatusChanged")
    ];

    public static async Task SeedAsync(
        AppDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var expectedTitles = Notifications.Select(n => n.Title).ToArray();
        var existingTitles = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => expectedTitles.Contains(n.Title))
            .Select(n => n.Title)
            .ToListAsync(cancellationToken);

        var existingSet = existingTitles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toInsert = Notifications
            .Where(n => !existingSet.Contains(n.Title))
            .Select(n => new Notification
            {
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                UserId = n.UserId,
                RecipientEmail = n.RecipientEmail,
                Status = n.Status,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt,
                SentAt = n.SentAt,
                SourceEvent = n.SourceEvent
            })
            .ToList();

        if (toInsert.Count == 0)
        {
            logger.LogInformation("Notification seed skipped: all 5 business notifications already exist.");
            return;
        }

        dbContext.Notifications.AddRange(toInsert);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Notification seed inserted {InsertedCount} notification(s).", toInsert.Count);
    }
}
