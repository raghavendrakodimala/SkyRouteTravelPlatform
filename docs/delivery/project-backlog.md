# Project Backlog — SkyRoute Travel Platform MVP

Version: 1.2
Date: 2026-07-07 (delivery-status reconciliation; content baseline 2026-07-03)
Author: project-coordinator
Status: Active — all items Delivered (see Section 13)
Phase: Phase 07 — Project Backlog Creation (Phase 08 — Parallel Delivery Plan applied the BL-033 decomposition below)

---

## 1. Purpose

This backlog decomposes the 8 approved user stories (`docs/requirements.md` v1.4, Approved) into concrete, implementable backlog items, each mapped to a specific component named in `docs/architecture/architecture-plan.md` v1.0. It is the single source Phase 08 (Parallel Delivery Plan), Phase 09 (Sprint Planning), and Phase 12 (Implementation) trace against.

No new scope, business rule, or priority is introduced by this document. Every item traces to an approved `US-*`/`FR-*`/`BR-*`/`DP-*` requirement and/or an architecture-plan component (`AD-*`, Section 2–5). Where an architecture-plan component appeared to imply something beyond an approved requirement, it is flagged in Section 7 (Open Questions) rather than silently built into a backlog item.

---

## 2. Sizing and Priority Conventions

- **Size** — T-shirt sizing only (XS/S/M/L). No story points or hours are used, per RISK-009 (no velocity baseline exists) and DEC-003 (T-shirt sizing approved for this sprint). Sizes are relative-complexity estimates, not time commitments.
- **Priority** — Inherited directly from the MoSCoW priority of the linked user story/FR in `docs/requirements.md`. All 8 user stories are Must Have; a small number of supporting FRs are Should Have (e.g., FR-054, FR-064, FR-072) and this is noted per item where relevant.
- **Dependencies** — Each item lists its direct blocking item(s) only (not the full transitive graph). The full parallel-track dependency graph is Phase 08's deliverable.
- **DoR check** — One-line confirmation against `.claude/rules/definition-of-ready.md`: business value clear (inherited from linked US-*), acceptance criteria traceable (linked AC/FR/BR), scope bounded (architecture component named), dependencies identified (listed). Since every item traces to already-approved, already-concretized specs, no new DoR analysis was required beyond this confirmation.

---

## 3. Backlog Item Structure

Each item includes: ID, Title, Linked User Story, Linked Architecture Component, Description, Size, Priority, Dependencies, DoR Check.

---

## 4. Backend Backlog Items

### BL-001 — Solution and Project Scaffolding

- **Linked User Story:** US-007 (enabling); all stories (foundational)
- **Linked Architecture Component:** Architecture Plan Section 2 — 3-project solution (`SkyRoute.Api`, `SkyRoute.Application`, `SkyRoute.Infrastructure`), AD-001
- **Description:** Create the `.sln` and three `.csproj` files per the approved solution structure. `SkyRoute.Application` and `SkyRoute.Infrastructure` as plain class libraries with zero `Microsoft.AspNetCore.*` package references (structural enforcement of DP-PROTOCOL-001/DP-AUTH-001-002). `SkyRoute.Api` as the Web SDK project referencing both. No test projects populated yet (test project scaffolding is Phase 13 scope, named for completeness in architecture plan Section 2).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** None (first item)
- **DoR Check:** Pass — architecture plan Section 2 fully specifies structure; no ambiguity.

### BL-002 — Domain Models

- **Linked User Story:** US-002, US-005, US-006, US-007, US-008
- **Linked Architecture Component:** `SkyRoute.Application/Domain/` — `Booking.cs`, `PassengerDetail.cs`, `FlightResult.cs`, `Airport.cs`
- **Description:** Implement persistence-agnostic, serialization-annotation-free POCOs per DP-PERSIST-002/DP-PROTOCOL-002: `Booking` (incl. `TenantId` field per DP-TENANT-005), `PassengerDetail`, `FlightResult` (incl. `provider`, `pricePerPassenger`, `baseFare`, durations per FR-010), `Airport` (IATA code, city, country, display name per FR-057).
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-001
- **DoR Check:** Pass — field shapes fully specified in architecture plan Section 5 (API contracts) and FR-010/FR-040/FR-043/FR-057; no ORM/serialization annotations permitted (DP-PERSIST-002).

### BL-003 — API Contract Models

- **Linked User Story:** US-001, US-006
- **Linked Architecture Component:** `SkyRoute.Application/Contracts/` — `SearchRequest.cs`, `BookingRequest.cs`, `BookingResponse.cs`; AD-004, AD-005
- **Description:** Implement request/response contract classes matching architecture plan Section 5 JSON shapes exactly (`SearchRequest` incl. `tripType` per BR-013/FR-005b; `BookingRequest` carrying full flight-detail snapshot per AD-004, not an opaque ID). DataAnnotations for field-level validation (`[Required]`, `[Range(1,9)]`, etc.) per AD-003 — no third-party validation package.
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-002
- **DoR Check:** Pass — full JSON contract shapes documented in architecture plan Section 5; FR-001/FR-039/FR-044 give the required fields.

### BL-004 — Airport Static Data (`AirportDataService`)

- **Linked User Story:** US-008
- **Linked Architecture Component:** `SkyRoute.Application/Data/AirportDataService.cs`; AD-002; frontend `shared/constants/airports.constants.ts` (see BL-013)
- **Description:** Concrete (non-interface, per YAGNI-001) class holding the backend's static airport list — minimum 6 airports across 2 countries, matching the recommended list in requirements.md Section 3.7 (LHR, MAN, JFK, LAX, DXB, SYD), used for backend validation of origin/destination codes (FR-006). This is the backend's independent copy per FR-055's single-source-per-layer allowance — the frontend constant (BL-013) is the separate authoritative source for the UI.
- **Size:** XS
- **Priority:** Must Have (FR-056/057/058/059 Must Have; FR-054 GET-endpoint alternative is Should Have and explicitly NOT built per AD-002)
- **Dependencies:** BL-002
- **DoR Check:** Pass — FR-056–059 give exact minimums; AD-002 resolves the FR-054/055 open choice (no `GET /api/airports` endpoint built).

### BL-005 — `RouteTypeResolver`

- **Linked User Story:** US-005
- **Linked Architecture Component:** `SkyRoute.Application/Services/RouteTypeResolver.cs`; DP-016
- **Description:** Single named backend method/class determining domestic vs. international route type from origin/destination country equality (BR-003). Authoritative — re-evaluated server-side at booking time regardless of client input (architecture plan Section 3.3 step 2).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-004
- **DoR Check:** Pass — BR-003 fully specifies the rule; DP-016 specifies the single-location constraint.

### BL-006 — Document Validation Patterns (`DocumentPatterns`)

- **Linked User Story:** US-005
- **Linked Architecture Component:** `SkyRoute.Application/Validation/` (named constants referenced by `BookingRequestValidator`); DP-015
- **Description:** Named constants for passport pattern (6–9 uppercase alphanumeric) and national ID pattern (5–20 alphanumeric with hyphens), per BR-003/US-005 AC6. Must be referenced (not duplicated inline) by `BookingRequestValidator` (BL-009) and mirrored in the Angular `document-number.validators.ts` (BL-017).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-002
- **DoR Check:** Pass — exact pattern rules given in US-005 AC6 and DP-015.

### BL-007 — `IFlightProvider` Interface

- **Linked User Story:** US-007
- **Linked Architecture Component:** `SkyRoute.Application/Interfaces/IFlightProvider.cs`; DP-001
- **Description:** Define the sole integration contract between aggregation and providers: `ProviderName` property, `SearchAsync(SearchRequest, CancellationToken)` returning `IReadOnlyList<FlightResult>` (architecture plan Section 3.1).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-002, BL-003
- **DoR Check:** Pass — interface signature fully specified in architecture plan Section 3.1; FR-046/FR-047 mandate it.

### BL-008 — `GlobalAirProvider` and `BudgetWingsProvider` Implementations

- **Linked User Story:** US-002, US-007
- **Linked Architecture Component:** `SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs`, `BudgetWingsProvider.cs`; DP-006, AD-009
- **Description:** Two concrete `IFlightProvider` implementations, each with a hardcoded realistic flight schedule (ASM-006 — fixed regardless of date/route, filtered only by cabin class per BR-009) and a named, isolated pricing method: `ApplyGlobalAirPricing` (base × 1.15, BR-001) and `ApplyBudgetWingsPricing` (max(base × 0.90, 29.99), BR-002, round-then-floor order per architecture plan Section 3.1 note). Each result tagged with `ProviderName` (FR-052). Placed in `SkyRoute.Infrastructure` per AD-009 (adapter role).
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-007
- **DoR Check:** Pass — BR-001/BR-002 give exact pricing formulas and rounding rules with worked examples; FR-048 mandates the two named providers.

### BL-009 — `IFlightAggregatorService` / `FlightAggregatorService`

- **Linked User Story:** US-007, US-002
- **Linked Architecture Component:** `SkyRoute.Application/Interfaces/IFlightAggregatorService.cs` + `Services/FlightAggregatorService.cs`; DP-004, AD-010
- **Description:** Aggregation orchestration: `SearchAsync` invokes all registered `IFlightProvider` instances concurrently via `Task.WhenAll`, with **per-provider try/catch wrapping each task before it enters `Task.WhenAll`** (AD-010 — the critical fault-isolation detail; `Task.WhenAll` alone does not swallow individual task exceptions). Failed providers return an empty collection and are logged (BR-007, FR-009, FR-050).
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-007, BL-008
- **DoR Check:** Pass — architecture plan Section 3.2 gives the exact implementation shape including the AD-010 fault-isolation detail; BR-007/FR-049/FR-050 are the acceptance criteria.

### BL-010 — `SearchRequestValidator`

- **Linked User Story:** US-001
- **Linked Architecture Component:** `SkyRoute.Application/Validation/SearchRequestValidator.cs`; AD-003
- **Description:** Cross-field/context validation beyond DataAnnotations: origin ≠ destination (FR-002), departure date not in the past (FR-003), passenger count 1–9 (FR-004), cabin class one of 3 values (FR-005), trip type must be `OneWay` (FR-005b), origin/destination are known airport codes via `AirportDataService` (FR-006). Produces field-keyed error dictionary for `ValidationProblem`.
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-003, BL-004
- **DoR Check:** Pass — FR-002–FR-006, FR-005b, FR-061 give every validation rule explicitly.

### BL-011 — `IBookingStore` / `InMemoryBookingStore`

- **Linked User Story:** US-006
- **Linked Architecture Component:** `SkyRoute.Application/Interfaces/IBookingStore.cs` + `SkyRoute.Infrastructure/Persistence/InMemoryBookingStore.cs`; DP-002, DP-PERSIST-001–005, DP-TENANT-006
- **Description:** Persistence-agnostic interface (`CreateAsync`, `GetByReferenceAsync`, `ExistsAsync`, `ListByTenantAsync` — all domain-object/scalar signatures only, no `ConcurrentDictionary<,>`/`IQueryable<T>` in the interface per DP-PERSIST-001) and its in-memory `ConcurrentDictionary`-backed implementation (thread-safe singleton per BR-008). All query operations carry a `tenantId` parameter (DP-TENANT-006), ignored by the MVP implementation but present in the signature.
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-002
- **DoR Check:** Pass — interface signature given verbatim in architecture plan Section 3.3; BR-008/DP-PERSIST-001–005/DP-TENANT-006 are the acceptance criteria.

### BL-012 — `ITenantContext` / `DefaultTenantContext`

- **Linked User Story:** US-006 (supporting seam; no direct US-* AC, but required by approved DP-TENANT-001–003)
- **Linked Architecture Component:** `SkyRoute.Application/Interfaces/ITenantContext.cs` + `SkyRoute.Infrastructure/Tenancy/DefaultTenantContext.cs`; DP-TENANT-001–002
- **Description:** Single-property interface (`string TenantId`) and its default implementation returning the constant `"default"`, registered in DI. Consumed by `BookingService` via constructor injection.
- **Size:** XS
- **Priority:** Must Have (DP-TENANT-001/002 are Must Have per requirements.md Section 3.10)
- **Dependencies:** BL-001
- **DoR Check:** Pass — DP-TENANT-001–002 fully specify the interface and default value; zero ambiguity.

### BL-013 — `BookingReferenceGenerator`

- **Linked User Story:** US-006
- **Linked Architecture Component:** `SkyRoute.Application/Services/BookingReferenceGenerator.cs`; BR-004, DP-018, AD-008
- **Description:** Standalone, dependency-free class generating `SKY-[INT/DOM]-[XXXXXX]` references using `RandomNumberGenerator` (not `System.Random`) for the 6-character uppercase alphanumeric suffix (BR-004). Extracted as its own class (beyond DP-018's minimum "extractable method") per AD-008, for trivial unit-testability without constructing `BookingService`.
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-001
- **DoR Check:** Pass — BR-004 gives exact format, character set, length, and RNG requirement.

### BL-014 — `BookingRequestValidator`

- **Linked User Story:** US-005, US-006
- **Linked Architecture Component:** `SkyRoute.Application/Validation/BookingRequestValidator.cs`; AD-003
- **Description:** Cross-field validation: passenger count matches passenger array length, each passenger has non-empty full name (min 2 chars, at least one letter per FR-064), valid email (RFC 5322 simplified, FR-065), non-empty document number conforming to the server-resolved route type's pattern (via BL-005 + BL-006). Produces field-keyed errors (e.g., `passengers[1].documentNumber`) for `ValidationProblem` (FR-062, FR-063).
- **Size:** S
- **Priority:** Must Have (FR-062/063/065 Must Have; FR-064 full-name letter check is Should Have)
- **Dependencies:** BL-003, BL-005, BL-006
- **DoR Check:** Pass — FR-062–065 give every validation rule explicitly.

### BL-015 — `IBookingService` / `BookingService`

- **Linked User Story:** US-006
- **Linked Architecture Component:** `SkyRoute.Application/Interfaces/IBookingService.cs` + `Services/BookingService.cs`; DP-003
- **Description:** Orchestrates the 7-step booking flow per architecture plan Section 3.3: validate (BL-014) → re-resolve route type server-side (BL-005, authoritative regardless of client input) → validate document format against resolved route type → recompute total price server-side (BR-006) → generate + collision-check booking reference (BL-013, BL-011) → build `Booking` domain object with `TenantId` (BL-012) and `n` `PassengerDetail` records (BR-005) → persist via `IBookingStore` (BL-011) → map to `BookingResponse` (FR-044).
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-005, BL-011, BL-012, BL-013, BL-014
- **DoR Check:** Pass — the full 7-step flow is specified verbatim in architecture plan Section 3.3, each step traced to BR-003/004/005/006/041 or FR-041/044.

### BL-016 — `ApiExceptionMiddleware`

- **Linked User Story:** All (cross-cutting; no single US-* owns this)
- **Linked Architecture Component:** `SkyRoute.Api/Middleware/ApiExceptionMiddleware.cs`; BR-011, DP-007
- **Description:** Single centralised exception-handling middleware, registered first in the pipeline, producing a generic 500 JSON body with no stack trace/exception type/internal message (FR-069). 400 responses bypass this middleware and are produced directly by controllers via `ValidationProblem` (architecture plan Section 3.6).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-001
- **DoR Check:** Pass — BR-011/DP-007/FR-069 fully specify the requirement; architecture plan Section 3.6 gives the exact registration point and response-body constraint.

### BL-017 — `SearchController`

- **Linked User Story:** US-001
- **Linked Architecture Component:** `SkyRoute.Api/Controllers/SearchController.cs`; DP-004 (controller side)
- **Description:** Thin `POST /api/search` controller: model-bind `SearchRequest`, invoke `SearchRequestValidator` (BL-010) → `ValidationProblem` if invalid, else `await IFlightAggregatorService.SearchAsync(...)` → `Ok(results)`. No aggregation or provider logic in the controller (DP-003/DP-004 analog).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-009, BL-010
- **DoR Check:** Pass — architecture plan Section 3.7 gives the exact controller responsibility boundary; FR-001/FR-007/FR-008 are the acceptance criteria.

### BL-018 — `BookingController`

- **Linked User Story:** US-006
- **Linked Architecture Component:** `SkyRoute.Api/Controllers/BookingController.cs`; DP-003
- **Description:** Thin `POST /api/bookings` controller: model-bind `BookingRequest`, invoke `BookingRequestValidator` (BL-014) → `ValidationProblem` if invalid, else `await IBookingService.CreateBookingAsync(...)` → `Created(...)`/`Ok(response)`. No business logic in the controller.
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-015, BL-014
- **DoR Check:** Pass — architecture plan Section 3.7; FR-039/FR-041/FR-042/FR-043/FR-044 are the acceptance criteria.

### BL-019 — DI Composition Root, CORS, and Configuration (`Program.cs`)

- **Linked User Story:** US-007 (extensibility demonstration); all stories (enabling)
- **Linked Architecture Component:** `SkyRoute.Api/Program.cs`, `appsettings.json`/`appsettings.Development.json`; architecture plan Section 3.8–3.9, AD-007
- **Description:** Wire all DI registrations exactly as listed in architecture plan Section 3.8 (`IBookingStore` singleton, `ITenantContext` singleton, `BookingReferenceGenerator` singleton, both `IFlightProvider` implementations scoped — resolved as `IEnumerable<IFlightProvider>` per FR-053, `IFlightAggregatorService`/`IBookingService` scoped, both validators scoped). Register the named `"RequireBookingOwner"` authorization-policy stub with an allow-all assertion, never applied via `[Authorize]` (AD-007/DP-AUTH-004/DP-POLICY-001 — zero-runtime-cost pattern demonstration). Configure CORS restricted to `http://localhost:4200` (never a wildcard, ASM-012, NFR-SEC target). Register `ApiExceptionMiddleware` first in the pipeline.
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-008, BL-009, BL-011, BL-012, BL-013, BL-016, BL-017, BL-018
- **DoR Check:** Pass — architecture plan Sections 3.5, 3.8, 3.9 give exact registration code; ASM-012 confirms the CORS requirement.

---

## 5. Frontend Backlog Items

### BL-020 — Angular Workspace Scaffolding and Routing Shell

- **Linked User Story:** US-001, US-004 (enabling); all stories
- **Linked Architecture Component:** `app.config.ts`, `app.routes.ts`; architecture plan Section 4.1, 4.4
- **Description:** Angular 22 standalone workspace with `provideHttpClient()`/`provideRouter(routes)`, and the four-route shell (`/search`, `/results`, `/booking`, `/confirmation`) per architecture plan Section 4.4, including the recommended functional `CanActivate` guard on `/booking` and `/confirmation` checking `BookingStateService` signals (Should Have — usability robustness, not FR-mandated).
- **Size:** S
- **Priority:** Must Have (routing shell); Should Have (guard)
- **Dependencies:** None (parallel-startable with BL-001)
- **DoR Check:** Pass — architecture plan Section 4.1/4.4 gives folder structure and route table verbatim.

### BL-021 — Shared Models (TypeScript)

- **Linked User Story:** US-001, US-002, US-005, US-006
- **Linked Architecture Component:** `shared/models/flight-result.model.ts`, `search-request.model.ts`, `booking-request.model.ts`, `passenger-detail.model.ts`, `airport.model.ts`
- **Description:** TypeScript interfaces matching the backend API contract shapes exactly (architecture plan Section 5), so `FlightSearchService`/`BookingService` (Angular) can type HTTP responses without duplication.
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-003 (backend contract shape must be settled first; can proceed in parallel once architecture Section 5 is treated as frozen)
- **DoR Check:** Pass — exact field shapes given in architecture plan Section 5 JSON examples.

### BL-022 — `airports.constants.ts`

- **Linked User Story:** US-008
- **Linked Architecture Component:** `shared/constants/airports.constants.ts`; AD-002, DP-012
- **Description:** Single-source static `AIRPORTS` constant array (minimum 6 airports, 2 countries, each with code/city/country/display name per FR-057), imported directly into `SearchFormComponent` — no wrapper service (YAGNI, since it is static non-HTTP data). Must not be duplicated in any component or service file (DP-012).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-020
- **DoR Check:** Pass — FR-056–059 give exact minimums; AD-002/DP-012 give the single-source constraint.

### BL-023 — `pricing.util.ts`

- **Linked User Story:** US-002, US-004
- **Linked Architecture Component:** `shared/utils/pricing.util.ts`; DP-011
- **Description:** Single named utility function computing total price = per-passenger price × passenger count, formatted as USD to 2 decimal places (FR-017). Must be the only place this calculation exists on the frontend — not duplicated between the results component and the booking component (DP-011).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-021
- **DoR Check:** Pass — FR-017/DP-011 fully specify the single-calculation constraint and formatting rule.

### BL-024 — `document-number.validators.ts`

- **Linked User Story:** US-005
- **Linked Architecture Component:** `shared/validators/document-number.validators.ts`; DP-015
- **Description:** Named pattern constants mirroring the backend's `DocumentPatterns` (BL-006) for immediate client-side feedback (passport: 6–9 uppercase alphanumeric; national ID: 5–20 alphanumeric with hyphens). Backend remains authoritative (DP-014) — this is a convenience layer only.
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-006 (pattern values must match, though implementation can proceed in parallel once BR-003/DP-015 patterns are known from the requirements doc directly)
- **DoR Check:** Pass — US-005 AC6/DP-015 give the exact patterns to mirror.

### BL-025 — `AuthService` (No-Op)

- **Linked User Story:** None directly (cross-cutting seam required by approved DP-AUTH-003)
- **Linked Architecture Component:** `core/services/auth.service.ts`
- **Description:** Single no-op `AuthService` class — no token storage, no header injection, no login/logout behaviour. No other component or feature service may reference an auth library or embed token-handling logic (DP-AUTH-003).
- **Size:** XS
- **Priority:** Must Have (DP-AUTH-003 is Must Have)
- **Dependencies:** BL-020
- **DoR Check:** Pass — DP-AUTH-003 fully specifies the no-op behaviour; zero ambiguity.

### BL-026 — `FlightSearchService` (Angular)

- **Linked User Story:** US-001
- **Linked Architecture Component:** `features/search/flight-search.service.ts`; DP-010, DP-PROTOCOL-006
- **Description:** Sole place `HttpClient` is injected for search. `search(request: SearchRequest): Observable<FlightResult[]>` calling `POST /api/search`. No component injects `HttpClient` directly (DP-010/DP-PROTOCOL-006).
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-021
- **DoR Check:** Pass — architecture plan Section 4.2 gives the exact method signature and endpoint.

### BL-027 — `SearchStateService`

- **Linked User Story:** US-001, US-002, US-003
- **Linked Architecture Component:** `features/search/search-state.service.ts`; DP-013, AD-006
- **Description:** Signal-based state holding last search criteria, results, loading flag, and error message. Exactly one Observable→Signal conversion point per data flow (via one-time `.subscribe()` from `FlightSearchService`, per AD-006's KISS convention). All downstream consumption (sorting, display) is Signals/`computed()` only — no `async` pipe mixed with signals for the same data flow (DP-013).
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-026
- **DoR Check:** Pass — architecture plan Section 4.3 gives the exact conversion-point pattern with code example.

### BL-028 — `SearchFormComponent`

- **Linked User Story:** US-001, US-008
- **Linked Architecture Component:** `features/search/search-form/search-form.component.ts`
- **Description:** Standalone component: origin/destination dropdowns (from BL-022's `AIRPORTS` constant, same-airport selection blocked per US-001 AC8/US-008 AC4), native date picker (ASM-010) blocking past dates (US-001 AC9), passenger count 1–9 selector, cabin class 3-option selector, submit disabled while invalid (US-001 AC5). On submit, delegates to `FlightSearchService`/`SearchStateService` (BL-026/027) — the component itself constructs no HTTP requests (DP-009). Loading indicator shown for request duration (FR-013).
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-022, BL-026, BL-027
- **DoR Check:** Pass — US-001 AC1–9 and US-008 AC1–4 give every form/validation rule.

### BL-029 — `ResultsListComponent`

- **Linked User Story:** US-002, US-004
- **Linked Architecture Component:** `features/results/results-list/results-list.component.ts`
- **Description:** Renders each flight result (provider, flight number, departure/arrival HH:mm, duration "Xh Ym", cabin class, total price prominently + per-passenger secondary, per FR-016–018) reading from `SearchStateService` (signal, no new HTTP call). Empty-state message on zero results (US-002 AC6); user-facing error message on failure without backend details (US-002 AC7); results persist until a new search (US-002 AC8). Each result has a "Select"/"Book" action navigating to `/booking` and populating `BookingStateService` (US-004 AC1/AC4) — no API call for that navigation.
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-027, BL-023, BL-030 (sort control feeds display order into this component)
- **DoR Check:** Pass — FR-016–019, US-002 AC1–8, US-004 AC1/AC4 give full display/behaviour rules.

### BL-030 — `SortControlComponent`

- **Linked User Story:** US-003
- **Linked Architecture Component:** `features/results/sort-control/sort-control.component.ts`
- **Description:** Sort control offering the 4 options (price low-high default, price high-low, duration shortest-first, departure earliest-first). Re-orders via `computed()` over the existing results signal — zero additional API calls (FR-021, US-003 AC2). Active option visually indicated (US-003 AC3); consistent full-set re-ordering (US-003 AC4).
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-027
- **DoR Check:** Pass — US-003 AC1–5/FR-020–024 give every sort rule and the default order.

### BL-031 — `BookingService` (Angular)

- **Linked User Story:** US-006
- **Linked Architecture Component:** `features/booking/booking.service.ts`; DP-010, DP-PROTOCOL-006
- **Description:** Sole place `HttpClient` is injected for booking. `createBooking(request: BookingRequest): Observable<BookingResponse>` calling `POST /api/bookings`. No component injects `HttpClient` directly.
- **Size:** XS
- **Priority:** Must Have
- **Dependencies:** BL-021
- **DoR Check:** Pass — architecture plan Section 4.2 gives the exact method signature and endpoint.

### BL-032 — `BookingStateService`

- **Linked User Story:** US-004, US-005, US-006
- **Linked Architecture Component:** `features/booking/booking-state.service.ts`; DP-013, AD-006
- **Description:** Signal-based state carrying the selected flight (populated from `ResultsListComponent` with no API call, per US-004 AC4), passenger form values, and the booking response after confirmation. Same one-conversion-point Observable→Signal rule as `SearchStateService` (BL-027).
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-031
- **DoR Check:** Pass — architecture plan Section 4.2/4.3; US-004 AC4 is the acceptance criterion for the no-API-call carry-over.

### BL-033 — `BookingFormComponent` — **DECOMPOSED (Phase 08)**

- **Status:** Superseded/Decomposed. Do not implement as a single item.
- **Decomposition rationale:** Flagged at Phase 07 as RISK-014 (single largest/most complex item, L-sized, bottleneck risk for the compressed one-day sprint). At Phase 08 (Parallel Delivery Plan), the SDLC Orchestrator confirmed the split should proceed. `BookingFormComponent` is decomposed into three M/S-sized implementation sub-tasks — **BL-036**, **BL-037**, **BL-038** — along natural seams in the original description: read-only summary/price display, per-passenger dynamic form-array orchestration + aggregate validity gating, and submit/loading/error/navigation/re-submission-guard wiring.
- **Architecture note:** This is a task-decomposition split for delivery-tracking and ownership purposes only — it does **not** change the architecture. All three sub-tasks still implement the single `features/booking/booking-form/booking-form.component.ts` component named in `docs/architecture/architecture-plan.md` Section 4.1 (no new component file/class is introduced, no Solution Architect approval was required or sought for this delivery-tracking split). The three sub-tasks compose into one component's template/class, built and integrated as sequential sections of the same file.
- **Original scope (US-004, US-005, US-006; FR-025–038) is fully preserved** — nothing is added, removed, or reduced. See BL-036/BL-037/BL-038 below for the redistributed description, sizing, dependencies, and DoR checks.
- **Superseded by:** BL-036, BL-037, BL-038 (Section 5, below BL-035).

### BL-034 — `PassengerFormSectionComponent`

- **Linked User Story:** US-005
- **Linked Architecture Component:** `features/booking/passenger-form-section/passenger-form-section.component.ts`
- **Description:** One instance per passenger, numbered clearly ("Passenger 1", "Passenger 2" — FR-028). Collects full name (min 2 chars, at least one letter — US-005 AC7/FR-064), email (standard format — US-005 AC8/FR-065), document number with label ("Passport Number"/"National ID") and validation pattern (BL-024) determined by route type resolved at booking-screen load and fixed for the session (US-005 AC5/AC6/BR-003). First passenger's email is also the primary contact (US-005 AC3/BR-005). Blocks submission on invalid/missing data (US-005 AC10).
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-024
- **DoR Check:** Pass — US-005 AC1–10 give the complete per-field specification.

### BL-035 — `ConfirmationComponent`

- **Linked User Story:** US-006
- **Linked Architecture Component:** `features/confirmation/confirmation/confirmation.component.ts`
- **Description:** Displays the booking reference prominently (US-006 AC5), the booked flight summary, total price, and full name of each passenger (US-006 AC4/FR-035), reading from `BookingStateService` (BL-032) with no new API call.
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-032
- **DoR Check:** Pass — US-006 AC4–5/FR-035–036 give the complete display specification.

### BL-036 — `BookingFormComponent`: Flight Summary & Price Breakdown Display *(split from BL-033, Phase 08)*

- **Linked User Story:** US-004
- **Linked Architecture Component:** `features/booking/booking-form/booking-form.component.ts` (read-only display section)
- **Description:** Displays the flight summary (route, provider, flight number, times, cabin class — US-004 AC2) and price breakdown (per-passenger price, passenger count, total via `pricing.util.ts` BL-023 — US-004 AC3), all read from `BookingStateService` with no new API call (US-004 AC4). Purely presentational — no form controls, no submit logic. This sub-task can be built and visually verified before the passenger form array (BL-037) or submit wiring (BL-038) exist, since it depends only on state already populated by `ResultsListComponent`'s "Select"/"Book" action (BL-029).
- **Size:** S
- **Priority:** Must Have
- **Dependencies:** BL-023, BL-032
- **DoR Check:** Pass — US-004 AC2–4/FR-025–027 give the complete display specification; this sub-task's scope boundary (read-only display, no form/submit logic) is clarified by this decomposition itself.

### BL-037 — `BookingFormComponent`: Passenger Form Array Orchestration *(split from BL-033, Phase 08)*

- **Linked User Story:** US-004, US-005
- **Linked Architecture Component:** `features/booking/booking-form/booking-form.component.ts` (passenger-section orchestration)
- **Description:** Renders one `PassengerFormSectionComponent` (BL-034) per passenger, sized from the selected flight's passenger count (FR-028). Aggregates validity across all rendered passenger sections and exposes a single "all passengers valid" signal that gates the "Confirm Booking" action (US-005 AC10/FR-031). Does not itself perform the HTTP submit — that is BL-038's responsibility. This is the most structurally complex of the three split sub-tasks (dynamic array of child form sections is the reason BL-033 was originally sized L) and is the one most worth completing before BL-038, since BL-038 needs its aggregate-validity output as an input.
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-032, BL-034
- **DoR Check:** Pass — FR-028/FR-031, US-005 AC10 give the complete orchestration/gating specification.

### BL-038 — `BookingFormComponent`: Submit Orchestration, Loading, Error, and Re-submission Guard *(split from BL-033, Phase 08)*

- **Linked User Story:** US-006
- **Linked Architecture Component:** `features/booking/booking-form/booking-form.component.ts` (submit-handler section)
- **Description:** On "Confirm Booking" click (enabled only once BL-037's aggregate-validity signal is true), assembles the `BookingRequest` from `BookingStateService` (selected flight + passenger form values from BL-037) and delegates to `BookingService` (Angular, BL-031), showing a loading state for the request duration (US-006 AC3). Navigates to `/confirmation` on success (FR-034), or shows a user-facing error without backend internals on failure (US-006 AC6/FR-037). Prevents re-submission without deliberate back-navigation (US-006 AC7/FR-038). This sub-task is the integration point of the other two — it should be built last among the three, once BL-036 (data to submit) and BL-037 (validity gate) exist.
- **Size:** M
- **Priority:** Must Have
- **Dependencies:** BL-031, BL-036, BL-037
- **DoR Check:** Pass — US-006 AC1, AC3, AC6–7/FR-032–038 give the complete submit/loading/error/re-submission specification.

---

## 6. Backlog Summary Table

| ID | Title | User Story | Size | Priority | Blocked By |
|---|---|---|---|---|---|
| BL-001 | Solution and Project Scaffolding | US-007 (enabling) | XS | Must | — |
| BL-002 | Domain Models | US-002, 005, 006, 007, 008 | S | Must | BL-001 |
| BL-003 | API Contract Models | US-001, 006 | S | Must | BL-002 |
| BL-004 | Airport Static Data (`AirportDataService`) | US-008 | XS | Must | BL-002 |
| BL-005 | `RouteTypeResolver` | US-005 | XS | Must | BL-004 |
| BL-006 | Document Validation Patterns | US-005 | XS | Must | BL-002 |
| BL-007 | `IFlightProvider` Interface | US-007 | XS | Must | BL-002, BL-003 |
| BL-008 | `GlobalAirProvider`/`BudgetWingsProvider` | US-002, 007 | M | Must | BL-007 |
| BL-009 | `IFlightAggregatorService` | US-007, 002 | M | Must | BL-007, BL-008 |
| BL-010 | `SearchRequestValidator` | US-001 | S | Must | BL-003, BL-004 |
| BL-011 | `IBookingStore`/`InMemoryBookingStore` | US-006 | S | Must | BL-002 |
| BL-012 | `ITenantContext`/`DefaultTenantContext` | US-006 (seam) | XS | Must | BL-001 |
| BL-013 | `BookingReferenceGenerator` | US-006 | S | Must | BL-001 |
| BL-014 | `BookingRequestValidator` | US-005, 006 | S | Must | BL-003, BL-005, BL-006 |
| BL-015 | `IBookingService`/`BookingService` | US-006 | M | Must | BL-005, BL-011, BL-012, BL-013, BL-014 |
| BL-016 | `ApiExceptionMiddleware` | All (cross-cutting) | XS | Must | BL-001 |
| BL-017 | `SearchController` | US-001 | XS | Must | BL-009, BL-010 |
| BL-018 | `BookingController` | US-006 | XS | Must | BL-015, BL-014 |
| BL-019 | DI Composition Root/CORS/Config | US-007 (+all) | S | Must | BL-008, 009, 011, 012, 013, 016, 017, 018 |
| BL-020 | Angular Workspace/Routing Shell | US-001, 004 (+all) | S | Must/Should | — |
| BL-021 | Shared Models (TS) | US-001, 002, 005, 006 | XS | Must | BL-003 |
| BL-022 | `airports.constants.ts` | US-008 | XS | Must | BL-020 |
| BL-023 | `pricing.util.ts` | US-002, 004 | XS | Must | BL-021 |
| BL-024 | `document-number.validators.ts` | US-005 | XS | Must | BL-006 |
| BL-025 | `AuthService` (no-op) | (seam) | XS | Must | BL-020 |
| BL-026 | `FlightSearchService` (Angular) | US-001 | XS | Must | BL-021 |
| BL-027 | `SearchStateService` | US-001, 002, 003 | S | Must | BL-026 |
| BL-028 | `SearchFormComponent` | US-001, 008 | M | Must | BL-022, BL-026, BL-027 |
| BL-029 | `ResultsListComponent` | US-002, 004 | M | Must | BL-027, BL-023, BL-030 |
| BL-030 | `SortControlComponent` | US-003 | S | Must | BL-027 |
| BL-031 | `BookingService` (Angular) | US-006 | XS | Must | BL-021 |
| BL-032 | `BookingStateService` | US-004, 005, 006 | S | Must | BL-031 |
| BL-033 | ~~`BookingFormComponent`~~ — **Decomposed (Phase 08)**, see BL-036/037/038 | US-004, 005, 006 | ~~L~~ | Must | Superseded — not implemented as a single item |
| BL-034 | `PassengerFormSectionComponent` | US-005 | M | Must | BL-024 |
| BL-035 | `ConfirmationComponent` | US-006 | S | Must | BL-032 |
| BL-036 | `BookingFormComponent`: Summary & Price Display *(split of BL-033)* | US-004 | S | Must | BL-023, BL-032 |
| BL-037 | `BookingFormComponent`: Passenger Form Array Orchestration *(split of BL-033)* | US-004, 005 | M | Must | BL-032, BL-034 |
| BL-038 | `BookingFormComponent`: Submit/Loading/Error/Re-submission *(split of BL-033)* | US-006 | M | Must | BL-031, BL-036, BL-037 |

**Total: 37 active backlog items** (19 backend, 18 frontend). BL-033 is decomposed/superseded by BL-036–BL-038 (Phase 08) and is not counted as a separate active item; the original 35-item count (Phase 07) is preserved in history via BL-033's decomposition note above — no scope was added or removed, only re-sequenced into smaller units.

---

## 7. User Story Coverage Map

| User Story | Backlog Items |
|---|---|
| US-001 — Search for Available Flights | BL-003, BL-004, BL-010, BL-017, BL-020, BL-021, BL-022, BL-026, BL-027, BL-028 |
| US-002 — View Flight Search Results | BL-002, BL-008, BL-009, BL-023, BL-027, BL-029 |
| US-003 — Sort Flight Results | BL-027, BL-030 |
| US-004 — Select a Flight and Initiate Booking | BL-020, BL-023, BL-029, BL-032, BL-036, BL-037 |
| US-005 — Enter Passenger Details | BL-002, BL-005, BL-006, BL-014, BL-024, BL-034, BL-037 |
| US-006 — Confirm Booking and Receive Reference | BL-002, BL-011, BL-012, BL-013, BL-014, BL-015, BL-018, BL-031, BL-032, BL-035, BL-038 |
| US-007 — Provider Extensibility | BL-001, BL-007, BL-008, BL-009, BL-019 |
| US-008 — View Airport Selection | BL-004, BL-022, BL-028 |

Every user story maps to at least 2 backlog items; every backlog item maps to at least one user story or an explicitly named cross-cutting seam (BL-012, BL-016, BL-019, BL-025) required by an approved DP-* constraint. No orphaned items in either direction.

*(Updated at Phase 08 to reflect the BL-033 → BL-036/BL-037/BL-038 decomposition; total US-* coverage is unchanged since the split redistributes, not removes, BL-033's original scope.)*

---

## 8. Sequencing Notes (Direct Blockers Only)

This section restates the "Blocked By" column of Section 6 as a narrative for Phase 08's benefit — it is not the full parallel-track plan.

**Backend spine:** BL-001 → BL-002 → (BL-003, BL-004, BL-006, BL-011, BL-012, BL-013 can proceed in parallel) → BL-007 → BL-008 → BL-009 → BL-017 (search side complete); and BL-005 → BL-014 → BL-015 → BL-018 (booking side complete) → BL-019 (composition root, needs everything wired).

**Frontend spine:** BL-020 → (BL-021, BL-022, BL-025 in parallel) → BL-023, BL-024 → BL-026 → BL-027 → BL-028/BL-030 → BL-029; and BL-031 → BL-032 → BL-034 → (BL-036, BL-037 — both consume BL-032/BL-034 outputs and can proceed in parallel once BL-032/BL-034 are done) → BL-038 (needs BL-036 and BL-037 both complete) → BL-035 (only needs BL-032, can proceed any time after BL-032 regardless of BL-036/037/038 progress).

*(Updated at Phase 08 — see `docs/delivery/parallel-delivery-plan.md` v1.0 for the full BL-033 decomposition rationale and the complete parallel-track graph across all 37 active items.)*

**Backend/frontend integration point:** Frontend services (BL-026, BL-031) can be built and unit-tested against contract models (BL-021) before the backend controllers (BL-017, BL-018) are live, per DEP-021 (dependency register) — but end-to-end integration testing (Phase 13–14) requires both sides complete, consistent with DEP-012.

**Cross-cutting, no hard blocker beyond BL-001/BL-020:** BL-016 (exception middleware), BL-025 (no-op AuthService) can be built at any time after workspace scaffolding.

Full parallel-track assignment (which items can run concurrently across which agents/roles) is deferred to Phase 08 — Parallel Delivery Plan, per this phase's scope boundary.

---

## 9. Definition of Ready — Confirmation Summary

Per `.claude/rules/definition-of-ready.md`, every item above was checked against:

- Business value clear — inherited from the linked US-* (all Must Have, all already Human PO-approved in `docs/requirements.md` v1.4).
- User story/requirement documented — yes, cited inline per item.
- Acceptance criteria documented — yes, traced to specific US-* AC numbers and/or FR-*/BR-*/DP-* IDs per item.
- Scope boundaries clear — yes, each item names exactly one architecture-plan component/class/file.
- Dependencies identified — yes, "Dependencies" field per item and Section 6/8.
- Risks identified — carried at the register level (Section 10 below); no new item-level risk was found requiring a distinct entry beyond what is already tracked.
- Required architecture guidance exists — yes, 100% of items cite a Section 2–5 architecture-plan location.
- Required API/UI/test specs exist — API contracts: architecture plan Section 5. Test approach: `docs/testing/test-strategy.md` v1.0 Sections 1–6 (unit/integration/component/E2E levels and traceability matrix already exist; item-level test-case authoring is Phase 13 scope). UI flow: architecture plan Section 4.
- NFR impact understood — carried at the NFR-spec level (`docs/specs/non-functional-requirements.md` v1.0); no item introduces a new NFR concern beyond what is already governed.
- Human Product Owner has no blocking questions — no new question was raised by this decomposition; all open questions in requirements.md Section 8 are already Resolved.

**Result: all 37 active items are Ready.** No item is Blocked or Conditionally Ready. (BL-033 is superseded/decomposed, not an active item requiring its own DoR status; its three replacements — BL-036, BL-037, BL-038 — were each individually DoR-checked at Phase 08 and are Ready.)

---

## 10. Out-of-Scope Confirmation

Every backlog item in Sections 4–5 was checked against `docs/requirements.md` v1.4 Section 7 (Out of Scope for MVP, items 1–33). Confirmed:

- No item implements authentication/authorisation enforcement (item 1, 23) — BL-016/BL-019's policy stub is a zero-runtime-cost pattern demonstration only, never applied via `[Authorize]`; BL-025's `AuthService` is a no-op.
- No item implements booking retrieval/cancellation, payment processing, seat selection, ancillary products, loyalty integration, or email confirmation (items 3–6, 8–11).
- No item implements real airline provider API integration or a live airport search API (items 6–7) — BL-008 providers are hardcoded mocks; BL-004/BL-022 are static constants.
- No item implements return/round-trip or multi-city itineraries (items 12–13) — BL-003's `SearchRequest` carries `tripType` fixed to `OneWay` only, per BR-013.
- No item implements cloud deployment, real database persistence, an admin portal, price alerts, i18n, mobile app, multi-tenant operation, GraphQL/gRPC/WebSocket endpoints, HTTP/3, dependency-upgrade tooling, a policy engine, zero-trust infrastructure, CI/CD pipeline artefacts, or OIDC/OAuth2/SAML/SSO integration (items 14–22, 24–33) — BL-011/BL-012 build only the approved persistence/tenancy *seams* (interfaces + trivial default implementations), consistent with DP-PERSIST-*/DP-TENANT-*/DP-CLOUD-*/DP-DEPLOY-*/DP-AUTH-* explicitly requiring the seam to exist without requiring the excluded capability itself.
- No item implements flight schedule filtering by date/route (item 17) — BL-008 providers return a fixed schedule per ASM-006.

**No backlog item introduces scope beyond the approved requirements baseline.**

---

## 11. Reference Documents

- `docs/requirements.md` (v1.4, Approved)
- `docs/specs/non-functional-requirements.md` (v1.0, Approved)
- `docs/testing/test-strategy.md` (v1.0)
- `docs/architecture/architecture-plan.md` (v1.0)
- `docs/delivery/risk-register.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/task-board.md`
- `.claude/rules/definition-of-ready.md`

---

## 12. Backlog Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial backlog created for Phase 07 — 35 items decomposed from 8 approved user stories, mapped to architecture-plan components |
| 2026-07-03 | project-coordinator | Phase 08 — per SDLC Orchestrator decision on HO-007 Open Question 1 (RISK-014), decomposed BL-033 (`BookingFormComponent`, L-sized) into BL-036 (Summary & Price Display, S), BL-037 (Passenger Form Array Orchestration, M), and BL-038 (Submit/Loading/Error/Re-submission, M). No scope, priority, or architecture change — task-decomposition only. Active item count: 37 (19 backend, 18 frontend). See `docs/delivery/parallel-delivery-plan.md` v1.0 for the delivery-track assignment and full rationale. |
| 2026-07-07 | project-coordinator | Phase 21 — Delivery Tracking Update: added Section 13 (Delivery Status). All 37 active items confirmed Done; PO-directed out-of-band deliverables and the approved challenge-PDF deviation (DEC-015) recorded. No backlog content rewritten. |

---

## 13. Delivery Status — Phase 21 Reconciliation (2026-07-07)

**All 37 active backlog items (BL-001–BL-019 backend; BL-020–BL-032, BL-034–BL-038 frontend) are Done** per full Definition of Done, not just Phase 12 implementation-complete: implemented (HO-012A/B), tested (Phases 13–14), all four numbered review loops closed to zero `Open` (Phases 15–18), QA findings consolidated (Phase 19), and independently re-verified fresh at Phase 20 — 365/365 tests passing on `main` @ `f4ae3da` (backend 172, frontend 181, E2E 12; `docs/testing/execution/phase-20-retest-summary.md`, HO-038, QA verdict: GO).

**PO-directed out-of-band deliverables** (delivered outside the original 37-item backlog, under explicit PO direction 2026-07-07; merged to `main`):

| ID | Deliverable | Evidence |
|---|---|---|
| OOB-01 | Backend route filtering (ASM-006 revised, requirements v1.5) | HO-032 |
| OOB-02 | Booking passenger-flow finalization — single-button in-place add; search passenger field removed | HO-032, HO-034 |
| OOB-03 | Production UI overhaul v2 — top nav with placeholder menu + Sign-in, journey progress strip, full-bleed hero, flight-card timeline layout, multi-column footer | HO-034 |
| OOB-04 | SDLC process hardening — `.claude/rules/ui-ux-quality-gates.md` + DoR/DoD alignment (retrospective `docs/delivery/retrospective-ui-quality-gap-2026-07-07.md`), autopilot efficiency review with canonical 01–24 phase model (`docs/delivery/autopilot-efficiency-review-2026-07-07.md`) | HO-033, HO-035 |

**Deliberate deviation from the challenge PDF (PO decision, 2026-07-07 — DEC-015):** passenger count is captured at booking (one-passenger-at-a-time in-place add), not on the search form; `SearchRequest.passengerCount` always submits `1`. This supersedes the search passenger-count selector originally specified in BL-028/US-001. Documented in README; awaiting no further action — approved.

**Residue (not backlog scope, tracked in `risk-register.md` RISK-016–RISK-019):** nested duplicate folder deletion approval, pending push to origin, 6 Low advisory findings in the ad-hoc booking-UI review, unmeasured NFR-TEST-005 coverage percentage.

---

*End of Project Backlog v1.2.*
