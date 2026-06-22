using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactManager.Infrastructure.Persistence.Configurations;

/// <summary>Konfiguracja EF Core dla encji <see cref="Subcategory"/> wraz z danymi słownikowymi (seed).</summary>
public class SubcategoryConfiguration : IEntityTypeConfiguration<Subcategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Subcategory> builder)
    {
        builder.ToTable("Subcategories");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Nazwa podkategorii unikalna w obrębie kategorii.
        builder.HasIndex(s => new { s.CategoryId, s.Name })
            .IsUnique();

        builder.HasOne(s => s.Category)
            .WithMany(c => c.Subcategories)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed podkategorii dla kategorii „Służbowy” (Id = 1).
        builder.HasData(
            new Subcategory { Id = 1, CategoryId = 1, Name = "Szef" },
            new Subcategory { Id = 2, CategoryId = 1, Name = "Klient" },
            new Subcategory { Id = 3, CategoryId = 1, Name = "Pracownik" },
            new Subcategory { Id = 4, CategoryId = 1, Name = "Kontrahent" }
        );
    }
}
