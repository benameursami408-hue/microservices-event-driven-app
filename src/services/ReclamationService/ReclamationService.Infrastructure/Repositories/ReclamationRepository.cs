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
                _context.Reclamations.Remove(reclamation);
                _context.SaveChanges();
            }
        }

        public List<Reclamation> GetAll()
        {
            return _context.Reclamations.ToList();
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
    }
}
