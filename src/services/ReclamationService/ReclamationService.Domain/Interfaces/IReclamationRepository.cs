
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Domain.Interfaces
{
    public interface IReclamationRepository
    {
        public List<Reclamation> GetAll();
        public List<Reclamation> GetForClient(long clientId);
        public List<Reclamation> GetOpenBacklog();
        public List<Reclamation> GetForSav(long savId);
        public List<Reclamation> GetForTechnician(long technicianId);
        public List<Reclamation> GetByStatus(ReclamationStatus status);
        public Reclamation? GetById(long id);
        public Reclamation? GetByRefernce(string reference);
        public List<Reclamation> GetByPriority(NamePriority priority);
        public Reclamation Create(Reclamation reclamation);
        public Reclamation Update(Reclamation reclamation);
        public void Delete(long id);
    }
}
