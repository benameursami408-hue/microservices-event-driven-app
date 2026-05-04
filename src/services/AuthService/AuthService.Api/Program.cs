using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AuthService.Infrastructure.Data;
using AuthService.Application.Services;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Infrastructure.Authentication;
using AuthService.Application.Interfaces;
using MassTransit;
using AuthService.Application.Outbox;
using AuthService.Infrastructure.Outbox;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using AuthService.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
const string localDevelopmentUrl = "http://localhost:5165";
var requestedUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? builder.Configuration["urls"];
var isRunningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);
string? localUrlOverrideReason = null;

if (!isRunningInContainer)
{
    if (string.IsNullOrWhiteSpace(requestedUrls))
    {
        builder.WebHost.UseUrls(localDevelopmentUrl);
        localUrlOverrideReason = "no ASPNETCORE_URLS or urls setting was provided";
    }
    else if (requestedUrls.Contains(":5000", StringComparison.OrdinalIgnoreCase))
    {
        builder.WebHost.UseUrls(localDevelopmentUrl);
        localUrlOverrideReason = $"the requested URL '{requestedUrls}' uses port 5000 reserved for the API Gateway";
    }
}

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.UseMessageRetry(retry => retry.Exponential(
            retryLimit: 3,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(2)));

        cfg.ConfigureEndpoints(context);
    });
});


builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Invalid value."
                            : error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more fields are invalid."
            });
        };
    });
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddExceptionHandler<AuthService.Api.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DBConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminUsersService>();
builder.Services.AddScoped<IAuthService, AuthenticationService>();
builder.Services.AddScoped<IOutboxWriter, EfOutboxWriter>();
builder.Services.AddHostedService<AuthService.Api.Infrastructure.OutboxDispatcher>();

// JWT Configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var authRateLimitPermit = builder.Configuration.GetValue<int?>("Security:AuthRateLimit:PermitLimit") ?? 10;
var authRateLimitWindowSeconds = builder.Configuration.GetValue<int?>("Security:AuthRateLimit:WindowSeconds") ?? 60;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AuthSensitive", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitPermit,
                Window = TimeSpan.FromSeconds(authRateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});


var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (!builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Contains("ChangeThis", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Jwt:SecretKey must be provided securely for non-development environments.");
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (localUrlOverrideReason is not null)
{
    app.Logger.LogWarning(
        "AuthService local startup URL was overridden because {Reason}. " +
        "The service is listening on {LocalDevelopmentUrl} instead.",
        localUrlOverrideReason,
        localDevelopmentUrl);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/health/live", () => Results.Ok(new { status = "Live", service = "AuthService" })).AllowAnonymous();
app.MapGet("/health/ready", async (AppDbContext dbContext, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var sqlHealthy = false;
    string? sqlError = null;
    try
    {
        sqlHealthy = await dbContext.Database.CanConnectAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        sqlError = ex.Message;
    }

    var rabbitHostConfigured = !string.IsNullOrWhiteSpace(configuration["RabbitMQ:Host"]);
    var rabbitUserConfigured = !string.IsNullOrWhiteSpace(configuration["RabbitMQ:Username"]);
    var rabbitPasswordConfigured = !string.IsNullOrWhiteSpace(configuration["RabbitMQ:Password"]);
    var ready = sqlHealthy && rabbitHostConfigured && rabbitUserConfigured && rabbitPasswordConfigured;

    var response = new
    {
        status = ready ? "Ready" : "NotReady",
        service = "AuthService",
        checks = new
        {
            sqlServer = sqlHealthy ? "Healthy" : "Unhealthy",
            sqlError,
            rabbitMqConfiguration = rabbitHostConfigured && rabbitUserConfigured && rabbitPasswordConfigured ? "Configured" : "MissingConfiguration"
        }
    };

    IResult result = ready
        ? Results.Ok(response)
        : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    return result;
}).AllowAnonymous();

app.MapControllers();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AuthService.DatabaseMigration");
    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            logger.LogInformation("Applying EF Core migrations (attempt {Attempt}/{MaxAttempts})...", attempt, maxAttempts);
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("EF Core migrations applied successfully.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delaySeconds = Math.Min(30, attempt * 2);
            logger.LogWarning(ex, "Migration attempt {Attempt} failed; retrying in {DelaySeconds}s", attempt, delaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }

    // Optional dev seeding (use env vars / appsettings overrides)
    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        using var seedScope = app.Services.CreateScope();
        var seedDbContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await TestDataSeeder.SeedUsersAsync(seedDbContext, passwordHasher, logger, app.Configuration, app.Environment);
    }

    using var outboxScope = app.Services.CreateScope();
    var outboxDbContext = outboxScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await outboxDbContext.Database.ExecuteSqlRawAsync(
        """
        IF OBJECT_ID(N'[dbo].[OutboxMessages]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[OutboxMessages](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [ClrType] nvarchar(1024) NOT NULL,
                [EventType] nvarchar(200) NOT NULL,
                [EventVersion] int NOT NULL,
                [CorrelationId] nvarchar(200) NOT NULL,
                [CausationId] nvarchar(200) NULL,
                [Producer] nvarchar(100) NOT NULL,
                [OccurredAt] datetime2 NOT NULL,
                [Payload] nvarchar(max) NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [ProcessedAt] datetime2 NULL,
                [RetryCount] int NOT NULL CONSTRAINT [DF_AuthOutboxMessages_RetryCount] DEFAULT(0),
                [LastError] nvarchar(2000) NULL
            );

            CREATE INDEX [IX_AuthOutboxMessages_ProcessedAt_CreatedAt]
                ON [dbo].[OutboxMessages]([ProcessedAt], [CreatedAt]);
        END
        """);
}

app.Run();
