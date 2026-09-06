using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class PassengerRouteRequestConfiguration : IEntityTypeConfiguration<PassengerRouteRequest>
{
    public void Configure(EntityTypeBuilder<PassengerRouteRequest> builder)
    {
        builder.ToTable("passenger_route_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(r => r.Version)
            .IsRowVersion();

        builder.Property(r => r.PassengerId)
            .HasColumnName("passenger_id")
            .IsRequired();

        builder.Property(r => r.FromLocation)
            .HasColumnName("from_location")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.ToLocation)
            .HasColumnName("to_location")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(r => r.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(r => r.AdminNotes)
            .HasColumnName("admin_notes")
            .HasMaxLength(500);

        builder.Property(r => r.ReviewedByAdminId)
            .HasColumnName("reviewed_by_admin_id");

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

        builder.HasOne(r => r.Passenger)
            .WithMany(p => p.RouteRequests)
            .HasForeignKey(r => r.PassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.PassengerId)
            .HasDatabaseName("idx_prr_passenger");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("idx_prr_status");
    }
}
