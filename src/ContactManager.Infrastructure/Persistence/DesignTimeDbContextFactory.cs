using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContactManager.Infrastructure.Persistence;

/// <summary>
/// Fabryka kontekstu używana przez narzędzia EF Core (np. <c>dotnet ef migrations</c>)
/// w czasie projektowania, niezależnie od hosta aplikacji.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        // Połączenie pobierane ze zmiennej środowiskowej lub domyślne dla developmentu.
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=phonebook;Username=phonebook;Password=phonebook";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
