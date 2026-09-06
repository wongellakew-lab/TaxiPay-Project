using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Infrastructure.Persistence.Context;

public class TaxiPayDbContext(DbContextOptions<TaxiPayDbContext> options) 
    : IdentityDbContext<TaxiPayUser, IdentityRole, string>(options)
{
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<FareMatrix> FareMatrices => Set<FareMatrix>();
    public DbSet<DriverShift> DriverShifts => Set<DriverShift>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<PassengerJourney> PassengerJourneyss => Set<PassengerJourney>();
    public DbSet<RouteChangeRequest> RouteChangeRequests => Set<RouteChangeRequest>();
    public DbSet<PassengerRouteRequest> PassengerRouteRequests => Set<PassengerRouteRequest>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaxiPayDbContext).Assembly);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            modelBuilder.Entity<Route>()
                .Property(r => r.Nodes)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Node>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Node>());
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.Id == Guid.Empty)
                    {
                        entry.Entity.Id = Guid.NewGuid();
                    }
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}