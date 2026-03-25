using ReclamationService.Domain.Entities;

namespace ReclamationService.Domain.Interfaces
{
    public interface IClientRepository
    {
        Client? GetById(long id);
        void Add(Client client);
        void Update(Client client);
    }
}
