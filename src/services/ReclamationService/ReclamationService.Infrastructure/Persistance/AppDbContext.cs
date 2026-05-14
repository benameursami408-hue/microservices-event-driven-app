using Microsoft.EntityFrameworkCore;
using ReclamationService.Domain.Entities;
using ReclamationService.Infrastructure.Outbox;

namespace ReclamationService.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Reclamation> Reclamations { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<AiPriorityAnalysis> AiPriorityAnalyses { get; set; } = null!;
        public DbSet<ReclamationHistory> ReclamationHistories { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    }
}
