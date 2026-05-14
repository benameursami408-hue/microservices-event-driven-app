using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Data;

public static class TestDataSeeder
{
    private sealed record SeedUser(
        long Id,
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Address,
        string Email,
        string Password,
        UserRole Role);

    private static readonly SeedUser[] DemoUsers =
    [
        new(100, "Admin", "SAV Pro", "+21670000100", "Siege SAV Pro, Tunis", "admin@savpro.local", "Password123!", UserRole.ADMIN),
        new(200, "Sofia", "Mansouri", "+21670000200", "Service SAV, Tunis", "sav@savpro.local", "Password123!", UserRole.SAV),
        new(301, "Ahmed", "Benali", "+21670000301", "Ariana, Tunisie", "tech1@savpro.local", "Password123!", UserRole.ST),
        new(302, "Youssef", "Amrani", "+21670000302", "Ben Arous, Tunisie", "tech2@savpro.local", "Password123!", UserRole.ST),
        new(303, "Sara", "El Mansouri", "+21670000303", "Tunis, Tunisie", "tech3@savpro.local", "Password123!", UserRole.ST),
        new(304, "Karim", "Haddad", "+21670000304", "Manouba, Tunisie", "tech4@savpro.local", "Password123!", UserRole.ST),
        new(501, "Societe", "Industrielle Atlas", "+21670000501", "Zone industrielle Mghira, Ben Arous", "client1@savpro.local", "Password123!", UserRole.CLIENT),
        new(502, "Hotel", "Marina", "+21670000502", "Port El Kantaoui, Sousse", "client2@savpro.local", "Password123!", UserRole.CLIENT),
        new(503, "Clinique", "Ibn Sina", "+21670000503", "Centre urbain nord, Tunis", "client3@savpro.local", "Password123!", UserRole.CLIENT),
        new(504, "Supermarche", "Central", "+21670000504", "Avenue Habib Bourguiba, Tunis", "client4@savpro.local", "Password123!", UserRole.CLIENT),
        new(505, "Usine", "Textile Nord", "+21670000505", "Zone industrielle Utique, Bizerte", "client5@savpro.local", "Password123!", UserRole.CLIENT),
        new(506, "Residence", "Les Jardins", "+21670000506", "La Marsa, Tunis", "client6@savpro.local", "Password123!", UserRole.CLIENT)
    ];

    public static async Task SeedUsersAsync(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger logger,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!ReadBool(configuration, "Seed:DemoData", true))
        {
            logger.LogInformation("Auth demo seed disabled by Seed:DemoData=false.");
            return;
        }

        var demoIds = DemoUsers.Select(user => user.Id).ToArray();
        var demoEmails = DemoUsers.Select(user => NormalizeEmail(user.Email)).ToArray();

        if (ReadBool(configuration, "Seed:ResetDemoData", false) && IsDevelopmentOrDocker(environment))
        {
            var rowsToReset = await dbContext.Users
                .Where(user => demoIds.Contains(user.Id) || demoEmails.Contains(user.Email.ToLower()))
                .ToListAsync(cancellationToken);

            if (rowsToReset.Count > 0)
            {
                dbContext.Users.RemoveRange(rowsToReset);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogWarning("Auth demo seed reset removed {Count} user(s).", rowsToReset.Count);
            }
        }

        var existingUsers = await dbContext.Users
            .Where(user => demoIds.Contains(user.Id) || demoEmails.Contains(user.Email.ToLower()))
            .ToListAsync(cancellationToken);

        var existingById = existingUsers.ToDictionary(user => user.Id);
        var existingByEmail = existingUsers
            .GroupBy(user => NormalizeEmail(user.Email), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var toInsert = new List<User>();
        var updatedCount = 0;

        foreach (var seed in DemoUsers)
        {
            var normalizedEmail = NormalizeEmail(seed.Email);
            var existing = existingById.GetValueOrDefault(seed.Id) ?? existingByEmail.GetValueOrDefault(normalizedEmail);

            if (existing is null)
            {
                toInsert.Add(CreateUser(seed, passwordHasher));
                continue;
            }

            ApplySeed(existing, seed, passwordHasher, forcePassword: IsDevelopmentOrDocker(environment));
            updatedCount++;

            if (existing.Id != seed.Id)
            {
                logger.LogWarning(
                    "Demo user {Email} already exists with Id={ExistingId}; expected demo Id={ExpectedId}. Run RESET_DEMO_DATA=true on a development database if cross-service ids must be rebuilt.",
                    normalizedEmail,
                    existing.Id,
                    seed.Id);
            }
        }

        if (toInsert.Count > 0)
        {
            await InsertWithIdentityAsync(dbContext, toInsert, cancellationToken);
        }

        if (updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Auth demo seed completed. Inserted={InsertedCount}, Updated={UpdatedCount}. Demo password for all accounts: Password123!",
            toInsert.Count,
            updatedCount);
    }

    private static async Task InsertWithIdentityAsync(AppDbContext dbContext, List<User> users, CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Users] ON", cancellationToken);
            dbContext.Users.AddRange(users);
            await dbContext.SaveChangesAsync(cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Users] OFF", cancellationToken);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static User CreateUser(SeedUser seed, IPasswordHasher passwordHasher)
    {
        return new User
        {
            Id = seed.Id,
            FirstName = seed.FirstName,
            LastName = seed.LastName,
            PhoneNumber = seed.PhoneNumber,
            Address = seed.Address,
            Email = NormalizeEmail(seed.Email),
            Password = passwordHasher.Hash(seed.Password),
            IsActive = true,
            Role = seed.Role
        };
    }

    private static void ApplySeed(User user, SeedUser seed, IPasswordHasher passwordHasher, bool forcePassword)
    {
        user.FirstName = seed.FirstName;
        user.LastName = seed.LastName;
        user.PhoneNumber = seed.PhoneNumber;
        user.Address = seed.Address;
        user.Email = NormalizeEmail(seed.Email);
        user.IsActive = true;
        user.Role = seed.Role;

        if (forcePassword && !PasswordMatches(passwordHasher, user.Password, seed.Password))
        {
            user.Password = passwordHasher.Hash(seed.Password);
        }
    }

    private static bool IsDevelopmentOrDocker(IHostEnvironment environment)
    {
        return environment.IsDevelopment()
            || environment.IsEnvironment("Docker")
            || environment.IsEnvironment("Demo");
    }

    private static bool PasswordMatches(IPasswordHasher passwordHasher, string passwordHash, string password)
    {
        try
        {
            return passwordHasher.Verify(passwordHash, password);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static bool ReadBool(IConfiguration? configuration, string key, bool defaultValue)
    {
        if (configuration is null)
        {
            return defaultValue;
        }

        var value = configuration[key];
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

}
