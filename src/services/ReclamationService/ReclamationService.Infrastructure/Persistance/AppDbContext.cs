using Microsoft.EntityFrameworkCore;
using ReclamationService.Domain.Entities;

namespace ReclamationService.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Reclamation> Reclamations { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
    }
}
