using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Configurations;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("disputes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(d => d.Version)
            .IsRowVersion();

        builder.Property(d => d.TripId)
            .HasColumnName("trip_id")
            .IsRequired();

        builder.Property(d => d.PassengerId)
            .HasColumnName("passenger_id")
            .IsRequired();

        builder.Property(d => d.DriverId)
            .HasColumnName("driver_id")
            .IsRequired();

        builder.Property(d => d.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.ResolutionNotes)
            .HasColumnName("resolution_notes")
            .HasMaxLength(500);

        builder.Property(d => d.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(d => d.ResolvedByAdminId)
            .HasColumnName("resolved_by_admin_id");

        builder.Property(e => e.IsDeleted)
    .HasColumnName("is_deleted")
    .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(d => d.ResolvedByAdmin)
            .WithMany(a => a.ResolvedDisputes)
            .HasForeignKey(d => d.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Trip)
            .WithOne(t => t.Dispute)
            .HasForeignKey<Dispute>(d => d.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Passenger)
            .WithMany(p => p.Disputes)
            .HasForeignKey(d => d.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Driver)
            .WithMany()
            .HasForeignKey(d => d.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("idx_disputes_status");

        builder.HasIndex(d => d.PassengerId)
            .HasDatabaseName("idx_disputes_passenger");
    }
}