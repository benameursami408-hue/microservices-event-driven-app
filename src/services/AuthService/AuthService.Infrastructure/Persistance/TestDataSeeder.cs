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
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Address,
        string Email,
        string Password,
        UserRole Role);

    private static readonly SeedUser[] BusinessUsers =
    [
        new("Sami", "Benameur", "0600000001", "12 Rue des Fleurs, Paris", "sami.benameur.client@sav.local", "Client!123", UserRole.CLIENT),
        new("Leila", "Mansour", "0600000002", "8 Avenue Habib Bourguiba, Tunis", "leila.mansour.client@sav.local", "Client!123", UserRole.CLIENT),
        new("Youssef", "Trabelsi", "0600000003", "44 Rue Victor Hugo, Lyon", "youssef.trabelsi.sav@sav.local", "SavAgent!123", UserRole.SAV),
        new("Nour", "Ben Ali", "0600000004", "5 Rue de la Republique, Marseille", "nour.benali.tech@sav.local", "Tech!1234", UserRole.ST)
    ];

    public static async Task SeedUsersAsync(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger logger,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var adminUser = BuildAdminSeed(configuration);
        var seedUsers = BusinessUsers.Append(adminUser).ToArray();
        var expectedEmails = seedUsers
            .Select(user => NormalizeEmail(user.Email))
            .ToArray();

        var existingUsers = await dbContext.Users
            .Where(user => expectedEmails.Contains(user.Email.ToLower()))
            .ToListAsync(cancellationToken);

        var existingUsersByEmail = existingUsers.ToDictionary(
            user => NormalizeEmail(user.Email),
            StringComparer.OrdinalIgnoreCase);

        var usersToInsert = BusinessUsers
            .Where(user => !existingUsersByEmail.ContainsKey(NormalizeEmail(user.Email)))
            .Select(user => CreateUser(user, passwordHasher))
            .ToList();

        dbContext.Users.AddRange(usersToInsert);

        var normalizedAdminEmail = NormalizeEmail(adminUser.Email);
        var adminWasCreated = false;
        if (!existingUsersByEmail.TryGetValue(normalizedAdminEmail, out var existingAdmin))
        {
            existingAdmin = CreateUser(adminUser, passwordHasher);
            dbContext.Users.Add(existingAdmin);
            adminWasCreated = true;
        }

        var adminAlreadyExisted = !adminWasCreated;
        var adminPasswordResetApplied = false;
        var adminWasUpdated = false;

        if (adminAlreadyExisted && IsDevelopmentOrDocker(environment))
        {
            if (!PasswordMatches(passwordHasher, existingAdmin!.Password, adminUser.Password))
            {
                existingAdmin.Password = passwordHasher.Hash(adminUser.Password);
                adminPasswordResetApplied = true;
                adminWasUpdated = true;
            }

            if (!string.Equals(existingAdmin.Email, normalizedAdminEmail, StringComparison.Ordinal))
            {
                existingAdmin.Email = normalizedAdminEmail;
                adminWasUpdated = true;
            }

            if (!existingAdmin.IsActive)
            {
                existingAdmin.IsActive = true;
                adminWasUpdated = true;
            }

            if (existingAdmin.Role != UserRole.ADMIN)
            {
                existingAdmin.Role = UserRole.ADMIN;
                adminWasUpdated = true;
            }
        }

        var insertedCount = usersToInsert.Count + (adminWasCreated ? 1 : 0);
        if (insertedCount > 0 || adminWasUpdated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Auth seed inserted {InsertedCount} user(s).", insertedCount);
        logger.LogInformation("Auth seed admin created: {AdminCreated}. Email={AdminEmail}", adminWasCreated, normalizedAdminEmail);
        logger.LogInformation("Auth seed admin already existed: {AdminAlreadyExisted}. Email={AdminEmail}", adminAlreadyExisted, normalizedAdminEmail);
        logger.LogInformation(
            "Auth seed admin development password reset applied: {PasswordResetApplied}. Email={AdminEmail}",
            adminPasswordResetApplied,
            normalizedAdminEmail);
    }

    private static SeedUser BuildAdminSeed(IConfiguration configuration)
    {
        var adminEmail = NormalizeEmail(configuration["Seed:AdminEmail"] ?? "admin@local");
        var adminPassword = configuration["Seed:AdminPassword"] ?? "ChangeMe_Admin_2026!";
        var adminFirstName = configuration["Seed:AdminFirstName"] ?? "Admin";
        var adminLastName = configuration["Seed:AdminLastName"] ?? "User";
        var adminPhoneNumber = configuration["Seed:AdminPhoneNumber"] ?? "0000000000";

        return new SeedUser(
            adminFirstName,
            adminLastName,
            adminPhoneNumber,
            "Local development administrator",
            adminEmail,
            adminPassword,
            UserRole.ADMIN);
    }

    private static User CreateUser(SeedUser user, IPasswordHasher passwordHasher)
    {
        return new User
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Email = NormalizeEmail(user.Email),
            Password = passwordHasher.Hash(user.Password),
            IsActive = true,
            Role = user.Role
        };
    }

    private static bool IsDevelopmentOrDocker(IHostEnvironment environment)
    {
        return environment.IsDevelopment()
            || environment.IsEnvironment("Docker");
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
}
