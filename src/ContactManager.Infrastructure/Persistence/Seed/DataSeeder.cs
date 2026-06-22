using ContactManager.Application.Interfaces;
using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContactManager.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeduje dane początkowe niewchodzące w skład migracji (np. domyślne konto administratora).
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Tworzy domyślne konto administratora, jeśli jeszcze nie istnieje.
    /// Dane logowania pochodzą z sekcji konfiguracji "Admin" (z wartościami domyślnymi).
    /// </summary>
    /// <param name="services">Dostawca usług aplikacji.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
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

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
