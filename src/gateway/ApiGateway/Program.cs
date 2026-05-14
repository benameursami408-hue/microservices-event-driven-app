using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            policy.WithOrigins("http://localhost:5173");
        }

        policy.WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Correlation-ID");
        policy.WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS");
        policy.AllowCredentials();
    });
});

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ApiGateway.CorrelationIdMiddleware>();
app.UseMiddleware<ApiGateway.SecurityHeadersMiddleware>();

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/health"),
    branch => branch.Run(async context =>
    {
        var path = context.Request.Path.Value?.TrimEnd('/') ?? string.Empty;

        object response;
        var statusCode = StatusCodes.Status200OK;

        if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase))
        {
            response = new { status = "Healthy", service = "ApiGateway" };
        }
        else if (string.Equals(path, "/health/live", StringComparison.OrdinalIgnoreCase))
        {
            response = new { status = "Live", service = "ApiGateway" };
        }
        else if (string.Equals(path, "/health/ready", StringComparison.OrdinalIgnoreCase))
        {
            response = new { status = "Ready", service = "ApiGateway", checks = new { ocelot = "Configured" } };
        }
        else
        {
            statusCode = StatusCodes.Status404NotFound;
            response = new { status = 404, title = "Not Found", detail = "Gateway health endpoint not found." };
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }));

app.UseCors("GatewayCors");

await app.UseOcelot();

app.Run();
