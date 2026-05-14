using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        DateTime CreatedAt,
        string SourceEvent);

    private static readonly DateTime BaseUtc = new(2026, 5, 13, 9, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger, IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        if (configuration is not null && !ReadBool(configuration, "Seed:DemoData", true))
        {
            logger.LogInformation("Notification demo seed disabled by Seed:DemoData=false.");
            return;
        }

        var notifications = BuildNotifications();

        if (ReadBool(configuration, "Seed:ResetDemoData", false))
        {
            var sourceEvents = notifications.Select(n => n.SourceEvent).ToArray();
            var existingDemo = await dbContext.Notifications
                .Where(n => n.SourceEvent != null && sourceEvents.Contains(n.SourceEvent))
                .ToListAsync(cancellationToken);
            dbContext.Notifications.RemoveRange(existingDemo);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Notification demo seed reset removed {Count} notification(s).", existingDemo.Count);
        }

        var expectedSources = notifications.Select(n => n.SourceEvent).ToArray();
        var existingSources = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.SourceEvent != null && expectedSources.Contains(n.SourceEvent))
            .Select(n => n.SourceEvent!)
            .ToListAsync(cancellationToken);

        var existingSet = existingSources.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toInsert = notifications
            .Where(n => !existingSet.Contains(n.SourceEvent))
            .Select(n => new Notification
            {
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                UserId = n.UserId,
                RecipientEmail = n.RecipientEmail,
                Status = n.Status,
                IsRead = n.IsRead,
                ReadAt = n.IsRead ? n.CreatedAt.AddMinutes(30) : null,
                CreatedAt = n.CreatedAt,
                SentAt = n.Status == NotificationStatus.Sent ? n.CreatedAt.AddMinutes(1) : null,
                SourceEvent = n.SourceEvent
            })
            .ToList();

        if (toInsert.Count > 0)
        {
            dbContext.Notifications.AddRange(toInsert);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Notification demo seed completed. Inserted={InsertedCount}.", toInsert.Count);
    }

    private static List<SeedNotification> BuildNotifications()
    {
        var rows = new List<SeedNotification>();
        var index = 1;

        void Add(string type, string title, string message, long userId, string email, bool read = false)
        {
            rows.Add(new SeedNotification(
                Type: type,
                Title: title,
                Message: message,
                UserId: userId,
                RecipientEmail: email,
                Status: NotificationStatus.Sent,
                IsRead: read,
                CreatedAt: BaseUtc.AddMinutes(index * 11),
                SourceEvent: $"DemoData:{index:000}"));
            index++;
        }

        Add("ADMIN_ALERT", "Nouvelle reclamation urgente", "REC-2026-0001 signale une production bloquee.", 100, "admin@savpro.local");
        Add("SLA_RISK", "SLA a risque", "Une reclamation urgente approche son echeance de premiere reponse.", 100, "admin@savpro.local");
        Add("REPORT_SUBMITTED", "Rapport soumis", "Un rapport de visite est disponible pour validation.", 100, "admin@savpro.local", read: true);
        Add("AI_PRIORITY_APPLIED", "Priorite IA appliquee", "L'assistant rule-based a recommande une priorite urgente.", 100, "admin@savpro.local");

        Add("RECLAMATION_CREATED", "Reclamation urgente creee", "Le client Societe Industrielle Atlas a cree une reclamation urgente.", 200, "sav@savpro.local");
        Add("TECHNICIAN_ASSIGNED", "Technicien assigne", "Ahmed Benali est assigne a INT-2026-0001.", 200, "sav@savpro.local");
        Add("INTERVENTION_PLANNED", "Intervention planifiee", "Deux interventions sont planifiees aujourd'hui.", 200, "sav@savpro.local");
        Add("RECLAMATION_RESOLVED", "Reclamation resolue", "REC-2026-0019 est resolue apres intervention.", 200, "sav@savpro.local", read: true);
        Add("REPORT_SUBMITTED", "Rapport technicien a verifier", "Un nouveau rapport attend validation SAV.", 200, "sav@savpro.local");

        for (var i = 1; i <= 8; i++)
        {
            Add(
                i <= 2 ? "INTERVENTION_TODAY" : i <= 4 ? "INTERVENTION_PLANNED" : i <= 6 ? "INTERVENTION_STARTED" : "REPORT_DUE",
                i <= 2 ? $"Intervention aujourd'hui #{i}" : i <= 4 ? $"Nouvelle mission planifiee #{i}" : i <= 6 ? $"Intervention en cours #{i}" : $"Rapport a completer #{i}",
                i <= 2 ? "Une intervention vous attend aujourd'hui dans votre planning."
                    : i <= 4 ? "Une nouvelle mission a ete assignee par le service SAV."
                    : i <= 6 ? "Votre intervention est marquee en cours."
                    : "Merci de completer le rapport de visite.",
                301,
                "tech1@savpro.local",
                read: i == 1);
        }

        for (var i = 1; i <= 5; i++)
        {
            Add("INTERVENTION_PLANNED", $"Mission technicien 2 #{i}", "Une intervention a ete assignee a votre agenda.", 302, "tech2@savpro.local", read: i == 1);
        }

        Add("INTERVENTION_PLANNED", "Mission Sara El Mansouri", "Une intervention electrique est planifiee demain.", 303, "tech3@savpro.local");
        Add("INTERVENTION_PLANNED", "Mission Karim Haddad", "Une intervention generale est affectee cette semaine.", 304, "tech4@savpro.local");

        var clients = new (long Id, string Email, string Name)[]
        {
            (501, "client1@savpro.local", "Societe Industrielle Atlas"),
            (502, "client2@savpro.local", "Hotel Marina"),
            (503, "client3@savpro.local", "Clinique Ibn Sina"),
            (504, "client4@savpro.local", "Supermarche Central"),
            (505, "client5@savpro.local", "Usine Textile Nord"),
            (506, "client6@savpro.local", "Residence Les Jardins")
        };

        foreach (var client in clients)
        {
            Add("CLIENT_UPDATE", $"Mise a jour dossier {client.Name}", "Votre reclamation SAV a ete mise a jour.", client.Id, client.Email, read: client.Id % 2 == 0);
        }

        Add("RECLAMATION_RESOLVED", "Reclamation resolue", "Votre dossier REC-2026-0019 a ete resolu.", 501, "client1@savpro.local");
        Add("AI_PRIORITY_APPLIED", "Priorite urgente confirmee", "Le service SAV a confirme la priorite urgente de votre dossier.", 501, "client1@savpro.local");

        return rows;
    }

    private static bool ReadBool(IConfiguration? configuration, string key, bool defaultValue)
    {
        if (configuration is null)
        {
            return defaultValue;
        }

        var value = configuration[key];
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

}
