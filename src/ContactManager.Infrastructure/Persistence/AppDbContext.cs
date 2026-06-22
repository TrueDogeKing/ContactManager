using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Persistence;

/// <summary>
/// Kontekst bazy danych aplikacji (Entity Framework Core / PostgreSQL).
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Tworzy nową instancję kontekstu z podanymi opcjami.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>Konta użytkowników (operatorów) uprawnionych do logowania.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Kontakty w książce telefonicznej.</summary>
    public DbSet<Contact> Contacts => Set<Contact>();

    /// <summary>Kategorie kontaktów.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Podkategorie kontaktów.</summary>
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wczytaj wszystkie konfiguracje IEntityTypeConfiguration z tej assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
