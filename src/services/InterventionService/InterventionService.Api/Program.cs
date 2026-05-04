using System.Text;
using InterventionService.Api.Infrastructure;
using InterventionService.Application.Consumers;
using InterventionService.Application.Interfaces;
using InterventionService.Application.Outbox;
using InterventionService.Application.Services;
using InterventionService.Domain.Interfaces;
using InterventionService.Infrastructure.Data;
using InterventionService.Infrastructure.Outbox;
using InterventionService.Infrastructure.Repositories;
using InterventionService.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DBConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "InterventionService requires ConnectionStrings:DBConnection. " +
        "Set it in appsettings, user-secrets, environment variables, or docker-compose.");
}

var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "InterventionService requires Jwt:SecretKey. " +
        "Set it in appsettings, user-secrets, environment variables, or docker-compose.");
}

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("intervention", includeNamespace: false));
    x.AddConsumer<PlanningRequestedConsumer>();

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

builder.Services.AddControllers();
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

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPlanningRequestRepository, PlanningRequestRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IInterventionRepository, InterventionRepository>();
builder.Services.AddScoped<IOutboxWriter, EfOutboxWriter>();
builder.Services.AddScoped<IEventIdempotencyStore, EventIdempotencyStore>();
builder.Services.AddScoped<IdempotentConsumerRunner>();
builder.Services.AddScoped<PlanningCapacityService>();
builder.Services.AddScoped<PlanningService>();
builder.Services.AddScoped<RealisationService>();
builder.Services.AddHostedService<OutboxDispatcher>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

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
app.MapGet("/health/live", () => Results.Ok(new { status = "Live", service = "InterventionService" })).AllowAnonymous();
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
        service = "InterventionService",
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
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("InterventionService.DatabaseMigration");
    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            logger.LogInformation("Applying InterventionService schema (attempt {Attempt}/{MaxAttempts})...", attempt, maxAttempts);
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("InterventionService EF migrations applied successfully.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delaySeconds = Math.Min(30, attempt * 2);
            logger.LogWarning(ex, "Schema attempt {Attempt} failed; retrying in {DelaySeconds}s", attempt, delaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }

    using var schemaScope = app.Services.CreateScope();
    var schemaDbContext = schemaScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await schemaDbContext.Database.ExecuteSqlRawAsync(SqlBootstrap.Script);
}

app.Run();

internal static class SqlBootstrap
{
    public const string Script =
        """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'planning') EXEC('CREATE SCHEMA planning');
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'realisation') EXEC('CREATE SCHEMA realisation');
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'integration') EXEC('CREATE SCHEMA integration');

        IF OBJECT_ID(N'[planning].[PlanningRequests]', N'U') IS NULL
        BEGIN
            CREATE TABLE [planning].[PlanningRequests](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [ReclamationId] bigint NOT NULL,
                [Reference] nvarchar(50) NOT NULL,
                [SavId] bigint NOT NULL,
                [SavName] nvarchar(100) NOT NULL,
                [Priority] nvarchar(40) NOT NULL,
                [ClientId] bigint NOT NULL,
                [CustomerName] nvarchar(100) NOT NULL,
                [CustomerEmail] nvarchar(100) NULL,
                [CustomerPhone] nvarchar(50) NULL,
                [ServiceAddress] nvarchar(300) NULL,
                [ProductName] nvarchar(150) NULL,
                [Brand] nvarchar(100) NULL,
                [Model] nvarchar(100) NULL,
                [SerialNumber] nvarchar(100) NULL,
                [RequestedAt] datetime2 NOT NULL,
                [Status] int NOT NULL
            );
            CREATE INDEX [IX_PlanningRequests_ReclamationId] ON [planning].[PlanningRequests]([ReclamationId]);
        END

        IF OBJECT_ID(N'[planning].[Appointments]', N'U') IS NULL
        BEGIN
            CREATE TABLE [planning].[Appointments](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [PlanningRequestId] uniqueidentifier NOT NULL,
                [ReclamationId] bigint NOT NULL,
                [Reference] nvarchar(50) NOT NULL,
                [StartAt] datetime2 NOT NULL,
                [EndAt] datetime2 NULL,
                [EstimatedDurationMinutes] int NOT NULL CONSTRAINT [DF_Appointments_EstimatedDurationMinutes] DEFAULT(90),
                [TimeZone] nvarchar(100) NOT NULL,
                [TechnicianId] bigint NULL,
                [TechnicianName] nvarchar(100) NULL,
                [CustomerPresenceRequired] bit NOT NULL,
                [Status] int NOT NULL,
                [Sequence] int NOT NULL,
                [CancelReasonCode] nvarchar(50) NULL,
                [CancelReasonText] nvarchar(500) NULL,
                [PlanningNote] nvarchar(500) NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NOT NULL
            );
            CREATE INDEX [IX_Appointments_ReclamationId_Status_Sequence] ON [planning].[Appointments]([ReclamationId], [Status], [Sequence]);
        END

        IF COL_LENGTH('planning.Appointments', 'EstimatedDurationMinutes') IS NULL
            ALTER TABLE [planning].[Appointments] ADD [EstimatedDurationMinutes] int NOT NULL CONSTRAINT [DF_Appointments_EstimatedDurationMinutes_Alter] DEFAULT(90);

        IF OBJECT_ID(N'[planning].[Assignments]', N'U') IS NULL
        BEGIN
            CREATE TABLE [planning].[Assignments](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [AppointmentId] uniqueidentifier NOT NULL,
                [TechnicianId] bigint NOT NULL,
                [TechnicianName] nvarchar(100) NOT NULL,
                [AssignedByUserId] bigint NOT NULL,
                [AssignedByRole] nvarchar(30) NOT NULL,
                [AssignedAt] datetime2 NOT NULL,
                [Status] int NOT NULL
            );
        END

        IF OBJECT_ID(N'[planning].[RescheduleRequests]', N'U') IS NULL
        BEGIN
            CREATE TABLE [planning].[RescheduleRequests](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [AppointmentId] uniqueidentifier NOT NULL,
                [ReasonCode] nvarchar(50) NOT NULL,
                [ReasonText] nvarchar(500) NULL,
                [RequestedByUserId] bigint NOT NULL,
                [RequestedByRole] nvarchar(30) NOT NULL,
                [RequestedAt] datetime2 NOT NULL,
                [Status] int NOT NULL
            );
        END

        IF OBJECT_ID(N'[realisation].[Interventions]', N'U') IS NULL
        BEGIN
            CREATE TABLE [realisation].[Interventions](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [AppointmentId] uniqueidentifier NOT NULL,
                [ReclamationId] bigint NOT NULL,
                [Reference] nvarchar(50) NOT NULL,
                [TechnicianId] bigint NOT NULL,
                [TechnicianName] nvarchar(100) NOT NULL,
                [StartedAt] datetime2 NULL,
                [EndedAt] datetime2 NULL,
                [Status] int NOT NULL,
                [Outcome] int NULL,
                [NeedsReplanning] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NOT NULL
            );
            CREATE INDEX [IX_Interventions_AppointmentId_ReclamationId] ON [realisation].[Interventions]([AppointmentId], [ReclamationId]);
        END

        IF OBJECT_ID(N'[realisation].[Diagnostics]', N'U') IS NULL
        BEGIN
            CREATE TABLE [realisation].[Diagnostics](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [InterventionId] uniqueidentifier NOT NULL,
                [Category] nvarchar(80) NOT NULL,
                [Summary] nvarchar(500) NOT NULL,
                [RootCause] nvarchar(1000) NULL,
                [RequiresParts] bit NOT NULL,
                [RequiresFollowUp] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL
            );
        END

        IF OBJECT_ID(N'[realisation].[RepairActions]', N'U') IS NULL
        BEGIN
            CREATE TABLE [realisation].[RepairActions](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [InterventionId] uniqueidentifier NOT NULL,
                [ActionType] nvarchar(80) NOT NULL,
                [Description] nvarchar(500) NOT NULL,
                [StartedAt] datetime2 NULL,
                [CompletedAt] datetime2 NULL,
                [Success] bit NOT NULL
            );
        END

        IF OBJECT_ID(N'[realisation].[PartsUsed]', N'U') IS NULL
        BEGIN
            CREATE TABLE [realisation].[PartsUsed](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [InterventionId] uniqueidentifier NOT NULL,
                [PartCode] nvarchar(80) NOT NULL,
                [Label] nvarchar(150) NOT NULL,
                [Quantity] int NOT NULL,
                [AvailabilityStatus] nvarchar(40) NOT NULL
            );
        END

        IF OBJECT_ID(N'[realisation].[InterventionEvidences]', N'U') IS NULL
        BEGIN
            CREATE TABLE [realisation].[InterventionEvidences](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [InterventionId] uniqueidentifier NOT NULL,
                [Kind] nvarchar(40) NOT NULL,
                [Url] nvarchar(500) NOT NULL,
                [CapturedAt] datetime2 NOT NULL,
                [CapturedByUserId] bigint NOT NULL,
                [CapturedByRole] nvarchar(30) NOT NULL
            );
        END

        IF OBJECT_ID(N'[realisation].[VisitReports]', N'U') IS NULL
        BEGIN
            CREATE TABLE [realisation].[VisitReports](
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [InterventionId] uniqueidentifier NOT NULL,
                [Summary] nvarchar(2000) NOT NULL,
                [Outcome] int NOT NULL,
                [CustomerPresent] bit NOT NULL,
                [NextStep] nvarchar(500) NULL,
                [Status] int NOT NULL,
                [PublishedAt] datetime2 NULL,
                [CreatedAt] datetime2 NOT NULL
            );
        END

        IF OBJECT_ID(N'[integration].[OutboxMessages]', N'U') IS NULL
        BEGIN
            CREATE TABLE [integration].[OutboxMessages](
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
                [RetryCount] int NOT NULL CONSTRAINT [DF_InterventionOutboxMessages_RetryCount] DEFAULT(0),
                [LastError] nvarchar(2000) NULL
            );
            CREATE INDEX [IX_InterventionOutboxMessages_ProcessedAt_CreatedAt]
                ON [integration].[OutboxMessages]([ProcessedAt], [CreatedAt]);
        END

        IF OBJECT_ID(N'[integration].[ProcessedIntegrationEvents]', N'U') IS NULL
        BEGIN
            CREATE TABLE [integration].[ProcessedIntegrationEvents](
                [EventId] uniqueidentifier NOT NULL PRIMARY KEY,
                [EventType] nvarchar(200) NOT NULL,
                [ProcessedAt] datetime2 NOT NULL
            );
        END
        """;
}
