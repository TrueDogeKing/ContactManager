using ContactManager.Infrastructure.Persistence;
using ContactManager.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace ContactManager.IntegrationTests;

/// Boots the real API against a throwaway PostgreSQL container (Testcontainers). Using a real
/// PostgreSQL is required because optimistic concurrency is mapped onto the "xmin" system column,
/// which an in-memory provider cannot reproduce. The schema is created from the real EF migrations
/// and the database is seeded with the same DataSeeder the application uses on startup.
public class ContactManagerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace the application's DbContext (which points at the configured connection
            // string) with one pointing at the test container.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_database.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Seed the same baseline the app seeds on startup: admin user + sample contacts (with logins).
        await DataSeeder.SeedAdminUserAsync(Services);
        await DataSeeder.SeedContactsAsync(Services);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
