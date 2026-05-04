using System.ComponentModel.DataAnnotations;
using AuthService.Domain.Enums;

namespace AuthService.Application.DTOs;

public class UpdateUserDto
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(50, ErrorMessage = "First name must not exceed 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(50, ErrorMessage = "Last name must not exceed 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [MaxLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    [RegularExpression(@"^\+?[0-9][0-9\s().-]{5,19}$", ErrorMessage = "Phone number format is invalid. Example: +216 22 000 000.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(250, ErrorMessage = "Address must not exceed 250 characters.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [MaxLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [EnumDataType(typeof(UserRole), ErrorMessage = "Role must be CLIENT, SAV, ADMIN, or ST.")]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
}
