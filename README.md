# SkyRoute Travel Platform

SkyRoute is a flight search and booking module built for the arrivia Senior Full-Stack Developer hiring challenge. It aggregates flight results from two mock airline providers — **GlobalAir** and **BudgetWings** — behind a single search API, filtering each provider's fixed schedule to the requested route, and lets a user carry a selected flight through a multi-passenger booking flow to a confirmation screen. The search form captures origin, destination, departure date, and cabin class; passengers are then added one at a time on the booking screen (up to 9) — each providing a full name, age, email address, and document number — with every saved passenger appearing as an editable summary card before the booking is confirmed and a reference code is returned. The backend is ASP.NET Core (.NET 10); the frontend is Angular (v22, standalone components, signal-based state).

---

## 1. Setup and Run Instructions

### 1.1 Prerequisites

| Tool | Version used by this repo | Notes |
|---|---|---|
| .NET SDK | **10.0** | All five backend projects — the three libraries (`SkyRoute.Api`, `SkyRoute.Application`, `SkyRoute.Infrastructure`) plus both test projects (`SkyRoute.Application.Tests`, `SkyRoute.Api.IntegrationTests`) — target `net10.0`. |
| Node.js | Compatible with Angular CLI 22 (`^22.22.3 \|\| ^24.15.0 \|\| >=26.0.0`, per `@angular/cli@22.0.5`'s published `engines` field) | Check with `node --version`. |
| npm | **11.17.0** (pinned via `src/UI/package.json`'s `packageManager` field) | Other npm 11.x releases will likely work, but this is the version the project was built and tested with. |

The backend and frontend are two independent projects with no shared package manager — install/run each separately as shown below.

### 1.2 Backend (ASP.NET Core API)

From the repository root:

```bash
dotnet restore SkyRoute.slnx
dotnet build SkyRoute.slnx
dotnet run --project src/Service/SkyRoute.Api
```

This starts the API on **`http://localhost:5094`** (see `src/Service/SkyRoute.Api/Properties/launchSettings.json`; the `https` profile also exposes `https://localhost:7252`). CORS is restricted to `http://localhost:4200` (`src/Service/SkyRoute.Api/appsettings.json`), so the frontend must be served from exactly that origin for requests to succeed. Business endpoints are versioned under `/api/v1/` (e.g. `POST /api/v1/search`, `POST /api/v1/bookings`); the service also exposes `GET /api/health` (health checks) and, in Development, OpenAPI 3.1 docs at `/api/openapi/v1.json` with an interactive Scalar reference at `/api/scalar/v1`.

### 1.3 Frontend (Angular)

From the `src/UI/` directory:

```bash
npm install
npm start
```

`npm start` runs `ng serve`, which starts the Angular dev server on **`http://localhost:4200`**. The frontend’s API base URL is configured as `http://localhost:5094/api/v1` (URL-segment API versioning, v1 default) in `src/UI/src/environments/environment.ts` — start the backend first (or at least before submitting a search), on that exact port.

Once both are running, open `http://localhost:4200` in a browser.

### 1.4 Running the Test Suites

**Backend (xUnit, via `dotnet test`):**

```bash
dotnet test SkyRoute.slnx
```

Covers `src/Service/tests/SkyRoute.Application.Tests` (service/domain-logic unit tests) and `src/Service/tests/SkyRoute.Api.IntegrationTests`.

**Frontend unit/component tests (Vitest, via Angular's test builder):**

```bash
cd src/UI
npm test
```

`npm test` runs `ng test`, which uses `@angular/build:unit-test` with the `vitest` runner (see `src/UI/angular.json`). For a single non-interactive run (e.g. in CI), use:

```bash
npm test -- --watch=false
```

**End-to-end tests (Playwright):**

E2E tests exercise a real backend and a real Angular dev server together; `src/UI/playwright.config.ts` deliberately does **not** auto-start either server (`baseURL: 'http://localhost:4200'` assumes both are already running). One-time browser install:

```bash
cd src/UI
npx playwright install chromium
```

Then, with the backend running (`dotnet run --project src/Service/SkyRoute.Api`, or `dotnet run --no-build --launch-profile http` if already built) and the frontend running (`npm start`) in separate terminals/processes:

```bash
cd src/UI
npx playwright test
```

(The `e2e` script in `package.json` — `npm run e2e` — is equivalent to `npx playwright test`.)

---

## 2. Architecture Decisions

The full rationale lives in `docs/architecture/architecture-plan.md`; the key structural decisions are summarized here.

- **3-project .NET solution split**: `SkyRoute.Api` (ASP.NET Core `Sdk.Web` — the only project referencing MVC/HTTP types; thin controllers plus the composition root in `Program.cs`), `SkyRoute.Application` (plain class library — domain models, contracts, interfaces, business-logic services, validators; zero ASP.NET Core reference, which structurally guarantees the API layer can't be bypassed), and `SkyRoute.Infrastructure` (concrete adapters: the two mock providers, the in-memory booking store, the default tenant context). Dependencies flow one way: `Api → Application`, `Infrastructure → Application`; `Infrastructure` is never referenced outside `Program.cs`'s DI wiring.
- **Provider abstraction**: `IFlightProvider` (`ProviderName` + `SearchAsync(SearchRequest, CancellationToken)`) is implemented independently by `GlobalAirProvider` and `BudgetWingsProvider`. `FlightAggregatorService` fans out to every registered `IFlightProvider` via `Task.WhenAll`, with each provider call wrapped in its own try/catch *before* joining the batch — so one provider throwing doesn't take down the aggregate search (it's simply treated as returning zero results for that provider). Adding a third provider is a new class plus one DI registration line, with no change to the aggregator.
- **Shared pricing/mapping pipeline**: both providers keep their own named pricing method (`ApplyGlobalAirPricing`, `ApplyBudgetWingsPricing`) so pricing rules stay independently unit-testable per provider, but the surrounding mapping mechanics — route filtering (case-insensitive origin/destination match against the provider's fixed schedule), cabin-class fare multiplier lookup, base-fare rounding, UTC departure/arrival timestamp construction, and building the `FlightResult` — are factored into a single shared `ProviderScheduleMapper.BuildResults()` helper (`src/Service/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs`) used by both providers, avoiding duplicating that pipeline twice.
- **Booking flow**: `BookingRequest` carries a full flight-detail snapshot (provider, flight number, route, times, cabin class, price) rather than an opaque flight ID, since the mock providers have no independent lookup store to match against. On submission, `BookingService` re-resolves the route type and re-derives the true fare server-side from the same provider schedule the original search used, rather than trusting the client-submitted price outright, and rejects the booking if the two disagree. Booking records are held in an in-memory `ConcurrentDictionary`-backed store (`InMemoryBookingStore`) behind an `IBookingStore` interface.
- **Passenger entry on the booking screen**: passenger details (full name, age, email, document number) are collected one passenger at a time through a single in-place form rather than rendering all forms at once. Age is a required whole number from 0 to 120, captured as data only — no pricing, eligibility, or other business logic is bound to it (PO decision 2026-07-08) — validated in the Angular form and authoritatively re-validated by the backend. "Add another passenger" validates the current form, saves the passenger as a compact summary card (with Edit and Remove actions and positional renumbering), and clears the same form for the next passenger, up to a cap of 9; "Confirm Booking" saves a filled form (if any) and submits every saved passenger. The document field's label and validation switch together by route type — "Passport Number" for international routes, "National ID" for domestic. A live price breakdown shows per-passenger price × the number of passengers added (minimum 1), with the total as the dominant figure; server-side `passengers[i].*` validation errors are mapped back onto the offending passenger card, which reopens in edit mode. Focus is managed explicitly on every state transition (buttons use `aria-disabled` plus click guards, never the native `disabled` attribute, so keyboard focus never drops to `<body>`), state changes are announced through a persistent polite live region, and both a `canDeactivate` router guard and a `beforeunload` listener confirm before unconfirmed passenger data is destroyed. `BookingRequest.passengerCount` is always `passengers.length`.
- **Frontend structure**: Angular 22 standalone components (no `NgModule`), organized by feature (`search/`, `results/`, `booking/`, `confirmation/`). `HttpClient` is only ever injected inside a feature's own service (`FlightSearchService`, `BookingService`) — never directly inside a component. State downstream of that single HTTP boundary is signal-based: each feature has a small state service (`SearchStateService`, `BookingStateService`) holding `signal()` state, with a single Observable→Signal conversion point per data flow and no mixing of RxJS and Signals for the same piece of state. Routing between `/search`, `/results`, `/booking`, and `/confirmation` reads from these state services rather than re-fetching data.
- **Validation**: field-level rules use `System.ComponentModel.DataAnnotations` (already part of the BCL — no new dependency); cross-field/context-dependent rules (e.g. passenger count matching the passengers array length, document format depending on domestic/international route type) live in dedicated `SearchRequestValidator`/`BookingRequestValidator` classes.
- **Auth and tenancy seams, deliberately inert in MVP**: a no-op Angular `AuthService`, a backend named authorization-policy stub that's registered but never applied to any endpoint, and an `ITenantContext` interface with a single `DefaultTenantContext` implementation returning a constant tenant ID. These exist so a future auth/multi-tenant implementation can slot in without changing existing service or controller code, without any of that behavior actually being exercised in this MVP.

---

## 3. Trade-offs and Known Limitations

These are honest, current-state limitations of the MVP as delivered for this challenge — not aspirational or hidden gaps:

- **No persistent database.** Bookings are held in an in-memory `ConcurrentDictionary` (`InMemoryBookingStore`). All booking data is lost when the API process restarts. The `IBookingStore` interface is designed so a real database-backed implementation could be substituted without touching `BookingService` or any controller, but no such implementation exists in this MVP.
- **No authentication or authorization.** Every endpoint is publicly accessible; there is no login, no user account, and no per-user booking ownership check. The frontend's `AuthService` and the backend's named authorization-policy stub are inert placeholders for a future implementation, not functioning security controls.
- **Mock provider data is in-process and deterministic, not a real airline API.** Each provider (`GlobalAirProvider`, `BudgetWingsProvider`) owns 4 hardcoded fixture flights plus deterministically generated flights (`RouteScheduleGenerator`) for every ordered pair of the 6 supported airports its fixtures don't cover — so **any valid route returns results** (fixture routes return exactly their documented flights; generated routes get 2 flights per provider with duration-derived fares, fed through the same pricing rules and cabin multipliers). One deliberate exception: MAN↔SYD intentionally has no direct service (DEC-021 — true to the real world) so the styled empty state stays reachable through a real search. A search returns only the schedule entries whose origin **and** destination match the requested route (case-insensitive exact match, applied in the shared `ProviderScheduleMapper.BuildResults()`), and identical searches always return identical flights (generation is a pure function of the route — no randomness, no clock reads). The requested departure date still does **not** filter the data: only the calendar-date component of the returned departure/arrival timestamps reflects it, and only the requested cabin class affects pricing (via the per-cabin fare multiplier). A real provider integration would additionally filter by date and would call an external service rather than an in-process schedule. (Route filtering is the v1.5 revision of the original fixed-schedule assumption — see `docs/requirements.md` ASM-006; full route coverage is the 2026-07-07 PO defect fix — see `docs/features/feature-provider-aggregation.md` Section 3.3.)
- **Passenger count is captured on the booking screen, not the search form.** Per a Product Owner UX decision (2026-07-07), the search form has no passenger-count field; every search request is sent with `passengerCount: 1` (the API contract still validates 1–9), so result prices effectively show the per-passenger fare. The number of passengers is then decided during booking by adding passengers one at a time (up to 9), and the booking screen's live price breakdown reflects the actual count entered.
- **One-way, single-leg search only.** No round-trip or multi-city itineraries.
- **Single-tenant.** The multi-tenancy seam (`ITenantContext`) exists structurally but only one tenant (`"default"`) is ever used.
- **No payment processing, email confirmations, or booking retrieval/cancellation UI.** Booking ends at an on-screen confirmation with a generated reference; nothing is sent externally and there is no "manage my booking" flow.
- **Accessibility review was performed via static code inspection only.** No live browser, screen reader, or automated tooling (e.g. axe-core, Lighthouse) was available in the review environment; findings (see `docs/reviews/accessibility-review-phase-17.md`) were derived by reading component templates/TypeScript/CSS directly, including manually computing WCAG colour-contrast ratios from the CSS hex values in use. All findings raised in that review have since been fixed and independently re-verified against the current source, but the review's underlying method remains static inspection rather than live assistive-technology testing.
- **English only, web only.** No internationalization and no native mobile app (the Angular UI is responsive but not a packaged mobile client).
- **No frontend linter and no CI pipeline (known deferred gap).** The frontend relies on the TypeScript compiler and Prettier (formatting only); there is no ESLint config, no `lint` npm script, and no `.github/workflows/` continuous-integration pipeline, so nothing runs automatically on push. This is a deliberate deferral for the MVP, not an oversight — adding tooling/dependencies is gated on approval. A production setup would add: ESLint (e.g. `@angular-eslint`) with a `lint` script, plus a CI workflow running build + test + lint on push.

---

## 4. Project Structure Reference

```text
src/
├── Service/                            .NET solution root (all backend projects)
│   ├── SkyRoute.Api/                   ASP.NET Core Web API — controllers, DI composition root, middleware
│   ├── SkyRoute.Application/           Domain models, contracts, service interfaces, business logic, validators
│   ├── SkyRoute.Infrastructure/        Mock providers, in-memory booking store, tenant context
│   └── tests/
│       ├── SkyRoute.Application.Tests/     Service/domain-logic unit tests (xUnit)
│       └── SkyRoute.Api.IntegrationTests/  API integration tests (xUnit)
└── UI/                                 Angular frontend (standalone components, signal-based state)
    ├── src/app/
    │   ├── core/                       Cross-cutting services (auth no-op, route focus management)
    │   ├── shared/                     Constants, models, pricing/validation utilities shared across features
    │   └── features/                   search/, results/, booking/, confirmation/
    └── e2e/                            Playwright end-to-end specs

docs/                                   Full SDLC artifact trail: requirements, architecture, specs, testing, reviews, delivery, handoffs
```

Deeper detail on requirements, NFRs, architecture, and review history is tracked under `docs/` (see `docs/requirements.md`, `docs/architecture/architecture-plan.md`, `docs/specs/`, `docs/reviews/`, and `docs/delivery/risk-register.md`).
