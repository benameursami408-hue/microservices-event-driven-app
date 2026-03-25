using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces
{
    public interface IUserRepository
    {
        public List<User> GetAll();
        public User? GetById(long id);
        public User? GetByEmail(string email);
        public Task<User?> GetByEmailAsync(string email);
        public User? GetByPhoneNumber(string phoneNumber);
        public User Create(User user);
        public Task<User> AddAsync(User user);
        public User Update(User user);
        public void Delete(long id);
    }
}
