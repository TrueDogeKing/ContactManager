using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Subcategory> Subcategories => Set<Subcategory>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wczytaj wszystkie konfiguracje IEntityTypeConfiguration z tej assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
