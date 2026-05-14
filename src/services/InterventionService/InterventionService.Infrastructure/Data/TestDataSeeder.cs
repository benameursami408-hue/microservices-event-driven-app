using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InterventionService.Infrastructure.Data;

public static class TestDataSeeder
{
    private sealed record SeedIntervention(
        int Number,
        long ReclamationId,
        long ClientId,
        string ClientName,
        string ClientEmail,
        string ClientPhone,
        string ServiceAddress,
        string ProductName,
        string Brand,
        string Model,
        string SerialNumber,
        string Priority,
        long TechnicianId,
        string TechnicianName,
        DateTime ScheduledAt,
        InterventionStatus Status,
        string Note);

    private static readonly DateTime Today = DateTime.UtcNow.Date.AddHours(8);

    private static readonly (long Id, string Name, string Email, string Phone, string Address)[] Clients =
    [
        (501, "Societe Industrielle Atlas", "client1@savpro.local", "+21670000501", "Zone industrielle Mghira, Ben Arous"),
        (502, "Hotel Marina", "client2@savpro.local", "+21670000502", "Port El Kantaoui, Sousse"),
        (503, "Clinique Ibn Sina", "client3@savpro.local", "+21670000503", "Centre urbain nord, Tunis"),
        (504, "Supermarche Central", "client4@savpro.local", "+21670000504", "Avenue Habib Bourguiba, Tunis"),
        (505, "Usine Textile Nord", "client5@savpro.local", "+21670000505", "Zone industrielle Utique, Bizerte"),
        (506, "Residence Les Jardins", "client6@savpro.local", "+21670000506", "La Marsa, Tunis")
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

    private static readonly (long Id, string Name)[] Technicians =
    [
        (301, "Ahmed Benali"),
        (302, "Youssef Amrani"),
        (303, "Sara El Mansouri"),
        (304, "Karim Haddad")
    ];

    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger, IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        if (configuration is not null && !ReadBool(configuration, "Seed:DemoData", true))
        {
            logger.LogInformation("Intervention demo seed disabled by Seed:DemoData=false.");
            return;
        }

        var seeds = BuildSeeds();

        if (ReadBool(configuration, "Seed:ResetDemoData", false))
        {
            await ResetDemoDataAsync(dbContext, seeds, logger, cancellationToken);
        }

        await SeedPlanningAsync(dbContext, seeds, logger, cancellationToken);
        await SeedRealisationAsync(dbContext, seeds, logger, cancellationToken);
    }

    private static List<SeedIntervention> BuildSeeds()
    {
        var result = new List<SeedIntervention>();
        var techSchedule = new (long TechId, InterventionStatus Status, DateTime ScheduledAt)[]
        {
            (301, InterventionStatus.Ready, Today.AddHours(1)),
            (301, InterventionStatus.Ready, Today.AddHours(4)),
            (301, InterventionStatus.Ready, Today.AddDays(1).AddHours(1)),
            (301, InterventionStatus.Ready, Today.AddDays(1).AddHours(4)),
            (301, InterventionStatus.Started, Today.AddHours(-2)),
            (301, InterventionStatus.Started, Today.AddHours(-1)),
            (301, InterventionStatus.Completed, Today.AddDays(-2).AddHours(2)),
            (301, InterventionStatus.Completed, Today.AddDays(-4).AddHours(3)),
            (302, InterventionStatus.Ready, Today.AddDays(2).AddHours(1)),
            (302, InterventionStatus.Ready, Today.AddDays(3).AddHours(1)),
            (302, InterventionStatus.Ready, Today.AddDays(4).AddHours(1)),
            (302, InterventionStatus.Ready, Today.AddDays(5).AddHours(1)),
            (302, InterventionStatus.Completed, Today.AddDays(-1).AddHours(1)),
            (302, InterventionStatus.Completed, Today.AddDays(-3).AddHours(2)),
            (302, InterventionStatus.Completed, Today.AddDays(-6).AddHours(2)),
            (302, InterventionStatus.Started, Today.AddHours(-3)),
            (302, InterventionStatus.Started, Today.AddHours(-2)),
            (303, InterventionStatus.Ready, Today.AddDays(1).AddHours(6)),
            (303, InterventionStatus.Completed, Today.AddDays(-5).AddHours(4)),
            (304, InterventionStatus.Aborted, Today.AddDays(-1).AddHours(5))
        };

        for (var i = 0; i < techSchedule.Length; i++)
        {
            var client = Clients[i % Clients.Length];
            var equipment = Equipment[i % Equipment.Length];
            var technician = Technicians.First(t => t.Id == techSchedule[i].TechId);
            var priority = i % 5 == 0 ? "Urgent" : i % 3 == 0 ? "High" : i % 2 == 0 ? "Medium" : "Low";
            result.Add(new SeedIntervention(
                Number: i + 1,
                ReclamationId: 10001 + i,
                ClientId: client.Id,
                ClientName: client.Name,
                ClientEmail: client.Email,
                ClientPhone: client.Phone,
                ServiceAddress: client.Address,
                ProductName: equipment.Product,
                Brand: equipment.Brand,
                Model: equipment.Model,
                SerialNumber: equipment.Serial,
                Priority: priority,
                TechnicianId: technician.Id,
                TechnicianName: technician.Name,
                ScheduledAt: techSchedule[i].ScheduledAt,
                Status: techSchedule[i].Status,
                Note: priority == "Urgent" ? "Intervention prioritaire pour risque SLA eleve." : "Mission planifiee pour scenario de demonstration PFE."));
        }

        return result;
    }

    private static async Task ResetDemoDataAsync(AppDbContext dbContext, IReadOnlyList<SeedIntervention> seeds, ILogger logger, CancellationToken cancellationToken)
    {
        var interventionIds = seeds.Select(s => InterventionId(s.Number)).ToArray();
        var appointmentIds = seeds.Select(s => AppointmentId(s.Number)).ToArray();
        var planningIds = seeds.Select(s => PlanningRequestId(s.Number)).ToArray();

        dbContext.VisitReports.RemoveRange(await dbContext.VisitReports.Where(x => interventionIds.Contains(x.InterventionId)).ToListAsync(cancellationToken));
        dbContext.InterventionEvidences.RemoveRange(await dbContext.InterventionEvidences.Where(x => interventionIds.Contains(x.InterventionId)).ToListAsync(cancellationToken));
        dbContext.PartsUsed.RemoveRange(await dbContext.PartsUsed.Where(x => interventionIds.Contains(x.InterventionId)).ToListAsync(cancellationToken));
        dbContext.RepairActions.RemoveRange(await dbContext.RepairActions.Where(x => interventionIds.Contains(x.InterventionId)).ToListAsync(cancellationToken));
        dbContext.Diagnostics.RemoveRange(await dbContext.Diagnostics.Where(x => interventionIds.Contains(x.InterventionId)).ToListAsync(cancellationToken));
        dbContext.Interventions.RemoveRange(await dbContext.Interventions.Where(x => interventionIds.Contains(x.Id)).ToListAsync(cancellationToken));
        dbContext.Assignments.RemoveRange(await dbContext.Assignments.Where(x => appointmentIds.Contains(x.AppointmentId)).ToListAsync(cancellationToken));
        dbContext.Appointments.RemoveRange(await dbContext.Appointments.Where(x => appointmentIds.Contains(x.Id)).ToListAsync(cancellationToken));
        dbContext.PlanningRequests.RemoveRange(await dbContext.PlanningRequests.Where(x => planningIds.Contains(x.Id)).ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Intervention demo seed reset completed.");
    }

    private static async Task SeedPlanningAsync(AppDbContext dbContext, IReadOnlyList<SeedIntervention> seeds, ILogger logger, CancellationToken cancellationToken)
    {
        var planningIds = seeds.Select(s => PlanningRequestId(s.Number)).ToArray();
        var appointmentIds = seeds.Select(s => AppointmentId(s.Number)).ToArray();

        var existingPlanning = await dbContext.PlanningRequests.Where(x => planningIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        var existingAppointments = await dbContext.Appointments.Where(x => appointmentIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        var existingAssignments = await dbContext.Assignments.Where(x => appointmentIds.Contains(x.AppointmentId)).Select(x => x.AppointmentId).ToListAsync(cancellationToken);

        var planningSet = existingPlanning.ToHashSet();
        var appointmentSet = existingAppointments.ToHashSet();
        var assignmentSet = existingAssignments.ToHashSet();

        var planningToInsert = new List<PlanningRequest>();
        var appointmentsToInsert = new List<Appointment>();
        var assignmentsToInsert = new List<Assignment>();

        foreach (var seed in seeds)
        {
            var planningId = PlanningRequestId(seed.Number);
            var appointmentId = AppointmentId(seed.Number);

            if (!planningSet.Contains(planningId))
            {
                planningToInsert.Add(new PlanningRequest
                {
                    Id = planningId,
                    ReclamationId = seed.ReclamationId,
                    Reference = $"PLAN-2026-{seed.Number:0000}",
                    SavId = 200,
                    SavName = "Sofia Mansouri",
                    Priority = seed.Priority,
                    ClientId = seed.ClientId,
                    CustomerName = seed.ClientName,
                    CustomerEmail = seed.ClientEmail,
                    CustomerPhone = seed.ClientPhone,
                    ServiceAddress = seed.ServiceAddress,
                    ProductName = seed.ProductName,
                    Brand = seed.Brand,
                    Model = seed.Model,
                    SerialNumber = seed.SerialNumber,
                    RequestedAt = seed.ScheduledAt.AddDays(-2),
                    Status = seed.Status == InterventionStatus.Completed ? PlanningRequestStatus.Satisfied : PlanningRequestStatus.InProgress
                });
            }

            if (!appointmentSet.Contains(appointmentId))
            {
                appointmentsToInsert.Add(new Appointment
                {
                    Id = appointmentId,
                    PlanningRequestId = planningId,
                    ReclamationId = seed.ReclamationId,
                    Reference = $"RDV-2026-{seed.Number:0000}",
                    StartAt = seed.ScheduledAt,
                    EndAt = seed.ScheduledAt.AddHours(2),
                    EstimatedDurationMinutes = 120,
                    TimeZone = "Africa/Tunis",
                    TechnicianId = seed.TechnicianId,
                    TechnicianName = seed.TechnicianName,
                    CustomerPresenceRequired = true,
                    Status = seed.Status == InterventionStatus.Completed ? AppointmentStatus.Completed : seed.Status == InterventionStatus.Aborted ? AppointmentStatus.Cancelled : AppointmentStatus.Confirmed,
                    Sequence = 1,
                    PlanningNote = seed.Note,
                    CreatedAt = seed.ScheduledAt.AddDays(-2),
                    UpdatedAt = seed.ScheduledAt.AddDays(-1)
                });
            }

            if (!assignmentSet.Contains(appointmentId))
            {
                assignmentsToInsert.Add(new Assignment
                {
                    Id = AssignmentId(seed.Number),
                    AppointmentId = appointmentId,
                    TechnicianId = seed.TechnicianId,
                    TechnicianName = seed.TechnicianName,
                    AssignedByUserId = 200,
                    AssignedByRole = "SAV",
                    AssignedAt = seed.ScheduledAt.AddDays(-1),
                    Status = AssignmentStatus.Assigned
                });
            }
        }

        if (planningToInsert.Count > 0) dbContext.PlanningRequests.AddRange(planningToInsert);
        if (appointmentsToInsert.Count > 0) dbContext.Appointments.AddRange(appointmentsToInsert);
        if (assignmentsToInsert.Count > 0) dbContext.Assignments.AddRange(assignmentsToInsert);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Intervention demo planning seed completed. Planning={PlanningCount}, Appointments={AppointmentCount}, Assignments={AssignmentCount}.",
            planningToInsert.Count,
            appointmentsToInsert.Count,
            assignmentsToInsert.Count);
    }

    private static async Task SeedRealisationAsync(AppDbContext dbContext, IReadOnlyList<SeedIntervention> seeds, ILogger logger, CancellationToken cancellationToken)
    {
        var interventionIds = seeds.Select(s => InterventionId(s.Number)).ToArray();
        var existingInterventions = await dbContext.Interventions.Where(x => interventionIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        var existingSet = existingInterventions.ToHashSet();
        var interventionsToInsert = new List<Intervention>();

        foreach (var seed in seeds)
        {
            var id = InterventionId(seed.Number);
            if (existingSet.Contains(id)) continue;

            var startedAt = seed.Status is InterventionStatus.Started or InterventionStatus.Completed ? seed.ScheduledAt.AddMinutes(10) : (DateTime?)null;
            var endedAt = seed.Status == InterventionStatus.Completed ? seed.ScheduledAt.AddHours(2) : (DateTime?)null;
            interventionsToInsert.Add(new Intervention
            {
                Id = id,
                AppointmentId = AppointmentId(seed.Number),
                ReclamationId = seed.ReclamationId,
                ClientId = seed.ClientId,
                Reference = $"INT-2026-{seed.Number:0000}",
                TechnicianId = seed.TechnicianId,
                TechnicianName = seed.TechnicianName,
                StartedAt = startedAt,
                EndedAt = endedAt,
                Status = seed.Status,
                Outcome = seed.Status == InterventionStatus.Completed ? InterventionOutcome.Solved : null,
                NeedsReplanning = false,
                CreatedAt = seed.ScheduledAt.AddDays(-1),
                UpdatedAt = endedAt ?? DateTime.UtcNow
            });
        }

        if (interventionsToInsert.Count > 0)
        {
            dbContext.Interventions.AddRange(interventionsToInsert);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SeedRealisationDetailsAsync(dbContext, seeds, logger, cancellationToken);
        logger.LogInformation("Intervention demo realisation seed completed. Interventions={InsertedCount}.", interventionsToInsert.Count);
    }

    private static async Task SeedRealisationDetailsAsync(AppDbContext dbContext, IReadOnlyList<SeedIntervention> seeds, ILogger logger, CancellationToken cancellationToken)
    {
        var detailsSeeds = seeds.Where(s => s.Status is InterventionStatus.Started or InterventionStatus.Completed).ToList();
        var interventionIds = detailsSeeds.Select(s => InterventionId(s.Number)).ToArray();

        var diagnosticsExisting = await dbContext.Diagnostics.Where(x => interventionIds.Contains(x.InterventionId)).Select(x => x.InterventionId).ToListAsync(cancellationToken);
        var reportsExisting = await dbContext.VisitReports.Where(x => interventionIds.Contains(x.InterventionId)).Select(x => x.InterventionId).ToListAsync(cancellationToken);
        var diagnosticsSet = diagnosticsExisting.ToHashSet();
        var reportsSet = reportsExisting.ToHashSet();

        var diagnostics = new List<Diagnostic>();
        var actions = new List<RepairAction>();
        var parts = new List<PartUsed>();
        var reports = new List<VisitReport>();
        var evidences = new List<InterventionEvidence>();

        foreach (var seed in detailsSeeds)
        {
            var interventionId = InterventionId(seed.Number);
            if (!diagnosticsSet.Contains(interventionId))
            {
                diagnostics.Add(new Diagnostic
                {
                    Id = DiagnosticId(seed.Number),
                    InterventionId = interventionId,
                    Category = seed.Priority == "Urgent" ? "Critical" : "Technical",
                    Summary = $"Diagnostic realise sur {seed.ProductName}.",
                    RootCause = seed.Status == InterventionStatus.Completed ? "Defaut identifie et corrige pendant la visite." : "Analyse en cours par le technicien.",
                    RequiresParts = seed.Number % 3 == 0,
                    RequiresFollowUp = seed.Status != InterventionStatus.Completed,
                    CreatedAt = seed.ScheduledAt.AddMinutes(20)
                });

                actions.Add(new RepairAction
                {
                    Id = RepairActionId(seed.Number),
                    InterventionId = interventionId,
                    ActionType = seed.Status == InterventionStatus.Completed ? "Repair" : "Diagnostic",
                    Description = seed.Status == InterventionStatus.Completed
                        ? "Controle, remplacement mineur et tests de validation effectues."
                        : "Controle initial et securisation de l'equipement en cours.",
                    StartedAt = seed.ScheduledAt.AddMinutes(15),
                    CompletedAt = seed.Status == InterventionStatus.Completed ? seed.ScheduledAt.AddHours(1) : null,
                    Success = seed.Status == InterventionStatus.Completed
                });

                if (seed.Number % 3 == 0)
                {
                    parts.Add(new PartUsed
                    {
                        Id = PartId(seed.Number),
                        InterventionId = interventionId,
                        PartCode = $"PCE-{seed.Number:0000}",
                        Label = "Filtre / composant de maintenance demo",
                        Quantity = 1,
                        AvailabilityStatus = "Used"
                    });
                }

                evidences.Add(new InterventionEvidence
                {
                    Id = EvidenceId(seed.Number),
                    InterventionId = interventionId,
                    Kind = "Photo",
                    Url = $"demo://interventions/INT-2026-{seed.Number:0000}/photo-1.jpg",
                    CapturedAt = seed.ScheduledAt.AddMinutes(45),
                    CapturedByUserId = seed.TechnicianId,
                    CapturedByRole = "ST"
                });
            }

            if (!reportsSet.Contains(interventionId) && seed.Number <= 17)
            {
                var published = seed.Status == InterventionStatus.Completed;
                reports.Add(new VisitReport
                {
                    Id = ReportId(seed.Number),
                    InterventionId = interventionId,
                    Summary = published
                        ? "Intervention terminee. Tests fonctionnels realises avec succes et client informe."
                        : "Brouillon de rapport: diagnostic en cours, rapport a completer apres intervention.",
                    Outcome = published ? InterventionOutcome.Solved : InterventionOutcome.NeedsPart,
                    CustomerPresent = seed.Number % 2 == 0,
                    NextStep = published ? "Suivi SAV dans 48h." : "Completer le rapport apres finalisation des actions.",
                    Status = published ? VisitReportStatus.Published : VisitReportStatus.Draft,
                    PublishedAt = published ? seed.ScheduledAt.AddHours(2) : null,
                    CreatedAt = seed.ScheduledAt.AddHours(1)
                });
            }
        }

        if (diagnostics.Count > 0) dbContext.Diagnostics.AddRange(diagnostics);
        if (actions.Count > 0) dbContext.RepairActions.AddRange(actions);
        if (parts.Count > 0) dbContext.PartsUsed.AddRange(parts);
        if (evidences.Count > 0) dbContext.InterventionEvidences.AddRange(evidences);
        if (reports.Count > 0) dbContext.VisitReports.AddRange(reports);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Intervention demo details seed completed. Diagnostics={Diagnostics}, Actions={Actions}, Parts={Parts}, Reports={Reports}.",
            diagnostics.Count,
            actions.Count,
            parts.Count,
            reports.Count);
    }

    private static Guid PlanningRequestId(int number) => GuidFromNumber(1000 + number);
    private static Guid AppointmentId(int number) => GuidFromNumber(2000 + number);
    private static Guid AssignmentId(int number) => GuidFromNumber(3000 + number);
    private static Guid InterventionId(int number) => GuidFromNumber(4000 + number);
    private static Guid DiagnosticId(int number) => GuidFromNumber(5000 + number);
    private static Guid RepairActionId(int number) => GuidFromNumber(6000 + number);
    private static Guid PartId(int number) => GuidFromNumber(7000 + number);
    private static Guid EvidenceId(int number) => GuidFromNumber(8000 + number);
    private static Guid ReportId(int number) => GuidFromNumber(9000 + number);

    private static Guid GuidFromNumber(int number)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{number:000000000000}");
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
