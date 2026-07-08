using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyRoute.Application.Domain;

namespace SkyRoute.Infrastructure.Persistence;

/// <summary>
/// Fluent-API mapping for the Booking aggregate (DATA-MODEL-001 §1.6/§1.7, §5.1 DDL). All
/// mapping lives here so the domain POCOs remain annotation-free (DP-PERSIST-002). The immutable
/// domain (required/init members, no public setters, IReadOnlyList collection) is mapped without
/// weakening it: EF Core 10 materialises via the implicit parameterless constructor + init/backing
/// fields and honours required members, so no EF-only constructor, setter, or shadow key had to be
/// added to any domain class.
///
/// Constraint fidelity vs. §5.1: because SQLite has no DECIMAL/DATETIME types (money is stored as
/// TEXT preserving exact decimal scale; DateTime as lexically-sortable ISO-8601 TEXT), the money
/// CHECKs CAST to REAL for the positivity test and the time-ordering CHECK relies on ISO-8601
/// lexical ordering. On a real SQL Server / PostgreSQL swap those same logical constraints are
/// emitted natively (DECIMAL(10,2) &gt; 0, DATETIME2 comparison) with no code change here.
/// </summary>
public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> booking)
    {
        // Bookings table + aggregate-root-level CHECK constraints (§5.1 CK_Bookings_*).
        booking.ToTable("Bookings", table =>
        {
            // NFR-DATA-001 defence in depth: the reference format is normally guaranteed by
            // BookingReferenceGenerator; the DB still refuses anything outside SKY-INT-/SKY-DOM-.
            table.HasCheckConstraint(
                "CK_Bookings_RefFormat",
                "BookingReference LIKE 'SKY-INT-%' OR BookingReference LIKE 'SKY-DOM-%'");

            // SEC-002 / §1.6: 1..9 passengers. Enforced on the materialised PassengerCount column
            // (see the shadow property below) — §5.4 item 3 "PassengerCount ... materialised".
            table.HasCheckConstraint("CK_Bookings_PaxCount", "PassengerCount BETWEEN 1 AND 9");

            // Money is TEXT on SQLite, so CAST to REAL for the > 0 test (a plain TEXT '>' would be
            // lexical and would wrongly accept e.g. '0.00'). CAST('0.00' AS REAL)=0 is rejected.
            table.HasCheckConstraint("CK_Bookings_Total", "CAST(TotalPrice AS REAL) > 0");
            table.HasCheckConstraint("CK_Bookings_Price", "CAST(PricePerPassenger AS REAL) > 0");

            // §1.6: arrival strictly after departure. ISO-8601 TEXT sorts lexically = chronological.
            table.HasCheckConstraint("CK_Bookings_Times", "ArrivalDateTimeUtc > DepartureDateTimeUtc");
        });

        // PK = BookingReference (CHAR(14)) — replaces the ConcurrentDictionary key; the unique PK
        // index is now the atomic source of truth for reference uniqueness (§1.8, NFR-DATA-001).
        booking.HasKey(b => b.BookingReference);
        booking.Property(b => b.BookingReference)
            .HasColumnName("BookingReference")
            .HasMaxLength(14)
            .IsFixedLength()
            .ValueGeneratedNever();

        booking.Property(b => b.TenantId)
            .HasColumnName("TenantId")
            .HasMaxLength(50)
            .HasDefaultValue("default")
            .IsRequired();

        // §5.4 item 1: the domain carries CabinClass on BOTH Booking and the flight snapshot (always
        // identical, set from the same request field). EF cannot map two properties to one physical
        // column, so this maps to a distinct "CabinClass" column while the snapshot's copy maps to
        // "FlightCabinClass" below. The paper "one column" ideal is a future normalisation; this
        // faithfully reflects the actual immutable domain without altering it.
        booking.Property(b => b.CabinClass)
            .HasColumnName("CabinClass")
            .HasMaxLength(20)
            .IsRequired();

        booking.Property(b => b.TotalPrice)
            .HasColumnName("TotalPrice")
            // Explicit store type avoids EF's SQLite "decimal defaults to TEXT" runtime warning;
            // the DECIMAL(10,2) semantics live in the CHECK above + app-side away-from-zero rounding.
            // A real provider swap uses .HasPrecision(10, 2) here instead — nothing else changes.
            .HasColumnType("TEXT")
            .IsRequired();

        booking.Property(b => b.CreatedAtUtc)
            .HasColumnName("CreatedAtUtc")
            .IsRequired();

        // §5.4 item 3: PassengerCount is not on the domain object (it is derived from the passenger
        // list). It is materialised here as a shadow column purely so the DB can enforce the 1..9
        // CHECK above. EfCoreBookingStore sets it from Passengers.Count on write — the domain is
        // untouched.
        booking.Property<int>("PassengerCount")
            .HasColumnName("PassengerCount")
            .IsRequired();

        ConfigureFlightSnapshot(booking);
        ConfigurePassengers(booking);
    }

    // §1.6: the flight snapshot is an OWNED single — its columns live on the Bookings table
    // (AD-004 keeps a snapshot, never a FK to a mutable Flights row).
    private static void ConfigureFlightSnapshot(EntityTypeBuilder<Booking> booking)
    {
        booking.OwnsOne(b => b.Flight, flight =>
        {
            flight.Property(f => f.Provider)
                .HasColumnName("ProviderName").HasMaxLength(50).IsRequired();
            flight.Property(f => f.FlightNumber)
                .HasColumnName("FlightNumber").HasMaxLength(10).IsRequired();
            flight.Property(f => f.Origin)
                .HasColumnName("OriginCode").HasMaxLength(3).IsFixedLength().IsRequired();
            flight.Property(f => f.Destination)
                .HasColumnName("DestinationCode").HasMaxLength(3).IsFixedLength().IsRequired();
            flight.Property(f => f.DepartureDateTime)
                .HasColumnName("DepartureDateTimeUtc").IsRequired();
            flight.Property(f => f.ArrivalDateTime)
                .HasColumnName("ArrivalDateTimeUtc").IsRequired();
            flight.Property(f => f.CabinClass)
                .HasColumnName("FlightCabinClass").HasMaxLength(20).IsRequired();
            flight.Property(f => f.PricePerPassenger)
                .HasColumnName("PricePerPassenger").HasColumnType("TEXT").IsRequired();
        });

        // The snapshot is mandatory — keep its flattened columns NOT NULL.
        booking.Navigation(b => b.Flight).IsRequired();
    }

    // §1.7: passengers are an OWNED collection in a separate BookingPassengers table, keyed by
    // (BookingReference, PassengerOrdinal). The composite natural key IS the UNIQUE(BookingReference,
    // PassengerOrdinal) constraint from §1.7 — a surrogate BookingPassengerId identity is unnecessary
    // once the ordinal is part of the key. The ordinal (set from list index by EfCoreBookingStore)
    // preserves request order: EF orders the owned collection by its key when materialising, so read
    // order == write order == request order.
    private static void ConfigurePassengers(EntityTypeBuilder<Booking> booking)
    {
        booking.OwnsMany(b => b.Passengers, pax =>
        {
            pax.ToTable("BookingPassengers", table =>
            {
                // §1.7 sanity bounds (DEC-022: no business rule bound to age).
                table.HasCheckConstraint("CK_BookingPassengers_Age", "Age BETWEEN 0 AND 120");
                // §1.7 order preservation: 1-based ordinal, 1..9 for the 1..9 allowed passengers.
                table.HasCheckConstraint("CK_BookingPassengers_Ordinal", "PassengerOrdinal BETWEEN 1 AND 9");
                table.HasCheckConstraint(
                    "CK_BookingPassengers_DocType",
                    "DocumentType IN ('Passport', 'National ID')");
                table.HasCheckConstraint("CK_BookingPassengers_NameLen", "length(FullName) >= 2");
                table.HasCheckConstraint("CK_BookingPassengers_DocLen", "length(DocumentNumber) BETWEEN 5 AND 20");
            });

            // FK → Bookings (shadow, since PassengerDetail has no back-reference in the domain).
            pax.WithOwner().HasForeignKey("BookingReference");

            // Order preservation (§1.7): a shadow ordinal that, combined with the FK, is the PK —
            // natively realising §1.7's UNIQUE(BookingReference, PassengerOrdinal), so a surrogate
            // BookingPassengerId identity is unnecessary. It is marked value-generated so Add() gives
            // each new passenger a distinct TEMP key (avoiding an identity-map collision on the
            // default 0); EfCoreBookingStore then assigns the real 1-based ordinal from the request
            // list index before SaveChanges. EF orders the collection by this key when materialising,
            // so read order == write order == request order. (EF Core 10 does not auto-populate an
            // owned-collection synthetic key, so it is assigned explicitly.)
            pax.Property<int>("PassengerOrdinal").HasColumnName("PassengerOrdinal").ValueGeneratedOnAdd();
            pax.HasKey("BookingReference", "PassengerOrdinal");

            pax.Property(p => p.FullName).HasColumnName("FullName").HasMaxLength(100).IsRequired();
            pax.Property(p => p.Age).HasColumnName("Age").IsRequired();
            pax.Property(p => p.Email).HasColumnName("Email").HasMaxLength(254).IsRequired();
            pax.Property(p => p.DocumentType).HasColumnName("DocumentType").HasMaxLength(11).IsRequired();
            pax.Property(p => p.DocumentNumber).HasColumnName("DocumentNumber").HasMaxLength(20).IsRequired();
        });
    }
}
