# Handoff — HO-045 (EF Core + SQLite booking persistence)

| Field | Value |
|---|---|
| Handoff ID | HO-045 |
| Date | 2026-07-08 |
| Branch | main (uncommitted working tree — orchestrator to branch/commit per git workflow) |
| Phase | Post-closure PO-approved task — booking persistence on EF Core + SQLite in-memory (DEC-025) |
| From agent | database-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete — implementation + tests + live run green; awaiting commit/merge |

## Work completed

Replaced the in-memory `ConcurrentDictionary` booking store with **EF Core over SQLite in-memory**, turning DATA-MODEL-001's Bookings aggregate into a real, constraint-enforcing schema while keeping the `IBookingStore` contract and the immutable domain unchanged. All EF Core code is confined to `SkyRoute.Infrastructure` (DP-PERSIST-001); the domain POCOs remain annotation-free (DP-PERSIST-002) — mapped via Fluent API + EF constructor/init binding, **no domain class was modified**.

- `AppDbContext` + `BookingConfiguration` map `Booking` (PK = BookingReference), the owned `BookingFlightSnapshot` (flattened onto the Bookings table, §1.6), and the owned `PassengerDetail` collection (separate `BookingPassengers` table, FK + 1-based order-preserving ordinal forming the PK = §1.7's `UNIQUE(BookingReference, PassengerOrdinal)`).
- Real constraints emitted and verified live: PK on BookingReference; NOT NULLs; CHECKs `CK_Bookings_PaxCount` (1–9), `CK_BookingPassengers_Age` (0–120), `CK_Bookings_Total`/`_Price` (> 0), `CK_Bookings_Times` (Arrival > Departure), `CK_Bookings_RefFormat`, `CK_BookingPassengers_DocType`/`_NameLen`/`_DocLen`/`_Ordinal`.
- `EfCoreBookingStore` implements all four `IBookingStore` methods; a duplicate-reference SaveChanges raises a SQLite UNIQUE/PK violation (`SqliteException` extended code 1555/2067), translated back into the same `DuplicateBookingReferenceException` — so BookingService's CR-003 retry loop is unchanged (a CHECK violation, extended 275, is deliberately NOT translated and surfaces as a real 500). Queries filter by `tenantId`; `ListByTenantAsync` orders by `CreatedAtUtc`.
- DI (`PersistenceServiceCollectionExtensions`): the default MVP path opens ONE `SqliteConnection("DataSource=:memory:")`, keeps it open as a singleton, and points the scoped `AppDbContext` at it (so all requests share one in-memory DB); `EnsureCreated()` runs at startup and per WebApplicationFactory test host. A `ConnectionStrings:Bookings` value switches to a durable DB (file SQLite now; one-line `UseSqlServer`/`UseNpgsql` + package later) with no DbContext/store/domain change (NFR-MAINT-001). `Program.cs` now calls `AddBookingPersistence(...)` + `InitializeBookingPersistence()` in place of the old singleton `InMemoryBookingStore` registration.
- `InMemoryBookingStore` retained (its unit tests intact) as the alternative implementation demonstrating the seam.

## Decisions made

- `PassengerOrdinal` is 1-based (EF value-generated keys treat 0 as "unset"); the composite `(BookingReference, PassengerOrdinal)` PK makes a surrogate `BookingPassengerId` unnecessary. `PassengerCount` materialized as a shadow column to carry the 1–9 CHECK. `CabinClass` stored in two columns (`CabinClass` + `FlightCabinClass`) because the immutable domain carries both fields. `RouteType` not materialized (domain does not carry it). Money → SQLite TEXT with `CAST(... AS REAL) > 0` CHECKs; `DateTime` → ISO-8601 TEXT (lexical ordering backs the time CHECK). All recorded in DATA-MODEL-001 §4 and DEC-025.
- Native SQLite bundle pinned to `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 to clear NU1903/CVE-2025-6965 (EF's transitive 2.1.11 is vulnerable) — keeps the build at 0 warnings.

## Artifacts created or updated

- Created: `src/Service/SkyRoute.Infrastructure/Persistence/AppDbContext.cs`, `BookingConfiguration.cs`, `EfCoreBookingStore.cs`, `PersistenceServiceCollectionExtensions.cs`; `src/Service/tests/SkyRoute.Application.Tests/Persistence/EfCoreBookingStoreTests.cs`.
- Updated: `src/Service/SkyRoute.Infrastructure/SkyRoute.Infrastructure.csproj` (EF Core Sqlite 10.0.9 + bundle 3.0.3), `src/Service/tests/SkyRoute.Application.Tests/SkyRoute.Application.Tests.csproj` (EF Core Sqlite 10.0.9), `src/Service/SkyRoute.Api/Program.cs` (DI wiring + EnsureCreated).
- Docs: `docs/architecture/data-model.md` (§4 rewritten, §5.3 Phases A/B marked implemented), `docs/delivery/decision-log.md` (DEC-025), `README.md` (persistence notes), this handoff + `current-handoff.md` + `handoff-index.md`.

## Open questions

None blocking. Reference-data tables (Airports/Providers/CabinClasses/Flights) remain §5.3 Phase C, not yet adopted.

## Risks and impediments

- Default store is SQLite `:memory:` — bookings still do not survive a process restart (by design for the MVP; durable swap is a connection-string change). Documented in README limitations.
- A single shared SQLite connection serializes concurrent access; fine for the MVP/tests (integration test classes run sequentially, each with its own host/DB). Noted in code comments.

## Required next agent action

Orchestrator: branch, commit, and `--no-ff` merge to `main` per the git workflow (working tree is otherwise clean of unrelated changes). No push unless the PO approves.

## Completion criteria for next step

- `dotnet build SkyRoute.slnx` 0 warnings / 0 errors — met.
- `dotnet test SkyRoute.slnx --no-build` all pass — met (247/247).
- Live run evidence captured (below) — met.

## Verification evidence

- Build: `dotnet build SkyRoute.slnx` → **0 Warning(s) / 0 Error(s)**.
- Tests: `dotnet test SkyRoute.slnx --no-build` → **247 passed** (SkyRoute.Application.Tests 207, SkyRoute.Api.IntegrationTests 40), 0 failed. Baseline was 239 (+8 new `EfCoreBookingStoreTests`).
- Live run (`dotnet run --project src/Service/SkyRoute.Api --urls http://localhost:5099`): `/api/health` → `Healthy` (booking-store check does a real ListByTenantAsync SELECT); POST `/api/v1/bookings` → **201** ref `SKY-INT-T21B6S` (total 575.00), second POST → 201 ref `SKY-INT-3LEF30`. Server log confirmed `EnsureCreated` built both tables with all 10 CHECK constraints, `INSERT INTO Bookings`×2 / `INSERT INTO BookingPassengers`×3, and a `SELECT ... FROM Bookings` join reading the persisted rows back within the run. Server stopped (taskkill); port 5099 confirmed free.

## Relevant files

- `src/Service/SkyRoute.Infrastructure/Persistence/{AppDbContext,BookingConfiguration,EfCoreBookingStore,PersistenceServiceCollectionExtensions,InMemoryBookingStore}.cs`
- `src/Service/SkyRoute.Api/Program.cs`
- `src/Service/tests/SkyRoute.Application.Tests/Persistence/EfCoreBookingStoreTests.cs`
- `docs/architecture/data-model.md`, `docs/delivery/decision-log.md`, `README.md`
