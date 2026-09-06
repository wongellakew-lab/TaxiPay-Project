using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class DriverShiftConfiguration : IEntityTypeConfiguration<DriverShift>
{
    public void Configure(EntityTypeBuilder<DriverShift> builder)
    {
        builder.ToTable("driver_shifts");

        builder.HasKey(ds => ds.Id);

        builder.Property(ds => ds.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(ds => ds.Version)
            .IsRowVersion();

        builder.Property(ds => ds.DriverId)
            .HasColumnName("driver_id")
            .IsRequired();

        builder.Property(ds => ds.RouteId)
            .HasColumnName("route_id")
            .IsRequired();

        builder.Property(ds => ds.ShiftStart)
            .HasColumnName("shift_start")
            .IsRequired();

        builder.Property(ds => ds.ShiftEnd)
            .HasColumnName("shift_end");

        builder.Property(ds => ds.CurrentTripNumber)
            .HasColumnName("current_trip_number")
            .HasDefaultValue(0);

        builder.Property(ds => ds.CurrentTripId)
            .HasColumnName("current_trip_id");

        builder.Property(ds => ds.TotalPassengers)
            .HasColumnName("total_passengers")
            .HasDefaultValue(0);

        builder.Property(ds => ds.TotalRevenue)
            .HasColumnName("total_revenue")
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(ds => ds.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ds => ds.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ds => ds.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ds => ds.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(ds => ds.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(ds => !ds.IsDeleted);

        builder.HasOne(ds => ds.Driver)
            .WithMany(d => d.Shifts)
            .HasForeignKey(ds => ds.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ds => ds.Route)
            .WithMany(r => r.Shifts)
            .HasForeignKey(ds => ds.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ds => ds.Trips)
            .WithOne(t => t.DriverShift)
            .HasForeignKey(t => t.DriverShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ds => new { ds.DriverId, ds.Status })
            .HasDatabaseName("idx_driver_shifts_driver_status");

        builder.HasIndex(ds => ds.ShiftStart)
            .HasDatabaseName("idx_driver_shifts_start");
    }
}