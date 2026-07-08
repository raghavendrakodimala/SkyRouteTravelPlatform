using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using SkyRoute.Api.HealthChecks;
using SkyRoute.Api.Middleware;
using SkyRoute.Api.Routing;
using SkyRoute.Application.Data;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Services;
using SkyRoute.Application.Validation;
using SkyRoute.Infrastructure.Persistence;
using SkyRoute.Infrastructure.Providers;
using SkyRoute.Infrastructure.Tenancy;

const string AngularDevClientCorsPolicy = "AngularDevClient";

// The single source of truth for the public route prefix (PO directives 2026-07-08):
// applied to every controller via GlobalRoutePrefixConvention. The {version} segment is
// resolved by Asp.Versioning (URL-segment reader) — business endpoints live under
// /api/v1/... with v1 as the default version; new versions are added per-controller via
// [ApiVersion("2.0")] without touching this prefix.
const string ApiRoutePrefix = "api/v{version:apiVersion}";

// Operational/documentation surfaces stay under the plain /api prefix, unversioned:
// health and API docs describe the SERVICE, not one contract version.
const string ApiInfraPrefix = "api";

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// MVC / Controllers — controllers declare only their resource segment ("search",
// "bookings"); the global prefix is combined by convention, never hardcoded per controller.
// ---------------------------------------------------------------------------
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new GlobalRoutePrefixConvention(ApiRoutePrefix));
});

// ---------------------------------------------------------------------------
// API versioning (PO directive 2026-07-08, DEC-023): URL-segment versioning with v1 as the
// default version; every current endpoint is mapped to v1 via [ApiVersion("1.0")] on the
// controllers. ReportApiVersions adds api-supported-versions/api-deprecated-versions
// response headers so clients can discover the version surface.
// ---------------------------------------------------------------------------
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        // "'v'VVV" → v1, v1.1, v2 …; substitution renders /api/v1/... (not /api/v{version}/...)
        // in the generated OpenAPI document.
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

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
    ?? [];

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
// FlightFareResolver depends on IEnumerable<IFlightProvider> (scoped), so it must itself be
// scoped rather than singleton (SEC-001, Phase 16 security review fix).
builder.Services.AddScoped<FlightFareResolver>();
builder.Services.AddScoped<IBookingService, BookingService>();

// ---------------------------------------------------------------------------
// Infrastructure — booking persistence is EF Core over SQLite (real PK/NOT NULL/CHECK
// constraint enforcement, DATA-MODEL-001), registered entirely inside SkyRoute.Infrastructure so
// no EF Core/DbContext type leaks into this composition root (DP-PERSIST-001). With no
// ConnectionStrings:Bookings configured it defaults to a single kept-open SQLite :memory:
// connection (bookings persist across requests within a run); supplying that key swaps in a real
// durable database with no other change (NFR-MAINT-001). InMemoryBookingStore still exists behind
// the same IBookingStore contract as the alternative implementation. The default (single-tenant)
// tenant context (DP-TENANT-002) is unchanged.
// ---------------------------------------------------------------------------
builder.Services.AddBookingPersistence(builder.Configuration.GetConnectionString("Bookings"));
builder.Services.AddSingleton<ITenantContext, DefaultTenantContext>();

// ---------------------------------------------------------------------------
// Health checks (PO directive 2026-07-08): /api/health reports overall service health from
// three checks — API liveness, a real booking-store read roundtrip, and the flight-provider
// registrations. Built-in framework feature; no additional package.
// ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running."))
    .AddCheck<BookingStoreHealthCheck>("booking-store")
    .AddCheck<FlightProvidersHealthCheck>("flight-providers");

// ---------------------------------------------------------------------------
// API documentation (PO directive 2026-07-08, DEC-023): the built-in ASP.NET Core OpenAPI
// generator (Microsoft.AspNetCore.OpenApi — emits OpenAPI 3.1, the latest supported format)
// with Scalar as the interactive reference UI (the post-Swashbuckle default pairing for
// .NET 9+). Both endpoints are mapped in Development only — this MVP runs locally via
// `dotnet run` (Development), and API docs are not a production surface.
// ---------------------------------------------------------------------------
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "SkyRoute API";
        document.Info.Version = "v1";
        document.Info.Description =
            "Flight search and booking API aggregating GlobalAir and BudgetWings provider schedules. "
            + "All endpoints are unauthenticated in the MVP (BR-010).";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Create the booking schema (tables + PK/NOT NULL/CHECK constraints) before the first request.
// For the in-memory SQLite path this also pins the schema onto the shared, kept-open connection
// that every request will reuse. Runs for the real app AND for each WebApplicationFactory test
// host, so every host starts with an isolated, schema-created database.
app.Services.InitializeBookingPersistence();

// ---------------------------------------------------------------------------
// Pipeline — ApiExceptionMiddleware registered first so it wraps every downstream request
// (BR-011, DP-007, architecture-plan.md Section 3.6).
// ---------------------------------------------------------------------------
app.UseMiddleware<ApiExceptionMiddleware>();

// SEC-003 (Phase 16 security review): baseline HTTP security response headers on every
// response. HSTS is intentionally omitted — this MVP is local-only, HTTP dev (no TLS
// termination in front of Kestrel here), and Strict-Transport-Security has no meaningful
// effect (and can be actively misleading) over plain HTTP.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

// AUD-037/045: UseHttpsRedirection is intentionally NOT registered. This MVP runs local-only
// over plain HTTP (`dotnet run`, no TLS termination in front of Kestrel — the same posture that
// makes SEC-003 omit HSTS above). With no configured HTTPS port the redirect is dead noise, and
// running it ahead of UseCors (as it previously did) broke cross-origin API calls under the
// https launch profile by issuing a redirect before the CORS headers were applied. Removing it
// is the clean fix for the documented HTTP-only dev flow; a real TLS-terminated deployment would
// reintroduce redirection AFTER CORS, behind the reverse proxy that owns TLS.
app.UseCors(AngularDevClientCorsPolicy);

app.UseAuthorization();

app.MapControllers();

// /api/health — always mapped (health is an operational surface, not a docs surface).
// AUD-035 (OWASP A05:2021 / CWE-200): the PUBLIC, unauthenticated body is trimmed to a coarse
// liveness shape — overall status plus each check's name and status only. The per-check
// free-text descriptions (which previously leaked internal CLR provider class names and a
// booking-existence oracle) are deliberately omitted from the response; that detail remains
// server-side, carried on each HealthCheckResult for logging and any future
// authenticated/Development-only detailed endpoint.
app.MapHealthChecks($"/{ApiInfraPrefix}/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
            }),
        });
        await context.Response.WriteAsync(payload);
    },
});

if (app.Environment.IsDevelopment())
{
    // Everything lives under the global prefix (PO directive 2026-07-08):
    // /api/openapi/v1.json — the OpenAPI 3.1 document; /api/scalar/v1 — interactive reference.
    app.MapOpenApi($"/{ApiInfraPrefix}/openapi/{{documentName}}.json");
    app.MapScalarApiReference($"/{ApiInfraPrefix}/scalar", options =>
    {
        options.Title = "SkyRoute API Reference";
        options.OpenApiRoutePattern = $"/{ApiInfraPrefix}/openapi/{{documentName}}.json";
    });
}

app.Run();

// Test-support only, non-behavioral: top-level statements compile to an internal Program
// class by default. WebApplicationFactory<Program> (SkyRoute.Api.IntegrationTests) needs that
// class to be reachable from a separate test assembly, so it is declared partial and public
// here — the standard, minimal convention for enabling WebApplicationFactory<Program> without
// altering any runtime behavior.
public partial class Program;
