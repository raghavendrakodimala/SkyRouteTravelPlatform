using Microsoft.EntityFrameworkCore;
using SkyRoute.Application.Domain;

namespace SkyRoute.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the booking aggregate (DATA-MODEL-001). Lives entirely in
/// SkyRoute.Infrastructure so no EF Core type crosses the SkyRoute.Application boundary
/// (DP-PERSIST-001): the domain POCOs stay annotation-free (DP-PERSIST-002) and all mapping is
/// Fluent-API only, via <see cref="BookingConfiguration"/>.
///
/// The Booking aggregate is mapped exactly as DATA-MODEL-001 §1.6/§1.7 describes it:
/// <see cref="Booking"/> is the aggregate root (PK = BookingReference), the
/// <see cref="BookingFlightSnapshot"/> is an owned single flattened onto the Bookings table
/// (§1.6), and <see cref="PassengerDetail"/> is an owned collection in a separate
/// BookingPassengers table with an ordinal that preserves request order (§1.7). Real
/// PK/NOT NULL/CHECK constraints are emitted so the store enforces integrity at the DB boundary
/// rather than trusting callers.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
    }
}
