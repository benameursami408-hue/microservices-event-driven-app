using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("reclamation", includeNamespace: false));
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<TechnicianAssignedConsumer>();
    x.AddConsumer<AppointmentConfirmedConsumer>();
    x.AddConsumer<AppointmentRescheduledConsumer>();
    x.AddConsumer<AppointmentCancelledConsumer>();
    x.AddConsumer<InterventionStartedConsumer>();
    x.AddConsumer<InterventionCompletedConsumer>();
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

var uploadRateLimitPermit = builder.Configuration.GetValue<int?>("Security:UploadRateLimit:PermitLimit") ?? 20;
var uploadRateLimitWindowSeconds = builder.Configuration.GetValue<int?>("Security:UploadRateLimit:WindowSeconds") ?? 60;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Uploads", httpContext =>
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = uploadRateLimitPermit,
                Window = TimeSpan.FromSeconds(uploadRateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});


var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (!builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Contains("ChangeThis", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Jwt:SecretKey must be provided securely for non-development environments.");
}

builder.Services.AddScoped<IReclamationRepository, ReclamationRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IServiceUserRepository, ServiceUserRepository>();
builder.Services.AddScoped<IAiPriorityAnalysisRepository, AiPriorityAnalysisRepository>();
builder.Services.AddScoped<IReclamationHistoryRepository, ReclamationHistoryRepository>();
builder.Services.AddScoped<IOutboxWriter, EfOutboxWriter>();
builder.Services.AddScoped<TicketClassificationService>();
builder.Services.AddScoped<ReclamationPriorityService>();
builder.Services.AddScoped<ReclamationSlaService>();
builder.Services.AddScoped<ReclamationsService>();
builder.Services.AddScoped<ClientsService>();
builder.Services.AddScoped<AiPriorityService>();
builder.Services.AddScoped<AdminReclamationStatsService>();
builder.Services.AddScoped<InterventionProjectionService>();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddHostedService<SlaMonitorWorker>();

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
app.UseStaticFiles();

app.UseRateLimiter();
app.UseCors("AllowSavProFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/health/live", () => Results.Ok(new { status = "Live", service = "ReclamationService" })).AllowAnonymous();
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
        service = "ReclamationService",
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

    using var bootstrapScope = app.Services.CreateScope();
    var bootstrapDbContext = bootstrapScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await bootstrapDbContext.Database.ExecuteSqlRawAsync(
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

        IF COL_LENGTH('dbo.Reclamations', 'ProductName') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ProductName] nvarchar(150) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'Barcode') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [Barcode] nvarchar(64) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'ProductImageUrl') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ProductImageUrl] nvarchar(500) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'PurchaseDate') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PurchaseDate] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'Brand') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [Brand] nvarchar(100) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'Model') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [Model] nvarchar(100) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'SerialNumber') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [SerialNumber] nvarchar(100) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'ProductReference') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ProductReference] nvarchar(100) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'SellerName') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [SellerName] nvarchar(150) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'PurchaseProofUrl') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PurchaseProofUrl] nvarchar(500) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'RequiresReplanning') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [RequiresReplanning] bit NOT NULL CONSTRAINT [DF_Reclamations_RequiresReplanning] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'LastInterventionOutcome') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [LastInterventionOutcome] nvarchar(40) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'LastInterventionReportSummary') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [LastInterventionReportSummary] nvarchar(2000) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'Severity') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [Severity] int NOT NULL CONSTRAINT [DF_Reclamations_Severity] DEFAULT(1);

        IF COL_LENGTH('dbo.Reclamations', 'Category') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [Category] int NOT NULL CONSTRAINT [DF_Reclamations_Category] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'CategoryReason') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [CategoryReason] nvarchar(250) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'CategoryUpdatedAt') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [CategoryUpdatedAt] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'PriorityScore') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PriorityScore] int NOT NULL CONSTRAINT [DF_Reclamations_PriorityScore] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'PriorityReasons') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PriorityReasons] nvarchar(2000) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'PrioritySource') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PrioritySource] int NOT NULL CONSTRAINT [DF_Reclamations_PrioritySource] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'PriorityUpdatedAt') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PriorityUpdatedAt] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'ManualPriorityOverride') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ManualPriorityOverride] bit NOT NULL CONSTRAINT [DF_Reclamations_ManualPriorityOverride] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'ManualPriorityOverrideReason') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ManualPriorityOverrideReason] nvarchar(500) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'IsBlocking') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [IsBlocking] bit NOT NULL CONSTRAINT [DF_Reclamations_IsBlocking] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'FollowUpCount') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [FollowUpCount] int NOT NULL CONSTRAINT [DF_Reclamations_FollowUpCount] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'FirstResponseDeadline') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [FirstResponseDeadline] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'PlanningDeadline') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PlanningDeadline] datetime2 NULL;


        IF OBJECT_ID(N'[dbo].[AiPriorityAnalyses]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[AiPriorityAnalyses](
                [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [ReclamationId] bigint NOT NULL,
                [SuggestedPriority] nvarchar(50) NOT NULL,
                [ConfidenceScore] int NOT NULL,
                [SlaRisk] nvarchar(50) NOT NULL,
                [Reason] nvarchar(1000) NOT NULL,
                [RecommendedAction] nvarchar(1000) NOT NULL,
                [DetectedKeywordsJson] nvarchar(max) NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [AcceptedAt] datetime2 NULL,
                [AcceptedByUserId] bigint NULL
            );
            CREATE INDEX [IX_AiPriorityAnalyses_ReclamationId_CreatedAt]
                ON [dbo].[AiPriorityAnalyses]([ReclamationId], [CreatedAt]);
        END

        IF COL_LENGTH('dbo.Reclamations', 'ResolutionDeadline') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ResolutionDeadline] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'SlaStatus') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [SlaStatus] int NOT NULL CONSTRAINT [DF_Reclamations_SlaStatus] DEFAULT(0);

        IF COL_LENGTH('dbo.Reclamations', 'SlaBreachedAt') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [SlaBreachedAt] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'ClaimedBySavId') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ClaimedBySavId] bigint NULL;

        IF COL_LENGTH('dbo.Reclamations', 'ClaimedBySavName') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ClaimedBySavName] nvarchar(100) NULL;

        IF COL_LENGTH('dbo.Reclamations', 'ClaimedAt') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ClaimedAt] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'ReleasedAt') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [ReleasedAt] datetime2 NULL;

        IF COL_LENGTH('dbo.Reclamations', 'PlanningRequestedAt') IS NULL
            ALTER TABLE [dbo].[Reclamations] ADD [PlanningRequestedAt] datetime2 NULL;

        IF OBJECT_ID(N'[dbo].[ServiceUsers]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[ServiceUsers](
                [Id] bigint NOT NULL PRIMARY KEY,
                [FullName] nvarchar(100) NOT NULL,
                [Email] nvarchar(100) NOT NULL,
                [Role] nvarchar(30) NOT NULL,
                [UpdatedAt] datetime2 NOT NULL
            );
            CREATE INDEX [IX_ServiceUsers_Role] ON [dbo].[ServiceUsers]([Role]);
        END
        """);

    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        using var seedScope = app.Services.CreateScope();
        var seedDbContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.SeedAsync(seedDbContext, logger, app.Configuration);
    }
}

app.Run();
