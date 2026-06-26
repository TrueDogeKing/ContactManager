using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactManager.Infrastructure.Persistence.Configurations;

/// EF Core configuration for the Category entity, including dictionary seed data.
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.AllowsCustomSubcategory).IsRequired();

        // Seed the required categories. Only "Inny" allows a free-text subcategory.
        builder.HasData(
            new Category
            {
                Id = 1,
                Name = "Służbowy",
                AllowsCustomSubcategory = false,
            },
            new Category
            {
                Id = 2,
                Name = "Prywatny",
                AllowsCustomSubcategory = false,
            },
            new Category
            {
                Id = 3,
                Name = "Inny",
                AllowsCustomSubcategory = true,
            }
        );
    }
}
