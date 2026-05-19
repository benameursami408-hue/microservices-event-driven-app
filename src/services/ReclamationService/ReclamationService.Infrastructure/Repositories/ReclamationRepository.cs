using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using ReclamationService.Domain.Enums;
using ReclamationService.Infrastructure.Data;

namespace ReclamationService.Infrastructure.Repositories
{
    public class ReclamationRepository : IReclamationRepository
    {
        private readonly AppDbContext _context;

        // Injection du DbContext via le constructeur
        public ReclamationRepository(AppDbContext context)
        {
            _context = context;
        }
        
        public Reclamation Create(Reclamation reclamation)
        {
            _context.Reclamations.Add(reclamation);
            _context.SaveChanges();
            return reclamation;
        }

        public void Delete(long id)
        {
            var reclamation = _context.Reclamations.FirstOrDefault(u => u.Id == id);
            if (reclamation != null)
            {
                var history = _context.ReclamationHistories
                    .Where(h => h.ReclamationId == id)
                    .ToList();

                if (history.Count != 0)
                {
                    _context.ReclamationHistories.RemoveRange(history);
                }

                _context.Reclamations.Remove(reclamation);
                _context.SaveChanges();
            }
        }

        public List<Reclamation> GetAll()
        {
            return _context.Reclamations
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public IQueryable<Reclamation> Query()
        {
            return _context.Reclamations.AsNoTracking();
        }

        public List<Reclamation> GetForClient(long clientId)
        {
            return _context.Reclamations
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public List<Reclamation> GetOpenBacklog()
        {
            return _context.Reclamations
                .Where(r => r.Status == ReclamationStatus.Open)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public List<Reclamation> GetForSav(long savId)
        {
            return _context.Reclamations
                .Where(r => r.SAVId == savId || r.ClaimedBySavId == savId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public List<Reclamation> GetForTechnician(long technicianId)
        {
            return _context.Reclamations
                .Where(r => r.TechnicianId == technicianId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public List<Reclamation> GetByStatus(ReclamationStatus status)
        {
            return _context.Reclamations
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public Reclamation? GetById(long id)
        {
            return _context.Reclamations.FirstOrDefault(u => u.Id == id);
        }

        public List<Reclamation> GetByPriority(NamePriority priority)
        {
            return _context.Reclamations.Where(u => u.Priority == priority).ToList();
        }

        public Reclamation? GetByRefernce(string reference)
        {
            return _context.Reclamations.FirstOrDefault(u => u.Reference == reference);
        }

        public Reclamation Update(Reclamation reclamation)
        {
            _context.Reclamations.Update(reclamation);
            _context.SaveChanges();
            return reclamation;
        }

        public Task<int> ClaimIfAvailableAsync(long id, long savId, string savName, DateTime claimedAt, CancellationToken cancellationToken = default)
        {
            return _context.Reclamations
                .Where(r => r.Id == id
                    && r.ClaimedBySavId == null
                    && r.Status != ReclamationStatus.Closed
                    && r.Status != ReclamationStatus.Cancelled
                    && r.Status != ReclamationStatus.Rejected)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.ClaimedBySavId, savId)
                    .SetProperty(r => r.ClaimedBySavName, savName)
                    .SetProperty(r => r.ClaimedAt, claimedAt)
                    .SetProperty(r => r.ReleasedAt, (DateTime?)null)
                    .SetProperty(r => r.UpdatedAt, claimedAt),
                    cancellationToken);
        }
    }
}
