using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.Mappers;
using AuthService.Application.Outbox;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using SharedEvents.Events;

namespace AuthService.Application.Services;

public class AdminUsersService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOutboxWriter _outboxWriter;

    public AdminUsersService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IOutboxWriter outboxWriter)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _outboxWriter = outboxWriter;
    }

    public List<UserDto> GetAll(UserRole? role = null)
    {
        var items = _userRepository.GetAll();
        if (role.HasValue)
        {
            items = items.Where(u => u.Role == role.Value).ToList();
        }

        return items.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto request)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing != null)
        {
            throw new BadRequestException("This email is already registered.");
        }

        var user = request.ToEntity();
        user.Password = _passwordHasher.Hash(request.Password);

        await _userRepository.AddAsync(user);

        await _outboxWriter.EnqueueAsync(new UserCreatedEvent
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            OccurredAt = DateTime.UtcNow
        });

        return user.ToDto();
    }
}
