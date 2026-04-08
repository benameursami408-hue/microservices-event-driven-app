namespace NotificationService.Api.Infrastructure;

public record CurrentUser(long UserId, string Email, string Role)
{
    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
}
