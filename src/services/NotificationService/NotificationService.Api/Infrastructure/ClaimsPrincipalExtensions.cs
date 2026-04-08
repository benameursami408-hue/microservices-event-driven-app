using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NotificationService.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? principal.FindFirstValue("sub");

        _ = long.TryParse(idValue, out var userId);

        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                    ?? principal.FindFirstValue("email")
                    ?? principal.FindFirstValue(ClaimTypes.Email)
                    ?? string.Empty;

        var role = principal.FindFirstValue(ClaimTypes.Role)
                   ?? principal.FindFirstValue("role")
                   ?? string.Empty;

        return new CurrentUser(userId, email, role);
    }
}
