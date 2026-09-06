using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class PassengerJourneyConfiguration : IEntityTypeConfiguration<PassengerJourney>
{
    public void Configure(EntityTypeBuilder<PassengerJourney> builder)
    {
        builder.ToTable("passenger_journeys");

        builder.HasKey(pj => pj.Id);

        builder.Property(pj => pj.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(pj => pj.Version)
            .IsRowVersion();

        builder.Property(pj => pj.TripId)
            .HasColumnName("trip_id")
            .IsRequired();

        builder.Property(pj => pj.PassengerId)
            .HasColumnName("passenger_id")
            .IsRequired();

        builder.Property(pj => pj.BoardingNode)
            .HasColumnName("boarding_node")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(pj => pj.DestinationNode)
            .HasColumnName("destination_node")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(pj => pj.Fare)
            .HasColumnName("fare")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(pj => pj.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pj => pj.PaymentStatus)
            .HasColumnName("payment_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pj => pj.AlightingStatus)
            .HasColumnName("alighting_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(AlightingStatus.OnBoard);

        builder.Property(pj => pj.AlightedAt)
            .HasColumnName("alighted_at");

        builder.Property(pj => pj.TransactionRef)
            .HasColumnName("transaction_ref")
            .HasMaxLength(100);

        builder.Property(pj => pj.ReceiptSent)
            .HasColumnName("receipt_sent")
            .HasDefaultValue(false);

        builder.Property(pj => pj.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(pj => pj.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(pj => pj.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(pj => pj.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(pj => !pj.IsDeleted);

        builder.HasOne(pj => pj.Trip)
            .WithMany(t => t.PassengerJourneys)
            .HasForeignKey(pj => pj.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pj => pj.Passenger)
            .WithMany(p => p.PassengerJourneys)
            .HasForeignKey(pj => pj.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pj => new { pj.TripId, pj.PassengerId })
            .IsUnique()
            .HasDatabaseName("idx_passenger_journeys_trip_passenger");

        builder.HasIndex(pj => pj.PassengerId)
            .HasDatabaseName("idx_passenger_journeys_passenger");
    }
}