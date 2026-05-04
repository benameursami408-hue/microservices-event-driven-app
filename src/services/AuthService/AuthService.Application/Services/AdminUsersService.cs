using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.Mappers;
using AuthService.Application.Outbox;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using SharedEvents.Events;
using Microsoft.EntityFrameworkCore;

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
        NormalizeCreateRequest(request);

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

    public async Task<UserDto> UpdateAsync(long id, UpdateUserDto request)
    {
        NormalizeUpdateRequest(request);

        var existing = _userRepository.GetById(id);
        if (existing == null)
        {
            throw new NotFoundException("User not found.");
        }

        var emailOwner = await _userRepository.GetByEmailAsync(request.Email);
        if (emailOwner != null && emailOwner.Id != id)
        {
            throw new BadRequestException("This email is already registered.");
        }

        ApplyCommonFields(existing, request);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            existing.Password = _passwordHasher.Hash(request.Password.Trim());
        }

        _userRepository.Update(existing);
        return existing.ToDto();
    }

    public void Delete(long id, long? currentUserId = null)
    {
        if (currentUserId.HasValue && currentUserId.Value == id)
        {
            throw new BadRequestException("You cannot delete your own admin account.");
        }

        var existing = _userRepository.GetById(id);
        if (existing == null)
        {
            throw new NotFoundException("User not found.");
        }

        _userRepository.Delete(id);
    }

    public async Task<UserStatsDto> GetStatsAsync()
    {
        var query = _userRepository.Query();

        var total = await query.CountAsync();
        var active = await query.CountAsync(u => u.IsActive);

        var byRole = await query
            .GroupBy(u => u.Role)
            .Select(g => new UserRoleCountDto { Role = g.Key, Count = g.Count() })
            .ToListAsync();

        var activeByRole = await query
            .Where(u => u.IsActive)
            .GroupBy(u => u.Role)
            .Select(g => new UserRoleCountDto { Role = g.Key, Count = g.Count() })
            .ToListAsync();

        return new UserStatsDto
        {
            Total = total,
            Active = active,
            Inactive = total - active,
            ByRole = byRole.OrderBy(x => x.Role).ToList(),
            ActiveByRole = activeByRole.OrderBy(x => x.Role).ToList()
        };
    }

    private static void NormalizeCreateRequest(CreateUserDto request)
    {
        request.FirstName = NormalizeRequiredText(request.FirstName, nameof(request.FirstName));
        request.LastName = NormalizeRequiredText(request.LastName, nameof(request.LastName));
        request.PhoneNumber = NormalizeRequiredText(request.PhoneNumber, nameof(request.PhoneNumber));
        request.Address = NormalizeOptionalText(request.Address);
        request.Email = NormalizeEmail(request.Email);
        request.Password = request.Password.Trim();
    }

    private static void NormalizeUpdateRequest(UpdateUserDto request)
    {
        request.FirstName = NormalizeRequiredText(request.FirstName, nameof(request.FirstName));
        request.LastName = NormalizeRequiredText(request.LastName, nameof(request.LastName));
        request.PhoneNumber = NormalizeRequiredText(request.PhoneNumber, nameof(request.PhoneNumber));
        request.Address = NormalizeOptionalText(request.Address);
        request.Email = NormalizeEmail(request.Email);
        request.Password = string.IsNullOrWhiteSpace(request.Password) ? null : request.Password.Trim();
    }

    private static void ApplyCommonFields(User existing, UpdateUserDto request)
    {
        existing.FirstName = request.FirstName;
        existing.LastName = request.LastName;
        existing.PhoneNumber = request.PhoneNumber;
        existing.Address = request.Address;
        existing.Email = request.Email;
        existing.Role = request.Role;
        existing.IsActive = request.IsActive;
    }

    private static string NormalizeEmail(string value)
    {
        var normalized = NormalizeRequiredText(value, "Email").ToLowerInvariant();
        return normalized;
    }

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BadRequestException($"{fieldName} is required.");
        }

        return normalized;
    }

    private static string NormalizeOptionalText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
