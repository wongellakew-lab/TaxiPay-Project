using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(t => t.Version)
            .IsRowVersion();

        builder.Property(t => t.DriverShiftId)
            .HasColumnName("driver_shift_id")
            .IsRequired();

        builder.Property(t => t.RouteId)
            .HasColumnName("route_id")
            .IsRequired();

        builder.Property(t => t.DriverId)
            .HasColumnName("driver_id")
            .IsRequired();

        builder.Property(t => t.Direction)
            .HasColumnName("direction")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.TripNumber)
            .HasColumnName("trip_number")
            .IsRequired();

        builder.Property(t => t.TripStart)
            .HasColumnName("trip_start")
            .IsRequired();

        builder.Property(t => t.TripEnd)
            .HasColumnName("trip_end");

        builder.Property(t => t.HasReachedFinalDestination)
            .HasColumnName("has_reached_final_destination")
            .HasDefaultValue(false);

        builder.Property(t => t.PassengersCount)
            .HasColumnName("passengers_count")
            .HasDefaultValue(0);

        builder.Property(t => t.TripRevenue)
            .HasColumnName("trip_revenue")
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasOne(t => t.DriverShift)
            .WithMany(ds => ds.Trips)
            .HasForeignKey(t => t.DriverShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(t => t.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Driver)
            .WithMany(d => d.Trips)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.PassengerJourneys)
            .WithOne(pj => pj.Trip)
            .HasForeignKey(pj => pj.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Receipt)
            .WithOne(r => r.Trip)
            .HasForeignKey<Receipt>(r => r.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Dispute)
            .WithOne(d => d.Trip)
            .HasForeignKey<Dispute>(d => d.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.DriverShiftId, t.Status })
            .HasDatabaseName("idx_trips_shift_status");

        builder.HasIndex(t => new { t.DriverId, t.Status })
            .HasDatabaseName("idx_trips_driver_status");

        builder.HasIndex(t => t.TripStart)
            .HasDatabaseName("idx_trips_start");
    }
}