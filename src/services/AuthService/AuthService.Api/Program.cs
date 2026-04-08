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
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Application.Outbox;
using AuthService.Infrastructure.Outbox;

var builder = WebApplication.CreateBuilder(args);

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

        cfg.ConfigureEndpoints(context);
    });
});


builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

        var hasAdmin = await seedDbContext.Users.AnyAsync(u => u.Role == UserRole.ADMIN);
        if (!hasAdmin)
        {
            var email = app.Configuration["Seed:AdminEmail"] ?? "admin@local";
            var password = app.Configuration["Seed:AdminPassword"] ?? "Admin123!";
            var firstName = app.Configuration["Seed:AdminFirstName"] ?? "Admin";
            var lastName = app.Configuration["Seed:AdminLastName"] ?? "User";
            var phone = app.Configuration["Seed:AdminPhoneNumber"] ?? "0000000000";

            seedDbContext.Users.Add(new User
            {
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phone,
                Address = "",
                Email = email,
                Password = passwordHasher.Hash(password),
                IsActive = true,
                Role = UserRole.ADMIN
            });

            await seedDbContext.SaveChangesAsync();
            logger.LogWarning("Seeded ADMIN user Email={Email} (Seed:Enabled=true)", email);
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
                [RetryCount] int NOT NULL CONSTRAINT [DF_AuthOutboxMessages_RetryCount] DEFAULT(0),
                [LastError] nvarchar(2000) NULL
            );

            CREATE INDEX [IX_AuthOutboxMessages_ProcessedAt_CreatedAt]
                ON [dbo].[OutboxMessages]([ProcessedAt], [CreatedAt]);
        END
        """);
}

app.Run();
