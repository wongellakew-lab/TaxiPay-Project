using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.ToTable("passengers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(p => p.Version)
            .IsRowVersion();

        builder.Property(p => p.Phone)
            .HasColumnName("phone")
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.TotalTrips)
            .HasColumnName("total_trips")
            .HasDefaultValue(0);

        builder.Property(p => p.TotalSpent)
            .HasColumnName("total_spent")
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(p => p.Rating)
            .HasColumnName("rating")
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0);

        builder.Property(p => p.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);

        builder.Property(p => p.VerifiedAt)
            .HasColumnName("verified_at");

        builder.Property(p => p.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Phone).IsUnique();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PassengerJourneys)
            .WithOne(pj => pj.Passenger)
            .HasForeignKey(pj => pj.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Disputes)
            .WithOne(d => d.Passenger)
            .HasForeignKey(d => d.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}