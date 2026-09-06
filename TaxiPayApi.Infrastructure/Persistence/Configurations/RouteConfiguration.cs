using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(r => r.Version)
            .IsRowVersion();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.RouteCode)
            .HasColumnName("route_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Nodes)
            .HasColumnName("nodes")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(r => r.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(r => r.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasMany(r => r.FareMatrices)
            .WithOne(f => f.Route)
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Shifts)
            .WithOne(s => s.Route)
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Trips)
            .WithOne(t => t.Route)
            .HasForeignKey(t => t.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.CurrentRouteChangeRequests)
            .WithOne(rc => rc.CurrentRoute)
            .HasForeignKey(rc => rc.CurrentRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.NewRouteChangeRequests)
            .WithOne(rc => rc.NewRoute)
            .HasForeignKey(rc => rc.NewRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.RouteCode)
            .IsUnique()
            .HasDatabaseName("idx_routes_code");

        builder.HasIndex(r => r.Name)
            .HasDatabaseName("idx_routes_name");
    }
}