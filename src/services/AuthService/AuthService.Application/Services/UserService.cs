using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public List<User> GetAll()
        {
            return _userRepository.GetAll();
        }

        public User? GetById(long id)
        {
            return _userRepository.GetById(id);
        }

        public User? GetByEmail(string email)
        {
            return _userRepository.GetByEmail(email);
        }

        public User? GetByPhoneNumber(string phoneNumber)
        {
            return _userRepository.GetByPhoneNumber(phoneNumber);
        }

        public User Create(User user)
        {
            return _userRepository.Create(user);
        }

        public User Update(User user)
        {
            return _userRepository.Update(user);
        }

        public void Delete(long id)
        {
            _userRepository.Delete(id);
        }
    }
}
