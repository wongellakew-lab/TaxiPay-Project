using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("admins");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(a => a.Version)
            .IsRowVersion();

        builder.Property(a => a.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Email)
            .HasColumnName("email")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Phone)
            .HasColumnName("phone")
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(a => a.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(a => a.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(a => a.Email).IsUnique();
        builder.HasIndex(a => a.Phone).IsUnique();
        builder.HasIndex(a => a.UserId).IsUnique();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.ReviewedRouteChangeRequests)
            .WithOne()
            .HasForeignKey(rc => rc.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.ResolvedDisputes)
            .WithOne()
            .HasForeignKey(d => d.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}