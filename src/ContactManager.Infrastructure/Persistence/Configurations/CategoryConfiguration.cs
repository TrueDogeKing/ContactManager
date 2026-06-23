using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactManager.Infrastructure.Persistence.Configurations;

/// <summary>Konfiguracja EF Core dla encji <see cref="Category"/> wraz z danymi słownikowymi (seed).</summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        // Seed wymaganych kategorii.
        builder.HasData(
            new Category { Id = 1, Name = "Służbowy" },
            new Category { Id = 2, Name = "Prywatny" },
            new Category { Id = 3, Name = "Inny" }
        );
    }
}
