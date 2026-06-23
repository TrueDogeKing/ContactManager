using ContactManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactManager.Infrastructure.Persistence.Configurations;

/// <summary>Konfiguracja EF Core dla encji <see cref="Contact"/>.</summary>
public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Unikalny indeks na adres e-mail kontaktu.
        builder.HasIndex(c => c.Email)
            .IsUnique();

        builder.Property(c => c.PasswordHash)
            .IsRequired();

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(c => c.CustomSubcategory)
            .HasMaxLength(100);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Relacja: Kontakt -> Kategoria (wymagana). Brak kaskadowego usuwania słownika.
        builder.HasOne(c => c.Category)
            .WithMany(cat => cat.Contacts)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacja: Kontakt -> Podkategoria (opcjonalna).
        builder.HasOne(c => c.Subcategory)
            .WithMany(s => s.Contacts)
            .HasForeignKey(c => c.SubcategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optimistic concurrency oparte o systemową kolumnę PostgreSQL "xmin".
        builder.Property(c => c.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
