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

var builder = WebApplication.CreateBuilder(args);

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notification", includeNamespace: false));
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<ReclamationCreatedConsumer>();

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

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationSender, LoggingNotificationSender>();
builder.Services.AddScoped<NotificationWorkflow>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
}

app.Run();
