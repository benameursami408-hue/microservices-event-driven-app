using ReclamationService.Domain.Entities;

namespace ReclamationService.Domain.Interfaces;

public interface IServiceUserRepository
{
    ServiceUser? GetById(long id);
    void Upsert(ServiceUser user);
}
