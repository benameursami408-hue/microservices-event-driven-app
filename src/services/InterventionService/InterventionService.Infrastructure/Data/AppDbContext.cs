using InterventionService.Domain.Entities;
using InterventionService.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace InterventionService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<PlanningRequest> PlanningRequests => Set<PlanningRequest>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<RescheduleRequest> RescheduleRequests => Set<RescheduleRequest>();
    public DbSet<Intervention> Interventions => Set<Intervention>();
    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();
    public DbSet<RepairAction> RepairActions => Set<RepairAction>();
    public DbSet<PartUsed> PartsUsed => Set<PartUsed>();
    public DbSet<InterventionEvidence> InterventionEvidences => Set<InterventionEvidence>();
    public DbSet<VisitReport> VisitReports => Set<VisitReport>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlanningRequest>().ToTable("PlanningRequests", "planning");
        modelBuilder.Entity<Appointment>().ToTable("Appointments", "planning");
        modelBuilder.Entity<Assignment>().ToTable("Assignments", "planning");
        modelBuilder.Entity<RescheduleRequest>().ToTable("RescheduleRequests", "planning");
        modelBuilder.Entity<Intervention>().ToTable("Interventions", "realisation");
        modelBuilder.Entity<Diagnostic>().ToTable("Diagnostics", "realisation");
        modelBuilder.Entity<RepairAction>().ToTable("RepairActions", "realisation");
        modelBuilder.Entity<PartUsed>().ToTable("PartsUsed", "realisation");
        modelBuilder.Entity<InterventionEvidence>().ToTable("InterventionEvidences", "realisation");
        modelBuilder.Entity<VisitReport>().ToTable("VisitReports", "realisation");
        modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages", "integration");
        modelBuilder.Entity<ProcessedIntegrationEvent>().ToTable("ProcessedIntegrationEvents", "integration");

        modelBuilder.Entity<PlanningRequest>()
            .HasMany(x => x.Appointments)
            .WithOne(x => x.PlanningRequest)
            .HasForeignKey(x => x.PlanningRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasMany(x => x.Assignments)
            .WithOne(x => x.Appointment)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Intervention>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlanningRequest>().HasIndex(x => x.ReclamationId);
        modelBuilder.Entity<Appointment>().HasIndex(x => new { x.ReclamationId, x.Status, x.Sequence });
        modelBuilder.Entity<Intervention>().HasIndex(x => new { x.AppointmentId, x.ReclamationId });
    }
}
