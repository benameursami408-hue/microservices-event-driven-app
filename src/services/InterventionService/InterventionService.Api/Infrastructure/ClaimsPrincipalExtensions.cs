using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InterventionService.Application.Security;

namespace InterventionService.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal, HttpContext httpContext)
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

        var firstName = principal.FindFirstValue("firstName") ?? string.Empty;
        var lastName = principal.FindFirstValue("lastName") ?? string.Empty;
        var fullName = $"{firstName} {lastName}".Trim();
        var correlationId = httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationValue)
            ? correlationValue.ToString()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = httpContext.TraceIdentifier;
        }

        httpContext.Response.Headers["X-Correlation-ID"] = correlationId;
        return new CurrentUser(userId, email, fullName, role, correlationId);
    }
}
