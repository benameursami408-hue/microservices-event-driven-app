using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Infrastructure.Data;

public static class TestDataSeeder
{
    private sealed record SeedClient(long Id, string FullName, string Email, string PhoneNumber);

    private sealed record SeedReclamation(
        string Reference,
        string Description,
        NamePriority Priority,
        ReclamationStatus Status,
        long ClientId,
        string ClientName,
        long? SavId,
        string? SavName,
        DateTime? AssignedAt,
        long? TechnicianId,
        string? TechnicianName,
        DateTime? PlannedStartAt,
        DateTime? PlannedEndAt,
        string? PlanningNote,
        string? ResolutionNote,
        DateTime? ResolvedAt,
        DateTime? ClosedAt,
        DateTime? CancelledAt,
        DateTime? RejectedAt,
        string? RejectionReason,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record SeedHistory(
        string ReclamationReference,
        ReclamationStatus FromStatus,
        ReclamationStatus ToStatus,
        long ActorUserId,
        string ActorRole,
        string Comment,
        DateTime OccurredAt);

    private static readonly SeedClient[] Clients =
    [
        new(1001, "Sami Benameur", "sami.benameur.client@sav.local", "0600000001"),
        new(1002, "Leila Mansour", "leila.mansour.client@sav.local", "0600000002"),
        new(1003, "Amine Khelifi", "amine.khelifi.client@sav.local", "0600000003"),
        new(1004, "Yasmine Triki", "yasmine.triki.client@sav.local", "0600000004"),
        new(1005, "Karim Ben Salah", "karim.bensalah.client@sav.local", "0600000005")
    ];

    private static readonly DateTime BaseUtc = new(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);

    private static readonly SeedReclamation[] Reclamations =
    [
        new(
            Reference: "SAV-260401-0001",
            Description: "Le lave-linge fuit a chaque cycle d'essorage.",
            Priority: NamePriority.HIGH,
            Status: ReclamationStatus.Cancelled,
            ClientId: 1001,
            ClientName: "Sami Benameur",
            SavId: 2001,
            SavName: "Youssef Trabelsi",
            AssignedAt: BaseUtc.AddHours(2),
            TechnicianId: null,
            TechnicianName: null,
            PlannedStartAt: null,
            PlannedEndAt: null,
            PlanningNote: null,
            ResolutionNote: null,
            ResolvedAt: null,
            ClosedAt: null,
            CancelledAt: BaseUtc.AddHours(6),
            RejectedAt: null,
            RejectionReason: null,
            CreatedAt: BaseUtc,
            UpdatedAt: BaseUtc.AddHours(6)),
        new(
            Reference: "SAV-260401-0002",
            Description: "Ecran TV noir apres 10 minutes d'utilisation.",
            Priority: NamePriority.URGENT,
            Status: ReclamationStatus.Assigned,
            ClientId: 1002,
            ClientName: "Leila Mansour",
            SavId: 2001,
            SavName: "Youssef Trabelsi",
            AssignedAt: BaseUtc.AddHours(3),
            TechnicianId: null,
            TechnicianName: null,
            PlannedStartAt: null,
            PlannedEndAt: null,
            PlanningNote: null,
            ResolutionNote: null,
            ResolvedAt: null,
            ClosedAt: null,
            CancelledAt: null,
            RejectedAt: null,
            RejectionReason: null,
            CreatedAt: BaseUtc.AddHours(1),
            UpdatedAt: BaseUtc.AddHours(3)),
        new(
            Reference: "SAV-260401-0003",
            Description: "Le refrigerateur ne refroidit plus correctement.",
            Priority: NamePriority.HIGH,
            Status: ReclamationStatus.Planned,
            ClientId: 1003,
            ClientName: "Amine Khelifi",
            SavId: 2002,
            SavName: "Ines Bouraoui",
            AssignedAt: BaseUtc.AddHours(4),
            TechnicianId: 3001,
            TechnicianName: "Nour Ben Ali",
            PlannedStartAt: BaseUtc.AddDays(1),
            PlannedEndAt: BaseUtc.AddDays(1).AddHours(2),
            PlanningNote: "Intervention a domicile confirmee avec le client.",
            ResolutionNote: null,
            ResolvedAt: null,
            ClosedAt: null,
            CancelledAt: null,
            RejectedAt: null,
            RejectionReason: null,
            CreatedAt: BaseUtc.AddHours(2),
            UpdatedAt: BaseUtc.AddHours(4)),
        new(
            Reference: "SAV-260401-0004",
            Description: "Le four ne chauffe plus au-dessus de 120C.",
            Priority: NamePriority.MEDUIM,
            Status: ReclamationStatus.Resolved,
            ClientId: 1004,
            ClientName: "Yasmine Triki",
            SavId: 2002,
            SavName: "Ines Bouraoui",
            AssignedAt: BaseUtc.AddHours(5),
            TechnicianId: 3002,
            TechnicianName: "Hatem Gharbi",
            PlannedStartAt: BaseUtc.AddDays(2),
            PlannedEndAt: BaseUtc.AddDays(2).AddHours(1),
            PlanningNote: "Diagnostic sur place puis remplacement thermostat.",
            ResolutionNote: "Thermostat remplace et cycle de test valide.",
            ResolvedAt: BaseUtc.AddDays(2).AddHours(2),
            ClosedAt: null,
            CancelledAt: null,
            RejectedAt: null,
            RejectionReason: null,
            CreatedAt: BaseUtc.AddHours(3),
            UpdatedAt: BaseUtc.AddDays(2).AddHours(2)),
        new(
            Reference: "SAV-260401-0005",
            Description: "Aspirateur robot bloque avec erreur moteur.",
            Priority: NamePriority.LOW,
            Status: ReclamationStatus.Closed,
            ClientId: 1005,
            ClientName: "Karim Ben Salah",
            SavId: 2003,
            SavName: "Meriem Chaabane",
            AssignedAt: BaseUtc.AddHours(6),
            TechnicianId: 3003,
            TechnicianName: "Walid Mzoughi",
            PlannedStartAt: BaseUtc.AddDays(1).AddHours(3),
            PlannedEndAt: BaseUtc.AddDays(1).AddHours(5),
            PlanningNote: "Depot atelier organise par le client.",
            ResolutionNote: "Moteur nettoye et firmware mis a jour.",
            ResolvedAt: BaseUtc.AddDays(1).AddHours(5),
            ClosedAt: BaseUtc.AddDays(2).AddHours(6),
            CancelledAt: null,
            RejectedAt: null,
            RejectionReason: null,
            CreatedAt: BaseUtc.AddHours(4),
            UpdatedAt: BaseUtc.AddDays(2).AddHours(6))
    ];

    private static readonly SeedHistory[] Histories =
    [
        new("SAV-260401-0001", ReclamationStatus.Open, ReclamationStatus.Cancelled, 1001, "CLIENT", "Annulation demandee par le client.", BaseUtc.AddHours(6)),
        new("SAV-260401-0002", ReclamationStatus.Open, ReclamationStatus.Assigned, 2001, "SAV", "Dossier affecte a l'equipe SAV.", BaseUtc.AddHours(3)),
        new("SAV-260401-0003", ReclamationStatus.Assigned, ReclamationStatus.Planned, 2002, "SAV", "Rendez-vous technicien planifie.", BaseUtc.AddHours(4)),
        new("SAV-260401-0004", ReclamationStatus.InProgress, ReclamationStatus.Resolved, 3002, "ST", "Intervention terminee, probleme corrige.", BaseUtc.AddDays(2).AddHours(2)),
        new("SAV-260401-0005", ReclamationStatus.Resolved, ReclamationStatus.Closed, 2003, "SAV", "Validation client recue et dossier cloture.", BaseUtc.AddDays(2).AddHours(6))
    ];

    public static async Task SeedAsync(
        AppDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedClientsAsync(dbContext, logger, cancellationToken);
        await SeedReclamationsAsync(dbContext, logger, cancellationToken);
        await SeedHistoriesAsync(dbContext, logger, cancellationToken);
    }

    private static async Task SeedClientsAsync(AppDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var targetIds = Clients.Select(c => c.Id).ToArray();
        var existingIds = await dbContext.Clients
            .AsNoTracking()
            .Where(c => targetIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var existingSet = existingIds.ToHashSet();
        var toInsert = Clients
            .Where(c => !existingSet.Contains(c.Id))
            .Select(c => new Client
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                CreatedAt = BaseUtc
            })
            .ToList();

        if (toInsert.Count == 0)
        {
            logger.LogInformation("Reclamation seed skipped for Clients: all 5 records already exist.");
            return;
        }

        dbContext.Clients.AddRange(toInsert);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Reclamation seed inserted {InsertedCount} client(s).", toInsert.Count);
    }

    private static async Task SeedReclamationsAsync(AppDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var targetReferences = Reclamations.Select(r => r.Reference).ToArray();
        var existingReferences = await dbContext.Reclamations
            .AsNoTracking()
            .Where(r => targetReferences.Contains(r.Reference))
            .Select(r => r.Reference)
            .ToListAsync(cancellationToken);

        var existingSet = existingReferences.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toInsert = Reclamations
            .Where(r => !existingSet.Contains(r.Reference))
            .Select(r => new Reclamation
            {
                Reference = r.Reference,
                Description = r.Description,
                Priority = r.Priority,
                Status = r.Status,
                ClientId = r.ClientId,
                ClientName = r.ClientName,
                SAVId = r.SavId,
                SAVName = r.SavName,
                AssignedAt = r.AssignedAt,
                TechnicianId = r.TechnicianId,
                TechnicianName = r.TechnicianName,
                PlannedStartAt = r.PlannedStartAt,
                PlannedEndAt = r.PlannedEndAt,
                PlanningNote = r.PlanningNote,
                ResolutionNote = r.ResolutionNote,
                ResolvedAt = r.ResolvedAt,
                ClosedAt = r.ClosedAt,
                CancelledAt = r.CancelledAt,
                RejectedAt = r.RejectedAt,
                RejectionReason = r.RejectionReason,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToList();

        if (toInsert.Count == 0)
        {
            logger.LogInformation("Reclamation seed skipped for Reclamations: all 5 records already exist.");
            return;
        }

        dbContext.Reclamations.AddRange(toInsert);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Reclamation seed inserted {InsertedCount} reclamation(s).", toInsert.Count);
    }

    private static async Task SeedHistoriesAsync(AppDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var historyReferences = Histories
            .Select(h => h.ReclamationReference)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var referenceToId = await dbContext.Reclamations
            .AsNoTracking()
            .Where(r => historyReferences.Contains(r.Reference))
            .ToDictionaryAsync(r => r.Reference, r => r.Id, cancellationToken);

        var reclamationIds = referenceToId.Values.Distinct().ToArray();
        var existingHistoryKeys = await dbContext.ReclamationHistories
            .AsNoTracking()
            .Where(h => reclamationIds.Contains(h.ReclamationId))
            .Select(h => new { h.ReclamationId, h.FromStatus, h.ToStatus })
            .ToListAsync(cancellationToken);

        var existingSet = existingHistoryKeys
            .Select(x => $"{x.ReclamationId}:{(int)x.FromStatus}:{(int)x.ToStatus}")
            .ToHashSet(StringComparer.Ordinal);

        var toInsert = new List<ReclamationHistory>();

        foreach (var history in Histories)
        {
            if (!referenceToId.TryGetValue(history.ReclamationReference, out var reclamationId))
            {
                continue;
            }

            var key = $"{reclamationId}:{(int)history.FromStatus}:{(int)history.ToStatus}";
            if (existingSet.Contains(key))
            {
                continue;
            }

            toInsert.Add(new ReclamationHistory
            {
                ReclamationId = reclamationId,
                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,
                ActorUserId = history.ActorUserId,
                ActorRole = history.ActorRole,
                Comment = history.Comment,
                OccurredAt = history.OccurredAt
            });
        }

        if (toInsert.Count == 0)
        {
            logger.LogInformation("Reclamation seed skipped for ReclamationHistories: all 5 records already exist.");
            return;
        }

        dbContext.ReclamationHistories.AddRange(toInsert);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Reclamation seed inserted {InsertedCount} history record(s).", toInsert.Count);
    }
}
