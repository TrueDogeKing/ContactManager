using ContactManager.Application.Interfaces;
using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContactManager.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    /// Creates the default administrator account if it doesn't already exist.
    /// Login details come from the "Admin" configuration section (with default values).
    /// <param name="services">Application service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task SeedAdminUserAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var email = configuration["Admin:Email"] ?? "admin@contactmanager.local";
        var password = configuration["Admin:Password"] ?? "Admin123!";
        var firstName = configuration["Admin:FirstName"] ?? "Admin";
        var lastName = configuration["Admin:LastName"] ?? "Administrator";

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHasher.Hash(password),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    /// Adds a few sample contacts on first run. Idempotent: skipped when any contact already exists.
    /// Respects the category rules: Służbowy uses a dictionary subcategory, Prywatny has none,
    /// Inny uses a free-text subcategory.
    public static async Task SeedContactsAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Contacts.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var passwordHash = passwordHasher.Hash("Password123!");

        var contacts = new[]
        {
            new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Anna",
                LastName = "Kowalska",
                Email = "anna.kowalska@example.com",
                PasswordHash = passwordHash,
                Phone = "+48 600 100 200",
                BirthDate = new DateOnly(1990, 5, 14),
                CategoryId = 1,    // Służbowy
                SubcategoryId = 2, // Klient
                CreatedAt = now
            },
            new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Piotr",
                LastName = "Nowak",
                Email = "piotr.nowak@example.com",
                PasswordHash = passwordHash,
                Phone = "+48 600 300 400",
                BirthDate = new DateOnly(1985, 11, 2),
                CategoryId = 2,    // Prywatny (no subcategory)
                CreatedAt = now
            },
            new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Maria",
                LastName = "Wiśniewska",
                Email = "maria.wisniewska@example.com",
                PasswordHash = passwordHash,
                Phone = "+48 600 500 600",
                BirthDate = new DateOnly(1995, 3, 27),
                CategoryId = 3,    // Inny (free-text subcategory)
                CustomSubcategory = "Sąsiadka",
                CreatedAt = now
            }
        };

        db.Contacts.AddRange(contacts);

        // Also create User accounts for the seeded contacts so they can log in.
        foreach (var contact in contacts)
        {
            if (!await db.Users.AnyAsync(u => u.Email == contact.Email, cancellationToken))
            {
                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = contact.Email,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    PasswordHash = contact.PasswordHash,
                    CreatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
