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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

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

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("/health");

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
