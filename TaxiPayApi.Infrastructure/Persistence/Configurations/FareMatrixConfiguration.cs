using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class FareMatrixConfiguration : IEntityTypeConfiguration<FareMatrix>
{
    public void Configure(EntityTypeBuilder<FareMatrix> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(f => f.Version)
            .IsRowVersion();

        builder.Property(f => f.RouteId)
            .HasColumnName("route_id");

        builder.Property(f => f.FromNode)
            .HasColumnName("from_node")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(f => f.ToNode)
            .HasColumnName("to_node")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(f => f.Fare)
            .HasColumnName("fare")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(f => f.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.IsDeleted)
    .HasColumnName("is_deleted")
    .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(f => f.Route)
            .WithMany(r => r.FareMatrices)
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.RouteId, f.FromNode, f.ToNode })
            .IsUnique()
            .HasDatabaseName("uq_fare_matrix");

        // builder.HasIndex(f => new { f.RouteId, f.FromNode, f.ToNode, f.IsActive })
        //     .HasDatabaseName("idx_fare_lookup");
    }
}