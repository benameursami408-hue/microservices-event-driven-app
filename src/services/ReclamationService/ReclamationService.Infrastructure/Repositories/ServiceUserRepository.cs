using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;
using ReclamationService.Infrastructure.Data;

namespace ReclamationService.Infrastructure.Repositories;

public class ServiceUserRepository : IServiceUserRepository
{
    private readonly AppDbContext _context;

    public ServiceUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public ServiceUser? GetById(long id)
    {
        return _context.ServiceUsers.FirstOrDefault(user => user.Id == id);
    }

    public void Upsert(ServiceUser user)
    {
        var existing = _context.ServiceUsers.FirstOrDefault(item => item.Id == user.Id);
        if (existing is null)
        {
            _context.ServiceUsers.Add(user);
        }
        else
        {
            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.Role = user.Role;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        _context.SaveChanges();
    }
}
