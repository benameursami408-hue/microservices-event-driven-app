using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Application.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User user)
    {
        if (user == null)
        {
            return null!;
        }

        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Email = user.Email,
            IsActive = user.IsActive,
            Role = user.Role
        };
    }

    public static User ToEntity(this RegisterDto dto)
    {
        if (dto == null)
        {
            return null!;
        }

        return new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            Email = dto.Email,
            Password = dto.Password, // Usually overwritten by hashed password later in the service
            Role = UserRole.CLIENT,
            IsActive = true // Default state for new registrations
        };
    }

    public static User ToEntity(this CreateUserDto dto)
    {
        if (dto == null)
        {
            return null!;
        }

        return new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            Email = dto.Email,
            Password = dto.Password,
            Role = dto.Role,
            IsActive = dto.IsActive
        };
    }
}
