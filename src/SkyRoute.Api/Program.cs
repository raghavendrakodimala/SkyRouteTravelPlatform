using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Middleware;
using SkyRoute.Application.Data;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Services;
using SkyRoute.Application.Validation;
using SkyRoute.Infrastructure.Persistence;
using SkyRoute.Infrastructure.Providers;
using SkyRoute.Infrastructure.Tenancy;

const string AngularDevClientCorsPolicy = "AngularDevClient";

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// MVC / Controllers
// ---------------------------------------------------------------------------
builder.Services.AddControllers();

// SearchRequestValidator/BookingRequestValidator are the single authoritative source of
// the exact field-keyed validation messages returned to clients (feature-flight-search.md
// Section 4.1, feature-booking-flow.md Section 7). Suppressing the automatic
// [ApiController] model-state filter ensures those custom messages — not ASP.NET Core's
// generic DataAnnotations defaults — are always what the client receives (DP-014, FR-063).
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// ---------------------------------------------------------------------------
// CORS — restricted to the configured Angular dev client origin only, never a wildcard
// (ASM-012, NFR-SEC-006, DP-DEPLOY-001: externalised via configuration, not hardcoded).
// ---------------------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevClientCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ---------------------------------------------------------------------------
// Authorization — AD-007/DP-AUTH-004/DP-POLICY-001: a named policy stub demonstrating the
// pluggable-policy pattern exists at zero runtime cost. Never applied via [Authorize]
// anywhere in MVP code (BR-010: no authentication/authorization required for MVP).
// ---------------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireBookingOwner", policy => policy.RequireAssertion(_ => true));
});

// ---------------------------------------------------------------------------
// Application-layer services (stateless — safe as singletons)
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<AirportDataService>();
builder.Services.AddSingleton<RouteTypeResolver>();
builder.Services.AddSingleton<BookingReferenceGenerator>();
builder.Services.AddScoped<SearchRequestValidator>();
builder.Services.AddScoped<BookingRequestValidator>();

// ---------------------------------------------------------------------------
// Providers — multiple registrations of IFlightProvider resolve as IEnumerable<IFlightProvider>
// (FR-053). Adding a third provider requires only one new class + one new AddScoped line.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IFlightProvider, GlobalAirProvider>();
builder.Services.AddScoped<IFlightProvider, BudgetWingsProvider>();

// ---------------------------------------------------------------------------
// Orchestration services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IFlightAggregatorService, FlightAggregatorService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// ---------------------------------------------------------------------------
// Infrastructure — singleton in-memory store (BR-008 thread-safety, NFR-SCALE-002) and the
// default (single-tenant) tenant context (DP-TENANT-002).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IBookingStore, InMemoryBookingStore>();
builder.Services.AddSingleton<ITenantContext, DefaultTenantContext>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline — ApiExceptionMiddleware registered first so it wraps every downstream request
// (BR-011, DP-007, architecture-plan.md Section 3.6).
// ---------------------------------------------------------------------------
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors(AngularDevClientCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

// Test-support only, non-behavioral: top-level statements compile to an internal Program
// class by default. WebApplicationFactory<Program> (SkyRoute.Api.IntegrationTests) needs that
// class to be reachable from a separate test assembly, so it is declared partial and public
// here — the standard, minimal convention for enabling WebApplicationFactory<Program> without
// altering any runtime behavior.
public partial class Program;
