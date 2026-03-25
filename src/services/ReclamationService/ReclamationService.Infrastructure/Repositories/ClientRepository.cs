using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;
using ReclamationService.Infrastructure.Data;

namespace ReclamationService.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(Client client)
        {
            _context.Clients.Add(client);
            _context.SaveChanges();
        }

        public Client? GetById(long id)
        {
            return _context.Clients.FirstOrDefault(c => c.Id == id);
        }

        public void Update(Client client)
        {
            _context.Clients.Update(client);
            _context.SaveChanges();
        }
    }
}
