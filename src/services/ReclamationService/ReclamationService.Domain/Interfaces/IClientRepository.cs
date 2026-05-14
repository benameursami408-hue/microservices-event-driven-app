using ReclamationService.Domain.Entities;

namespace ReclamationService.Domain.Interfaces
{
    public interface IClientRepository
    {
        List<Client> GetAll();
        Client? GetById(long id);
        Client? GetByEmail(string email);
        long GetNextId();
        void Add(Client client);
        void Update(Client client);
    }
}
