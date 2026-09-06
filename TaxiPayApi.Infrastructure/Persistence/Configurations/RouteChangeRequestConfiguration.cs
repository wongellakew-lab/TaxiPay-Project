using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class RouteChangeRequestConfiguration : IEntityTypeConfiguration<RouteChangeRequest>
{
    public void Configure(EntityTypeBuilder<RouteChangeRequest> builder)
    {
        builder.ToTable("route_change_requests");

        builder.HasKey(rc => rc.Id);

        builder.Property(rc => rc.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(rc => rc.Version)
            .IsRowVersion();

        builder.Property(rc => rc.DriverId)
            .HasColumnName("driver_id")
            .IsRequired();

        builder.Property(rc => rc.CurrentRouteId)
            .HasColumnName("current_route_id")
            .IsRequired();

        builder.Property(rc => rc.NewRouteId)
            .HasColumnName("new_route_id")
            .IsRequired();

        builder.Property(rc => rc.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(rc => rc.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(rc => rc.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(rc => rc.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(rc => rc.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        builder.Property(rc => rc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(rc => rc.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(rc => rc.ReviewedByAdminId)
            .HasColumnName("reviewed_by_admin_id");

        builder.Property(e => e.IsDeleted)
    .HasColumnName("is_deleted")
    .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(rc => rc.ReviewedByAdmin)
            .WithMany(a => a.ReviewedRouteChangeRequests)
            .HasForeignKey(rc => rc.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rc => rc.Driver)
            .WithMany(d => d.RouteChangeRequests)
            .HasForeignKey(rc => rc.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rc => rc.CurrentRoute)
            .WithMany(r => r.CurrentRouteChangeRequests)
            .HasForeignKey(rc => rc.CurrentRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rc => rc.NewRoute)
            .WithMany(r => r.NewRouteChangeRequests)
            .HasForeignKey(rc => rc.NewRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rc => new { rc.DriverId, rc.Status })
            .HasDatabaseName("idx_route_change_driver_status");
    }
}