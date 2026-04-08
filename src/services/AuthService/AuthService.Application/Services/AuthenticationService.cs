using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.Mappers;
using AuthService.Application.Outbox;
using AuthService.Domain.Interfaces;
using SharedEvents.Events;

namespace AuthService.Application.Services;

public class AuthenticationService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IOutboxWriter _outboxWriter;

    public AuthenticationService(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher, 
        IJwtProvider jwtProvider,
        IOutboxWriter outboxWriter)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _outboxWriter = outboxWriter;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto request)
    {
        // 1. Check if email is already in use
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new BadRequestException("This email is already registered.");
        }

        // 2. Map DTO to User Domain Entity
        var user = request.ToEntity();

        // 3. Hash the password before saving
        user.Password = _passwordHasher.Hash(request.Password);

        // 4. Save to database
        await _userRepository.AddAsync(user);

        // Persist event to outbox for reliable async dispatch.
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

        // 5. Return safe DTO without password
        return user.ToDto();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        // 1. Find user by email
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            // Use generic message to prevent User Enumeration
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 2. Verify password hash
        bool isPasswordValid = _passwordHasher.Verify(user.Password, request.Password);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("This account is deactivated.");
        }

        // 3. Generate JWT Token
        string token = _jwtProvider.GenerateToken(user);

        // 4. Return Token + User Profile
        return new AuthResponseDto
        {
            Token = token,
            User = user.ToDto()
        };
    }
}
