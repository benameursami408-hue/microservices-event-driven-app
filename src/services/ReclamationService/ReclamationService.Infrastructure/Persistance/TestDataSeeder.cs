using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Infrastructure.Data;

public static class TestDataSeeder
{
    private sealed record SeedClient(long Id, string FullName, string Email, string PhoneNumber);

    private sealed record SeedReclamation(
        long Id,
        string Reference,
        string Description,
        NamePriority Priority,
        ReclamationStatus Status,
        long ClientId,
        string ClientName,
        string ProductName,
        string Brand,
        string Model,
        string SerialNumber,
        long? TechnicianId,
        string? TechnicianName,
        DateTime CreatedAt);

    private sealed record SeedHistory(long ReclamationId, ReclamationStatus FromStatus, ReclamationStatus ToStatus, long ActorUserId, string ActorRole, string Comment, DateTime OccurredAt);

    private sealed record SeedAiAnalysis(long ReclamationId, string SuggestedPriority, int ConfidenceScore, string SlaRisk, string Reason, string RecommendedAction, string[] DetectedKeywords, DateTime CreatedAt, DateTime? AcceptedAt, long? AcceptedByUserId);

    private static readonly DateTime BaseUtc = new(2026, 5, 13, 8, 0, 0, DateTimeKind.Utc);

    private static readonly SeedClient[] Clients =
    [
        new(501, "Societe Industrielle Atlas", "client1@savpro.local", "+21670000501"),
        new(502, "Hotel Marina", "client2@savpro.local", "+21670000502"),
        new(503, "Clinique Ibn Sina", "client3@savpro.local", "+21670000503"),
        new(504, "Supermarche Central", "client4@savpro.local", "+21670000504"),
        new(505, "Usine Textile Nord", "client5@savpro.local", "+21670000505"),
        new(506, "Residence Les Jardins", "client6@savpro.local", "+21670000506")
    ];

    private static readonly (string Product, string Brand, string Model, string Serial)[] Equipment =
    [
        ("Groupe electrogene Atlas XG-250", "Atlas", "XG-250", "GEN-XG250-001"),
        ("Compresseur AirMax C120", "AirMax", "C120", "CMP-C120-002"),
        ("Climatiseur Carrier 24K BTU", "Carrier", "24K BTU", "AC-24K-003"),
        ("Pompe Grundfos P-500", "Grundfos", "P-500", "PMP-P500-004"),
        ("Four industriel ThermoPro 900", "ThermoPro", "900", "FOUR-900-005"),
        ("Machine textile TexLine 3000", "TexLine", "3000", "TXT-3000-006"),
        ("Refrigerateur professionnel ColdMax 800", "ColdMax", "800", "FRG-800-007"),
        ("Onduleur APC Smart-UPS 10K", "APC", "Smart-UPS 10K", "UPS-10K-008"),
        ("Chaudiere industrielle HeatPro 200", "HeatPro", "200", "CHD-200-009"),
        ("Tableau electrique Schneider TGBT", "Schneider", "TGBT", "TGBT-010")
    ];

    private static readonly (string Description, NamePriority Priority, ReclamationStatus Status)[] Issues =
    [
        ("Le groupe electrogene est arrete et la production est bloquee.", NamePriority.URGENT, ReclamationStatus.Open),
        ("Fuite detectee sur la pompe principale.", NamePriority.HIGH, ReclamationStatus.Open),
        ("Demande d'information sur le contrat de maintenance annuel.", NamePriority.LOW, ReclamationStatus.Open),
        ("Vibration intermittente sur equipement de production.", NamePriority.MEDUIM, ReclamationStatus.Open),
        ("Odeur de brule au niveau du tableau electrique.", NamePriority.URGENT, ReclamationStatus.Open),
        ("Le compresseur surchauffe apres 20 minutes.", NamePriority.HIGH, ReclamationStatus.InProgress),
        ("Bruit anormal dans la machine textile.", NamePriority.MEDUIM, ReclamationStatus.InProgress),
        ("Onduleur affiche une alerte batterie critique.", NamePriority.URGENT, ReclamationStatus.InProgress),
        ("Refrigerateur professionnel ne maintient plus la temperature.", NamePriority.HIGH, ReclamationStatus.InProgress),
        ("Climatiseur ne refroidit plus la salle serveur.", NamePriority.HIGH, ReclamationStatus.InProgress),
        ("Controle preventif demande sur chaudiere industrielle.", NamePriority.MEDUIM, ReclamationStatus.Planned),
        ("Pompe secondaire lente au demarrage.", NamePriority.MEDUIM, ReclamationStatus.Planned),
        ("Fumee detectee pres du coffret de puissance.", NamePriority.URGENT, ReclamationStatus.Planned),
        ("Compresseur ne fonctionne pas en mode automatique.", NamePriority.HIGH, ReclamationStatus.Planned),
        ("Demande mineure de parametrage affichage.", NamePriority.LOW, ReclamationStatus.Planned),
        ("Remplacement filtre et test de pression effectues.", NamePriority.MEDUIM, ReclamationStatus.Resolved),
        ("Surchauffe corrigee apres nettoyage du condenseur.", NamePriority.HIGH, ReclamationStatus.Resolved),
        ("Question client traitee par le service SAV.", NamePriority.LOW, ReclamationStatus.Resolved),
        ("Production bloquee resolue apres remise en service du groupe.", NamePriority.URGENT, ReclamationStatus.Resolved),
        ("Fuite reparee au niveau du raccord principal.", NamePriority.HIGH, ReclamationStatus.Resolved),
        ("Affectation technicien requise pour diagnostic sur site.", NamePriority.HIGH, ReclamationStatus.Assigned),
        ("Verification securite demandee apres alerte electrique.", NamePriority.URGENT, ReclamationStatus.Assigned),
        ("Panne intermittente necessitant planification SAV.", NamePriority.MEDUIM, ReclamationStatus.Assigned),
        ("Client a annule la demande apres resolution interne.", NamePriority.LOW, ReclamationStatus.Cancelled),
        ("Dossier cloture apres validation client finale.", NamePriority.MEDUIM, ReclamationStatus.Closed)
    ];

    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger, IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        if (configuration is not null && !ReadBool(configuration, "Seed:DemoData", true))
        {
            logger.LogInformation("Reclamation demo seed disabled by Seed:DemoData=false.");
            return;
        }

        var reclamations = BuildReclamations();

        if (ReadBool(configuration, "Seed:ResetDemoData", false))
        {
            await ResetDemoDataAsync(dbContext, reclamations, logger, cancellationToken);
        }

        await SeedClientsAsync(dbContext, logger, cancellationToken);
        await SeedReclamationsAsync(dbContext, reclamations, logger, cancellationToken);
        await SeedHistoriesAsync(dbContext, BuildHistories(reclamations), logger, cancellationToken);
        await SeedAiAnalysesAsync(dbContext, BuildAiAnalyses(reclamations), logger, cancellationToken);
    }

    private static List<SeedReclamation> BuildReclamations()
    {
        var result = new List<SeedReclamation>();
        var technicians = new (long Id, string Name)?[]
        {
            (301, "Ahmed Benali"),
            (302, "Youssef Amrani"),
            (303, "Sara El Mansouri"),
            (304, "Karim Haddad")
        };

        for (var i = 0; i < Issues.Length; i++)
        {
            var client = Clients[i % Clients.Length];
            var equipment = Equipment[i % Equipment.Length];
            var technician = i < 5 ? null : technicians[i % technicians.Length];
            result.Add(new SeedReclamation(
                Id: 10001 + i,
                Reference: $"REC-2026-{i + 1:0000}",
                Description: Issues[i].Description,
                Priority: Issues[i].Priority,
                Status: Issues[i].Status,
                ClientId: client.Id,
                ClientName: client.FullName,
                ProductName: equipment.Product,
                Brand: equipment.Brand,
                Model: equipment.Model,
                SerialNumber: equipment.Serial,
                TechnicianId: technician?.Id,
                TechnicianName: technician?.Name,
                CreatedAt: BaseUtc.AddDays(-25 + i).AddHours(i % 6)));
        }

        return result;
    }

    private static List<SeedHistory> BuildHistories(IEnumerable<SeedReclamation> reclamations)
    {
        var histories = new List<SeedHistory>();
        foreach (var r in reclamations)
        {
            histories.Add(new SeedHistory(r.Id, ReclamationStatus.Open, ReclamationStatus.Open, r.ClientId, "CLIENT", "Reclamation created", r.CreatedAt));

            if (r.Status >= ReclamationStatus.Assigned && r.Status != ReclamationStatus.Cancelled && r.Status != ReclamationStatus.Rejected)
            {
                histories.Add(new SeedHistory(r.Id, ReclamationStatus.Open, ReclamationStatus.Assigned, 200, "SAV", "Technician assigned", r.CreatedAt.AddHours(3)));
            }

            if (r.Status >= ReclamationStatus.Planned && r.Status != ReclamationStatus.Cancelled && r.Status != ReclamationStatus.Rejected)
            {
                histories.Add(new SeedHistory(r.Id, ReclamationStatus.Assigned, ReclamationStatus.Planned, 200, "SAV", "Planning request created", r.CreatedAt.AddHours(8)));
            }

            if (r.Status >= ReclamationStatus.InProgress && r.Status != ReclamationStatus.Cancelled && r.Status != ReclamationStatus.Rejected)
            {
                histories.Add(new SeedHistory(r.Id, ReclamationStatus.Planned, ReclamationStatus.InProgress, r.TechnicianId ?? 301, "ST", "Intervention started", r.CreatedAt.AddDays(1)));
            }

            if (r.Status is ReclamationStatus.Resolved or ReclamationStatus.Closed)
            {
                histories.Add(new SeedHistory(r.Id, ReclamationStatus.InProgress, ReclamationStatus.Resolved, r.TechnicianId ?? 301, "ST", "Intervention completed", r.CreatedAt.AddDays(2)));
            }

            if (r.Status == ReclamationStatus.Closed)
            {
                histories.Add(new SeedHistory(r.Id, ReclamationStatus.Resolved, ReclamationStatus.Closed, 200, "SAV", "Reclamation resolved and closed", r.CreatedAt.AddDays(3)));
            }

            if (r.Status == ReclamationStatus.Cancelled)
            {
                histories.Add(new SeedHistory(r.Id, ReclamationStatus.Open, ReclamationStatus.Cancelled, r.ClientId, "CLIENT", "Client cancelled the request", r.CreatedAt.AddHours(5)));
            }

            if (r.Priority is NamePriority.HIGH or NamePriority.URGENT)
            {
                histories.Add(new SeedHistory(r.Id, ReclamationStatus.Open, r.Status, 200, "SAV", $"AI priority suggestion applied: Medium -> {ToDisplayPriority(r.Priority)}. Reason: business rule keywords detected.", r.CreatedAt.AddHours(2)));
            }
        }

        return histories;
    }

    private static List<SeedAiAnalysis> BuildAiAnalyses(IReadOnlyList<SeedReclamation> reclamations)
    {
        return reclamations
            .Where(r => r.Priority is NamePriority.HIGH or NamePriority.URGENT)
            .Take(8)
            .Select((r, index) => new SeedAiAnalysis(
                ReclamationId: r.Id,
                SuggestedPriority: ToDisplayPriority(r.Priority),
                ConfidenceScore: r.Priority == NamePriority.URGENT ? 94 - index % 3 : 86 - index % 4,
                SlaRisk: r.Priority == NamePriority.URGENT ? "High" : "Medium",
                Reason: r.Priority == NamePriority.URGENT
                    ? "Production, safety or critical equipment keywords were detected by the explainable rule-based assistant."
                    : "Failure, leak, overheating or non-working equipment keywords indicate a high operational impact.",
                RecommendedAction: r.Priority == NamePriority.URGENT
                    ? "Assign a technician immediately and monitor SLA closely."
                    : "Plan a technician intervention and keep the SAV manager informed.",
                DetectedKeywords: r.Priority == NamePriority.URGENT ? ["blocked", "critical", "safety"] : ["failure", "leak", "overheating"],
                CreatedAt: r.CreatedAt.AddHours(2),
                AcceptedAt: index % 2 == 0 ? r.CreatedAt.AddHours(3) : null,
                AcceptedByUserId: index % 2 == 0 ? 200 : null))
            .ToList();
    }

    private static async Task ResetDemoDataAsync(AppDbContext dbContext, IReadOnlyCollection<SeedReclamation> reclamations, ILogger logger, CancellationToken cancellationToken)
    {
        var ids = reclamations.Select(r => r.Id).ToArray();
        var references = reclamations.Select(r => r.Reference).ToArray();
        var clientIds = Clients.Select(c => c.Id).ToArray();

        dbContext.AiPriorityAnalyses.RemoveRange(await dbContext.AiPriorityAnalyses.Where(a => ids.Contains(a.ReclamationId)).ToListAsync(cancellationToken));
        dbContext.ReclamationHistories.RemoveRange(await dbContext.ReclamationHistories.Where(h => ids.Contains(h.ReclamationId)).ToListAsync(cancellationToken));
        dbContext.Reclamations.RemoveRange(await dbContext.Reclamations.Where(r => ids.Contains(r.Id) || references.Contains(r.Reference)).ToListAsync(cancellationToken));
        dbContext.Clients.RemoveRange(await dbContext.Clients.Where(c => clientIds.Contains(c.Id)).ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Reclamation demo seed reset completed.");
    }

    private static async Task SeedClientsAsync(AppDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var targetIds = Clients.Select(c => c.Id).ToArray();
        var existing = await dbContext.Clients.Where(c => targetIds.Contains(c.Id)).ToListAsync(cancellationToken);
        var existingById = existing.ToDictionary(c => c.Id);
        var inserted = 0;

        foreach (var seed in Clients)
        {
            if (existingById.TryGetValue(seed.Id, out var client))
            {
                client.FullName = seed.FullName;
                client.Email = seed.Email;
                client.PhoneNumber = seed.PhoneNumber;
                continue;
            }

            dbContext.Clients.Add(new Client
            {
                Id = seed.Id,
                FullName = seed.FullName,
                Email = seed.Email,
                PhoneNumber = seed.PhoneNumber,
                CreatedAt = BaseUtc.AddDays(-30)
            });
            inserted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Reclamation demo seed clients completed. Inserted={InsertedCount}, Updated={UpdatedCount}.", inserted, existing.Count);
    }

    private static async Task SeedReclamationsAsync(AppDbContext dbContext, List<SeedReclamation> seeds, ILogger logger, CancellationToken cancellationToken)
    {
        var ids = seeds.Select(r => r.Id).ToArray();
        var references = seeds.Select(r => r.Reference).ToArray();
        var existing = await dbContext.Reclamations.Where(r => ids.Contains(r.Id) || references.Contains(r.Reference)).ToListAsync(cancellationToken);
        var existingByReference = existing.ToDictionary(r => r.Reference, StringComparer.OrdinalIgnoreCase);
        var existingById = existing.ToDictionary(r => r.Id);
        var toInsert = new List<Reclamation>();
        var updated = 0;

        foreach (var seed in seeds)
        {
            var entity = existingById.GetValueOrDefault(seed.Id) ?? existingByReference.GetValueOrDefault(seed.Reference);
            if (entity is null)
            {
                toInsert.Add(CreateReclamation(seed));
                continue;
            }

            ApplyReclamation(entity, seed);
            updated++;
        }

        if (toInsert.Count > 0)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Reclamations] ON", cancellationToken);
                dbContext.Reclamations.AddRange(toInsert);
                await dbContext.SaveChangesAsync(cancellationToken);
                await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Reclamations] OFF", cancellationToken);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Reclamation demo seed tickets completed. Inserted={InsertedCount}, Updated={UpdatedCount}.", toInsert.Count, updated);
    }

    private static Reclamation CreateReclamation(SeedReclamation seed)
    {
        var entity = new Reclamation { Id = seed.Id };
        ApplyReclamation(entity, seed);
        return entity;
    }

    private static void ApplyReclamation(Reclamation entity, SeedReclamation seed)
    {
        var plannedStart = seed.Status >= ReclamationStatus.Planned && seed.Status != ReclamationStatus.Cancelled ? seed.CreatedAt.AddDays(1).AddHours(2) : (DateTime?)null;
        var plannedEnd = plannedStart?.AddHours(2);
        entity.Reference = seed.Reference;
        entity.Description = seed.Description;
        entity.Priority = seed.Priority;
        entity.Severity = seed.Priority;
        entity.Status = seed.Status;
        entity.ClientId = seed.ClientId;
        entity.ClientName = seed.ClientName;
        entity.SAVId = 200;
        entity.SAVName = "Sofia Mansouri";
        entity.AssignedAt = seed.Status >= ReclamationStatus.Assigned ? seed.CreatedAt.AddHours(3) : null;
        entity.TechnicianId = seed.TechnicianId;
        entity.TechnicianName = seed.TechnicianName;
        entity.PlannedStartAt = plannedStart;
        entity.PlannedEndAt = plannedEnd;
        entity.PlanningNote = plannedStart is null ? null : "Rendez-vous planifie depuis les donnees de demonstration.";
        entity.ResolutionNote = seed.Status is ReclamationStatus.Resolved or ReclamationStatus.Closed ? "Resolution validee dans le scenario de demonstration." : null;
        entity.ResolvedAt = seed.Status is ReclamationStatus.Resolved or ReclamationStatus.Closed ? seed.CreatedAt.AddDays(2) : null;
        entity.ClosedAt = seed.Status == ReclamationStatus.Closed ? seed.CreatedAt.AddDays(3) : null;
        entity.CancelledAt = seed.Status == ReclamationStatus.Cancelled ? seed.CreatedAt.AddHours(5) : null;
        entity.RejectedAt = seed.Status == ReclamationStatus.Rejected ? seed.CreatedAt.AddHours(4) : null;
        entity.RejectionReason = seed.Status == ReclamationStatus.Rejected ? "Demande hors garantie." : null;
        entity.CreatedAt = seed.CreatedAt;
        entity.UpdatedAt = seed.CreatedAt.AddHours(12);
        entity.ProductName = seed.ProductName;
        entity.Brand = seed.Brand;
        entity.Model = seed.Model;
        entity.SerialNumber = seed.SerialNumber;
        entity.ProductReference = seed.SerialNumber;
        entity.Barcode = seed.SerialNumber;
        entity.SellerName = "SAV Pro Demo";
        entity.PurchaseDate = seed.CreatedAt.AddMonths(-10);
        entity.Category = seed.Priority is NamePriority.HIGH or NamePriority.URGENT ? TicketCategory.TechnicalFailure : TicketCategory.Maintenance;
        entity.CategoryReason = "Demo data classification for PFE scenario.";
        entity.CategoryUpdatedAt = seed.CreatedAt.AddHours(1);
        entity.PriorityScore = seed.Priority switch
        {
            NamePriority.URGENT => 95,
            NamePriority.HIGH => 84,
            NamePriority.MEDUIM => 70,
            _ => 55
        };
        entity.PriorityReasons = seed.Priority is NamePriority.URGENT or NamePriority.HIGH
            ? "Keywords indicate high operational impact or safety risk."
            : "Standard business impact for demonstration.";
        entity.PrioritySource = PrioritySource.Rules;
        entity.PriorityUpdatedAt = seed.CreatedAt.AddHours(2);
        entity.IsBlocking = seed.Priority == NamePriority.URGENT;
        entity.FollowUpCount = seed.Status >= ReclamationStatus.InProgress ? 2 : 0;
        entity.FirstResponseDeadline = seed.CreatedAt.AddHours(seed.Priority == NamePriority.URGENT ? 2 : 8);
        entity.PlanningDeadline = seed.CreatedAt.AddDays(seed.Priority == NamePriority.URGENT ? 1 : 3);
        entity.ResolutionDeadline = seed.CreatedAt.AddDays(seed.Priority == NamePriority.URGENT ? 2 : 7);
        entity.SlaStatus = seed.Status is ReclamationStatus.Resolved or ReclamationStatus.Closed
            ? SlaStatus.Completed
            : seed.Priority == NamePriority.URGENT ? SlaStatus.NearBreach : SlaStatus.OnTrack;
        entity.SlaBreachedAt = null;
        entity.ManualPriorityOverride = false;
        entity.RequiresReplanning = false;
        entity.LastInterventionOutcome = seed.Status is ReclamationStatus.Resolved or ReclamationStatus.Closed ? "Solved" : null;
        entity.LastInterventionReportSummary = seed.Status is ReclamationStatus.Resolved or ReclamationStatus.Closed ? "Rapport de visite disponible dans le module interventions." : null;
    }

    private static async Task SeedHistoriesAsync(AppDbContext dbContext, List<SeedHistory> histories, ILogger logger, CancellationToken cancellationToken)
    {
        var reclamationIds = histories.Select(h => h.ReclamationId).Distinct().ToArray();
        var existing = await dbContext.ReclamationHistories
            .Where(h => reclamationIds.Contains(h.ReclamationId))
            .Select(h => new { h.ReclamationId, h.Comment })
            .ToListAsync(cancellationToken);
        var existingSet = existing.Select(h => $"{h.ReclamationId}:{h.Comment}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toInsert = histories
            .Where(h => !existingSet.Contains($"{h.ReclamationId}:{h.Comment}"))
            .Select(h => new ReclamationHistory
            {
                ReclamationId = h.ReclamationId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                ActorUserId = h.ActorUserId,
                ActorRole = h.ActorRole,
                Comment = h.Comment,
                OccurredAt = h.OccurredAt
            })
            .ToList();

        if (toInsert.Count > 0)
        {
            dbContext.ReclamationHistories.AddRange(toInsert);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Reclamation demo seed history completed. Inserted={InsertedCount}.", toInsert.Count);
    }

    private static async Task SeedAiAnalysesAsync(AppDbContext dbContext, List<SeedAiAnalysis> analyses, ILogger logger, CancellationToken cancellationToken)
    {
        var reclamationIds = analyses.Select(a => a.ReclamationId).ToArray();
        var existingIds = await dbContext.AiPriorityAnalyses
            .Where(a => reclamationIds.Contains(a.ReclamationId))
            .Select(a => a.ReclamationId)
            .ToListAsync(cancellationToken);
        var existingSet = existingIds.ToHashSet();
        var toInsert = analyses
            .Where(a => !existingSet.Contains(a.ReclamationId))
            .Select(a => new AiPriorityAnalysis
            {
                ReclamationId = a.ReclamationId,
                SuggestedPriority = a.SuggestedPriority,
                ConfidenceScore = a.ConfidenceScore,
                SlaRisk = a.SlaRisk,
                Reason = a.Reason,
                RecommendedAction = a.RecommendedAction,
                DetectedKeywordsJson = JsonSerializer.Serialize(a.DetectedKeywords),
                CreatedAt = a.CreatedAt,
                AcceptedAt = a.AcceptedAt,
                AcceptedByUserId = a.AcceptedByUserId
            })
            .ToList();

        if (toInsert.Count > 0)
        {
            dbContext.AiPriorityAnalyses.AddRange(toInsert);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Reclamation demo seed AI analyses completed. Inserted={InsertedCount}.", toInsert.Count);
    }

    private static string ToDisplayPriority(NamePriority priority) => priority switch
    {
        NamePriority.URGENT => "Urgent",
        NamePriority.HIGH => "High",
        NamePriority.MEDUIM => "Medium",
        _ => "Low"
    };

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
