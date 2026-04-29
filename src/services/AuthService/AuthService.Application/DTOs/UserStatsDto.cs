using AuthService.Domain.Enums;

namespace AuthService.Application.DTOs;

public class UserStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public List<UserRoleCountDto> ByRole { get; set; } = new();
    public List<UserRoleCountDto> ActiveByRole { get; set; } = new();
}

public class UserRoleCountDto
{
    public UserRole Role { get; set; }
    public int Count { get; set; }
}
