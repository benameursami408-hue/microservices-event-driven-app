using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
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

    private static readonly SeedUser[] Users =
    [
        new("Sami", "Benameur", "0600000001", "12 Rue des Fleurs, Paris", "sami.benameur.client@sav.local", "Client!123", UserRole.CLIENT),
        new("Leila", "Mansour", "0600000002", "8 Avenue Habib Bourguiba, Tunis", "leila.mansour.client@sav.local", "Client!123", UserRole.CLIENT),
        new("Youssef", "Trabelsi", "0600000003", "44 Rue Victor Hugo, Lyon", "youssef.trabelsi.sav@sav.local", "SavAgent!123", UserRole.SAV),
        new("Nour", "Ben Ali", "0600000004", "5 Rue de la Republique, Marseille", "nour.benali.tech@sav.local", "Tech!1234", UserRole.ST),
        new("Admin", "Platform", "0600000005", "1 Rue de l'Administration, Paris", "admin@sav.local", "Admin!1234", UserRole.ADMIN)
    ];

    public static async Task SeedUsersAsync(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var expectedEmails = Users.Select(u => u.Email).ToArray();
        var existingEmails = await dbContext.Users
            .AsNoTracking()
            .Where(u => expectedEmails.Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);

        var existingSet = existingEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usersToInsert = Users
            .Where(u => !existingSet.Contains(u.Email))
            .Select(u => new User
            {
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Email = u.Email,
                Password = passwordHasher.Hash(u.Password),
                IsActive = true,
                Role = u.Role
            })
            .ToList();

        if (usersToInsert.Count == 0)
        {
            logger.LogInformation("Auth seed skipped: all 5 business users already exist.");
            return;
        }

        dbContext.Users.AddRange(usersToInsert);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Auth seed inserted {InsertedCount} user(s).", usersToInsert.Count);
    }
}
