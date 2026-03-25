
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Domain.Interfaces
{
    public interface IReclamationRepository
    {
        public List<Reclamation> GetAll();
        public Reclamation? GetById(long id);
        public Reclamation? GetByRefernce(string reference);
        public List<Reclamation> GetByPriority(NamePriority priority);
        public Reclamation Create(Reclamation reclamation);
        public Reclamation Update(Reclamation reclamation);
        public void Delete(long id);
    }
}
