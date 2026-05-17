using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Configuration;
using NotificationService.Application.Consumers;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services;
using NotificationService.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notification", includeNamespace: false));
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<ReclamationCreatedConsumer>();
    x.AddConsumer<ReclamationAssignedConsumer>();
    x.AddConsumer<ReclamationPlannedConsumer>();
    x.AddConsumer<ReclamationStatusChangedConsumer>();
    x.AddConsumer<ReclamationPriorityUpdatedConsumer>();
    x.AddConsumer<TechnicianAssignedConsumer>();
    x.AddConsumer<AppointmentConfirmedConsumer>();
    x.AddConsumer<AppointmentRescheduledConsumer>();
    x.AddConsumer<AppointmentCancelledConsumer>();
    x.AddConsumer<SlaNearBreachDetectedConsumer>();
    x.AddConsumer<SlaBreachedConsumer>();
    x.AddConsumer<PlanningConflictDetectedConsumer>();
    x.AddConsumer<InterventionStartedConsumer>();
    x.AddConsumer<RealisationReportedConsumer>();
    x.AddConsumer<ReplanningRequiredConsumer>();

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
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

builder.Services.AddExceptionHandler<NotificationService.Api.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));

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

// JWT auth (validates tokens issued by AuthService)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token)
                    && context.Request.Cookies.TryGetValue("sav_access_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSavProFrontend", policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationSender, LoggingNotificationSender>();
builder.Services.AddScoped<IEventIdempotencyStore, EventIdempotencyStore>();
builder.Services.AddScoped<IdempotentConsumerRunner>();
builder.Services.AddScoped<NotificationWorkflow>();

var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (!builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Contains("ChangeThis", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Jwt:SecretKey must be provided securely for non-development environments.");
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowSavProFrontend");
app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/health/live", () => Results.Ok(new { status = "Live", service = "NotificationService" })).AllowAnonymous();
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
        service = "NotificationService",
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
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("NotificationService.DatabaseMigration");
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

    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        using var seedScope = app.Services.CreateScope();
        var seedDbContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.SeedAsync(seedDbContext, logger, app.Configuration);
    }

    using var schemaScope = app.Services.CreateScope();
    var schemaDbContext = schemaScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await schemaDbContext.Database.ExecuteSqlRawAsync(
        """
        IF COL_LENGTH('dbo.Notifications', 'IsRead') IS NULL
            ALTER TABLE [dbo].[Notifications] ADD [IsRead] bit NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT(0);

        IF COL_LENGTH('dbo.Notifications', 'ReadAt') IS NULL
            ALTER TABLE [dbo].[Notifications] ADD [ReadAt] datetime2 NULL;

        IF OBJECT_ID(N'[dbo].[ProcessedIntegrationEvents]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[ProcessedIntegrationEvents](
                [EventId] uniqueidentifier NOT NULL PRIMARY KEY,
                [EventType] nvarchar(200) NOT NULL,
                [ProcessedAt] datetime2 NOT NULL
            );
        END
        """);
}

app.Run();
