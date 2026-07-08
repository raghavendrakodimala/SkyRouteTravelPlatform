using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Persistence;

/// <summary>
/// Composition-root wiring for EF Core booking persistence. Everything EF-specific stays inside
/// SkyRoute.Infrastructure (DP-PERSIST-001): SkyRoute.Api only calls these two extension methods
/// and never references a DbContext, connection, or provider type directly.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="EfCoreBookingStore"/> as the <see cref="IBookingStore"/> over an
    /// EF Core SQLite database.
    ///
    /// Provider-swap seam (NFR-MAINT-001): when <paramref name="bookingsConnectionString"/> is
    /// supplied (config key <c>ConnectionStrings:Bookings</c>) it is used directly — e.g. a
    /// file-based SQLite DB that survives restarts, a genuine "real database" path. Switching to
    /// SQL Server / PostgreSQL later is a one-line change below plus its NuGet package; the
    /// DbContext, mappings, store, and domain are untouched:
    ///     options.UseSqlServer(bookingsConnectionString);   // + Microsoft.EntityFrameworkCore.SqlServer
    ///
    /// Default MVP path (no connection string): a single SQLite <c>:memory:</c> connection, opened
    /// once and kept open for the whole app lifetime as a singleton. A <c>:memory:</c> database
    /// exists only while a connection to it is open, and each NEW connection gets its own empty
    /// database — so the scoped DbContext MUST reuse this one shared open connection. Passing a bare
    /// "DataSource=:memory:" string to UseSqlite instead would give every DbContext its own throwaway
    /// DB and bookings would vanish between requests.
    /// </summary>
    public static IServiceCollection AddBookingPersistence(this IServiceCollection services, string? bookingsConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(bookingsConnectionString))
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(bookingsConnectionString));
        }
        else
        {
            services.AddSingleton(_ =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                return connection;
            });

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
                options.UseSqlite(serviceProvider.GetRequiredService<SqliteConnection>()));
        }

        services.AddScoped<IBookingStore, EfCoreBookingStore>();
        return services;
    }

    /// <summary>
    /// Creates the schema (tables + constraints) before the first request. For the in-memory path
    /// this must run once at startup, after the shared connection is opened, so EnsureCreated builds
    /// the schema on the connection every request will share. Called once from Program.cs after
    /// <c>builder.Build()</c>; each WebApplicationFactory test host runs it too, giving every host an
    /// isolated, schema-created in-memory database.
    /// </summary>
    public static void InitializeBookingPersistence(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
}
