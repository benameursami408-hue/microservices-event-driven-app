using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;
using ReclamationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ReclamationService.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Client> GetAll()
        {
            return _context.Clients.AsNoTracking().OrderBy(c => c.FullName).ToList();
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

        public Client? GetByEmail(string email)
        {
            return _context.Clients.FirstOrDefault(c => c.Email == email);
        }

        public long GetNextId()
        {
            var currentMax = _context.Clients.Select(c => (long?)c.Id).Max() ?? 1000;
            return currentMax + 1;
        }

        public void Update(Client client)
        {
            _context.Clients.Update(client);
            _context.SaveChanges();
        }
    }
}
