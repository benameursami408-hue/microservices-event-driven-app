using Microsoft.EntityFrameworkCore;
using ReclamationService.Infrastructure.Data;
using ReclamationService.Domain.Interfaces;
using ReclamationService.Infrastructure.Repositories;
using ReclamationService.Application.Services;
using MassTransit;
using ReclamationService.Application.Consumers;
using ReclamationService.Application.Outbox;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ReclamationService.Api.Infrastructure;
using ReclamationService.Infrastructure.Outbox;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("reclamation", includeNamespace: false));
    x.AddConsumer<UserCreatedConsumer>();

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

builder.Services.AddExceptionHandler<ReclamationService.Api.Infrastructure.GlobalExceptionHandler>();
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

var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (!builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Contains("ChangeThis", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Jwt:SecretKey must be provided securely for non-development environments.");
}

builder.Services.AddScoped<IReclamationRepository, ReclamationRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IReclamationHistoryRepository, ReclamationHistoryRepository>();
builder.Services.AddScoped<IOutboxWriter, EfOutboxWriter>();
builder.Services.AddScoped<ReclamationsService>();
builder.Services.AddHostedService<OutboxDispatcher>();

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
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ReclamationService.DatabaseMigration");
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
                [RetryCount] int NOT NULL CONSTRAINT [DF_OutboxMessages_RetryCount] DEFAULT(0),
                [LastError] nvarchar(2000) NULL
            );

            CREATE INDEX [IX_OutboxMessages_ProcessedAt_CreatedAt]
                ON [dbo].[OutboxMessages]([ProcessedAt], [CreatedAt]);
        END
        """);
}

app.Run();
