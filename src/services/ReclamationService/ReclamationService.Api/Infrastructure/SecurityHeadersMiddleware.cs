using Microsoft.Net.Http.Headers;

namespace ReclamationService.Api.Infrastructure;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            if (!_environment.IsDevelopment())
            {
                headers[HeaderNames.ContentSecurityPolicy] = "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'";
                if (context.Request.IsHttps)
                {
                    headers[HeaderNames.StrictTransportSecurity] = "max-age=31536000; includeSubDomains";
                }
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
