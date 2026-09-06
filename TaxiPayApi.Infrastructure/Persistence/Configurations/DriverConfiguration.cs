using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(d => d.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(d => d.Version)
            .IsRowVersion();

        builder.Property(d => d.DriverId)
            .HasColumnName("driver_id")
            .HasMaxLength(6);

        builder.Property(d => d.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Phone)
            .HasColumnName("phone")
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(d => d.LicenseNumber)
            .HasColumnName("license_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.PhotoUrl)
            .HasColumnName("photo_url")
            .HasMaxLength(255);

        builder.Property(d => d.Rating)
            .HasColumnName("rating")
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0);

        builder.Property(d => d.TotalTrips)
            .HasColumnName("total_trips")
            .HasDefaultValue(0);

        builder.Property(d => d.TotalRevenue)
            .HasColumnName("total_revenue")
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(d => d.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);

        builder.Property(d => d.VerifiedAt)
            .HasColumnName("verified_at");

        builder.Property(d => d.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.IsDeleted)
               .HasColumnName("is_deleted")
               .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(d => d.DriverId).IsUnique().HasFilter("\"driver_id\" IS NOT NULL");
        builder.HasIndex(d => d.Phone).IsUnique();
        builder.HasIndex(d => d.LicenseNumber).IsUnique();
        builder.HasIndex(d => d.UserId).IsUnique();

        builder.Property(d => d.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        builder.Property(d => d.PreferredRouteId)
            .HasColumnName("preferred_route_id");

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.CurrentRoute)
            .WithMany()
            .HasForeignKey(d => d.CurrentRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.PreferredRoute)
            .WithMany()
            .HasForeignKey(d => d.PreferredRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Shifts)
            .WithOne(s => s.Driver)
            .HasForeignKey(s => s.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Trips)
            .WithOne(t => t.Driver)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.RouteChangeRequests)
            .WithOne(r => r.Driver)
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}