using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(r => r.Version)
            .IsRowVersion();

        builder.Property(r => r.TripId)
            .HasColumnName("trip_id")
            .IsRequired();

        builder.Property(r => r.JourneyId)
            .HasColumnName("journey_id");

        builder.Property(r => r.ReceiptNumber)
            .HasColumnName("receipt_number")
            .HasMaxLength(50);

        builder.Property(r => r.PassengerPhone)
            .HasColumnName("passenger_phone")
            .HasMaxLength(20);

        builder.Property(r => r.PassengerName)
            .HasColumnName("passenger_name")
            .HasMaxLength(100);

        builder.Property(r => r.DriverName)
            .HasColumnName("driver_name")
            .HasMaxLength(100);

        builder.Property(r => r.DriverCode)
            .HasColumnName("driver_code")
            .HasMaxLength(20);

        builder.Property(r => r.RouteName)
            .HasColumnName("route_name")
            .HasMaxLength(100);

        builder.Property(r => r.BoardingNode)
            .HasColumnName("boarding_node")
            .HasMaxLength(50);

        builder.Property(r => r.DestinationNode)
            .HasColumnName("destination_node")
            .HasMaxLength(50);

        builder.Property(r => r.Fare)
            .HasColumnName("fare")
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(r => r.PaymentMethod)
            .HasColumnName("payment_method")
            .HasMaxLength(30);

        builder.Property(r => r.PaymentStatus)
            .HasColumnName("payment_status")
            .HasMaxLength(30);

        builder.Property(r => r.TransactionRef)
            .HasColumnName("transaction_ref")
            .HasMaxLength(100);

        builder.Property(r => r.PassengerId)
            .HasColumnName("passenger_id")
            .IsRequired();

        builder.Property(r => r.ReceiptText)
            .HasColumnName("receipt_text")
            .HasMaxLength(1000);

        builder.Property(r => r.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        builder.Property(r => r.DeliveryStatus)
            .HasColumnName("delivery_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(r => r.Trip)
            .WithMany()
            .HasForeignKey(r => r.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Passenger)
           .WithMany()
           .HasForeignKey(r => r.PassengerId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Journey)
            .WithMany()
            .HasForeignKey(r => r.JourneyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.TripId)
            .HasDatabaseName("idx_receipts_trip");

        builder.HasIndex(r => r.PassengerId)
            .HasDatabaseName("idx_receipts_passenger");

        builder.HasIndex(r => r.ReceiptNumber)
            .HasDatabaseName("idx_receipts_number");
    }
}