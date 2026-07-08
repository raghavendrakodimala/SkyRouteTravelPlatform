# Current Handoff — HO-045 (EF Core + SQLite booking persistence, DEC-025)

| Field | Value |
|---|---|
| Handoff ID | HO-045 |
| Date | 2026-07-08 |
| Branch | main (uncommitted working tree — orchestrator to branch/commit per git workflow) |
| Phase | Post-closure PO-approved task — booking persistence on EF Core + SQLite in-memory (DEC-025) |
| From agent | database-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete — implementation + tests + live run green; awaiting commit/merge |

Full numbered handoff: `docs/handoffs/39-database-engineer-to-sdlc-orchestrator-efcore-persistence.md`.

## Work completed

Replaced the in-memory `ConcurrentDictionary` booking store with **EF Core over SQLite in-memory**, turning DATA-MODEL-001's Bookings aggregate into a real, constraint-enforcing schema while keeping the `IBookingStore` contract and the immutable domain unchanged. All EF Core code is confined to `SkyRoute.Infrastructure` (DP-PERSIST-001); domain POCOs stay annotation-free (DP-PERSIST-002) — mapped via Fluent API + EF constructor/init binding, **no domain class modified**.

- `AppDbContext` + `BookingConfiguration`: `Booking` (PK BookingReference), owned `BookingFlightSnapshot` flattened onto Bookings (§1.6), owned `PassengerDetail` collection → `BookingPassengers` table (FK + 1-based order-preserving ordinal = PK, realising §1.7's `UNIQUE(BookingReference, PassengerOrdinal)`).
- Real constraints emitted + verified live: PK, NOT NULLs, and CHECKs — `CK_Bookings_PaxCount` (1–9), `CK_BookingPassengers_Age` (0–120), `CK_Bookings_Total`/`_Price` (>0), `CK_Bookings_Times` (Arrival>Departure), `CK_Bookings_RefFormat`, `CK_BookingPassengers_DocType`/`_NameLen`/`_DocLen`/`_Ordinal`.
- `EfCoreBookingStore` (registered default): duplicate reference → SQLite UNIQUE/PK violation (extended 1555/2067) translated to `DuplicateBookingReferenceException`, so BookingService's CR-003 retry loop is unchanged; a CHECK violation (275) is NOT translated (stays a 500). Queries filter by `tenantId`; list orders by `CreatedAtUtc`.
- DI (`PersistenceServiceCollectionExtensions`): single kept-open `:memory:` `SqliteConnection` singleton shared by scoped contexts; `EnsureCreated()` at startup + per WebApplicationFactory host. `ConnectionStrings:Bookings` swaps to a durable DB (one-line `UseSqlServer`/`UseNpgsql` later) with no service/domain change. `InMemoryBookingStore` retained as the alternative.

## Artifacts created or updated

- Created: `.../Persistence/AppDbContext.cs`, `BookingConfiguration.cs`, `EfCoreBookingStore.cs`, `PersistenceServiceCollectionExtensions.cs`; `tests/.../Persistence/EfCoreBookingStoreTests.cs`.
- Updated: `SkyRoute.Infrastructure.csproj` (EF Core Sqlite 10.0.9 + `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 NU1903 pin), `SkyRoute.Application.Tests.csproj` (EF Core Sqlite 10.0.9), `SkyRoute.Api/Program.cs` (DI + EnsureCreated).
- Docs: `docs/architecture/data-model.md` (§4 rewritten, §5.3 A/B implemented), `docs/delivery/decision-log.md` (DEC-025), `README.md`, this + numbered handoff + `handoff-index.md`.

## Validation evidence

- `dotnet build SkyRoute.slnx` → **0 Warning(s), 0 Error(s)**.
- `dotnet test SkyRoute.slnx --no-build` → **247 passed, 0 failed** (Application.Tests 207, Api.IntegrationTests 40); +8 new `EfCoreBookingStoreTests` over the 239 baseline.
- Live run (`--urls http://localhost:5099`): `/api/health` Healthy (booking-store SELECT roundtrip); POST `/api/v1/bookings` → 201 refs `SKY-INT-T21B6S` (575.00) and `SKY-INT-3LEF30`. Server log confirmed EnsureCreated built both tables + all 10 CHECK constraints, `INSERT INTO Bookings`×2 / `BookingPassengers`×3, and a `SELECT ... FROM Bookings` read-back within the run. Server stopped (taskkill); port 5099 confirmed free.

## Decisions made

- DEC-025 (PO-approved): EF Core + SQLite in-memory booking store, real constraint enforcement, provider-swap-ready via connection string; `InMemoryBookingStore` retained as the alternative.
- 1-based `PassengerOrdinal` (value-generated keys treat 0 as unset); composite PK makes a surrogate id unnecessary; `PassengerCount` materialized for the 1–9 CHECK; `CabinClass` in two columns (immutable domain has both fields); `RouteType` not materialized (not on the domain); money→TEXT with `CAST(... AS REAL)>0` CHECKs; DateTime→ISO-8601 TEXT (lexical ordering backs the time CHECK). See DATA-MODEL-001 §4.
- Native SQLite bundle pinned to 3.0.3 to clear NU1903/CVE-2025-6965.

## Open questions

None blocking. Reference-data tables remain §5.3 Phase C, not yet adopted.

## Risks and impediments

- Default store is SQLite `:memory:` — bookings still do not survive a restart (durable swap is a connection-string change). Documented in README limitations.
- Single shared SQLite connection serializes concurrent access — fine for MVP/tests (integration test classes run sequentially, each with its own host/DB).

## Required next agent action

sdlc-orchestrator: branch, commit, and `--no-ff` merge to `main` per the git workflow (no push unless the PO approves).

## Completion criteria for next step

Changes committed and merged per the phased workflow; working tree clean.

## Relevant files

- `src/Service/SkyRoute.Infrastructure/Persistence/{AppDbContext,BookingConfiguration,EfCoreBookingStore,PersistenceServiceCollectionExtensions,InMemoryBookingStore}.cs`
- `src/Service/SkyRoute.Api/Program.cs`
- `src/Service/tests/SkyRoute.Application.Tests/Persistence/EfCoreBookingStoreTests.cs`
- `docs/architecture/data-model.md`, `docs/delivery/decision-log.md`, `README.md`
