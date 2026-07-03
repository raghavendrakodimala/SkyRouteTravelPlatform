# Feature Specification — Provider Aggregation and Mock Provider Data

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | FEAT-PA-001 |
| Version | 1.0 |
| Date | 2026-07-03 |
| Status | Draft — Ready for Phase 11 Spec Readiness Check |
| Owner | solution-architect |
| Source | `docs/requirements.md` v1.4 (US-007, FR-046–053, BR-001, BR-002, BR-007, BR-009, ASM-006, ASM-007), `docs/architecture/architecture-plan.md` v1.0 (Section 3.1–3.2), `docs/testing/test-strategy.md` v1.0 (Section 3, 6) |
| Phase | Phase 10 — Feature Specifications |
| User Story | US-007 (Provider Extensibility) |
| Backlog Items Made Implementation-Ready | BL-001, BL-007, BL-008, BL-009, BL-019 (supporting: BL-002 domain model, BL-004 airport data) |

### Purpose and Scope

This document is the last concretization layer before Phase 12 for the `IFlightProvider` contract, the two concrete mock providers (`GlobalAirProvider`, `BudgetWingsProvider`), and the aggregation/fault-isolation behavior. It does not reopen or contradict `docs/requirements.md` v1.4 or `docs/architecture/architecture-plan.md` v1.0. Because ASM-006 explicitly leaves the *specific* mock flight data undefined ("a realistic, hardcoded set of flights" — FR-048), this document supplies a concrete, ready-to-implement dataset as **Gap-fill decisions**, summarized in Section 6, so Phase 12 does not have to invent it and so Phase 13 test authors have a stable, documented fixture to write assertions against (consistent with `docs/testing/test-strategy.md` Section 3's expectation of "a known, fixed set of flights").

---

## 1. `IFlightProvider` Contract

Verbatim from `docs/architecture/architecture-plan.md` Section 3.1 — restated here as the authoritative implementation target, not a new design:

```csharp
public interface IFlightProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
```

- `ProviderName` is a literal, constant string per implementation: exactly `"GlobalAir"` for `GlobalAirProvider` and exactly `"BudgetWings"` for `BudgetWingsProvider` (**Gap-fill PA-04**) — these are the exact strings that must appear in the `provider` field of every `FlightResult` (FR-052) and that test doubles/stubs should use when simulating a named provider (per `docs/testing/test-strategy.md` Section 6).
- `SearchAsync` receives the full `SearchRequest` (origin, destination, departureDate, passengerCount, cabinClass, tripType) but — per ASM-006 (see Section 2 below) — uses only `cabinClass` (for fare-class selection, BR-009) and `departureDate` (for the *date component* of the returned datetimes, see Section 4) as actual inputs to its output. `origin`, `destination`, and `passengerCount` do not filter or alter which flights are returned; `passengerCount` is not used by the provider at all (per-passenger price is independent of how many passengers are booked — see BR-001/BR-002).
- Adding a third provider requires only a new class implementing this interface plus one new DI registration line (`AddScoped<IFlightProvider, ThirdProvider>()`) — no change to `FlightAggregatorService` or this interface (FR-053, US-007 AC2).

---

## 2. Fixed-Schedule Behavior — Restating ASM-006 (Important, Not New)

Per `docs/requirements.md` ASM-006 and `docs/architecture/architecture-plan.md` Section 3.1: **the same flight schedule is returned by each provider for any valid search input.** Origin/destination/date do not filter which flights come back — only `cabinClass` affects the returned fare (BR-009). This means, in practice, a search for `LHR → JFK` will receive results whose own `origin`/`destination` fields may show routes such as `MAN → LHR` or `SYD → LAX` — this is the **approved, existing** MVP simplification (ASM-006, Out of Scope item 17), not a defect and not something this document is introducing or correcting. Phase 12 implementers, Phase 13 test authors, and Phase 17 UX/accessibility reviewers should treat this as expected behavior, not a bug to file.

---

## 3. Fixed Mock Flight Dataset (**Gap-fill PA-01**)

Each provider returns exactly **4 flights** per search (regardless of requested route/date), each available in all 3 cabin classes (see Section 4 for per-class fare derivation). This gives 8 total raw flights across both providers per search before any provider fails (BR-007 scenarios reduce this to 4). All routes are drawn from the approved airport list (`docs/requirements.md` Section 3.7 / `airports.constants.ts`), mixing domestic and international pairs so the fixed dataset remains realistic and exercises both route types regardless of what a tester actually searches.

### 3.1 GlobalAir — Fixed Schedule

| Flight Number | Origin | Destination | Departure (local time-of-day) | Duration (minutes) | Duration (display) | Economy Base Fare |
|---|---|---|---|---|---|---|
| GA101 | LHR | JFK | 09:00 | 510 | 8h 30m | $250.00 |
| GA204 | LHR | DXB | 22:00 | 450 | 7h 30m | $300.00 |
| GA309 | JFK | LAX | 07:15 | 330 | 5h 30m | $180.00 |
| GA412 | MAN | LHR | 06:45 | 70 | 1h 10m | $80.00 |

### 3.2 BudgetWings — Fixed Schedule

| Flight Number | Origin | Destination | Departure (local time-of-day) | Duration (minutes) | Duration (display) | Economy Base Fare |
|---|---|---|---|---|---|---|
| BW210 | LHR | JFK | 11:00 | 495 | 8h 15m | $220.00 |
| BW225 | SYD | LAX | 23:00 | 780 | 13h 0m | $450.00 |
| BW238 | LAX | JFK | 06:00 | 300 | 5h 0m | $150.00 |
| BW241 | MAN | LHR | 14:00 | 65 | 1h 5m | $60.00 |

Each provider's dataset is a private, hardcoded, static list within its own class (`SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs`, `BudgetWingsProvider.cs`), consistent with named-constant/no-magic-numbers guidance (NFR-MAINT-002). Values above are the authoritative fixture — Phase 12 must not substitute a different dataset without recording a new decision, since Phase 13's test scenarios (per `docs/testing/test-strategy.md` Section 3) will assert against this fixed content.

---

## 4. Cabin Class Fare Derivation (**Gap-fill PA-02**)

BR-009 requires providers to "return flights for the requested cabin class." Rather than maintaining three separate hardcoded fare tables (introducing duplication risk, NFR-MAINT-002), each flight's Economy base fare (Section 3) is multiplied by a fixed, named class multiplier **before** the provider's own pricing rule (BR-001/BR-002) is applied:

| Cabin Class | Multiplier (applied to Economy base fare) |
|---|---|
| Economy | × 1.0 |
| Business | × 2.0 |
| First Class | × 3.5 |

This guarantees every one of the 4 fixed flights is always available in every requested cabin class (no artificial "no flights in this class" gap), and produces deterministic, testable numbers. Multipliers must be named constants (e.g., `CabinClassMultipliers.Business = 2.0m`), not inline magic numbers (DP-006, NFR-MAINT-002).

### 4.1 Worked Pricing Examples (BR-001, BR-002 — restated formulas, applied to this dataset's numbers)

**GlobalAir (BR-001: `finalPrice = round(baseFare × 1.15, 2)`):**

- GA101, Economy: `$250.00 × 1.0 = $250.00` base → `$250.00 × 1.15 = $287.50` per-passenger price.
- GA101, Business: `$250.00 × 2.0 = $500.00` base → `$500.00 × 1.15 = $575.00` per-passenger price.
- GA101, First Class: `$250.00 × 3.5 = $875.00` base → `$875.00 × 1.15 = $1,006.25` per-passenger price.
- (Generic BR-001 rounding example, unchanged from `docs/requirements.md`: base fare $87.50 → $100.63.)

**BudgetWings (BR-002: `finalPrice = round(max(baseFare × 0.90, 29.99), 2)`, discount applied to base fare only, round-then-floor per `docs/architecture/architecture-plan.md` Section 3.1 note):**

- BW241, Economy: `$60.00 × 1.0 = $60.00` base → `$60.00 × 0.90 = $54.00` → `max($54.00, $29.99) = $54.00` per-passenger price.
- BW238, Economy: `$150.00 × 1.0 = $150.00` base → `$150.00 × 0.90 = $135.00` → `max($135.00, $29.99) = $135.00` per-passenger price.
- (Generic BR-002 floor examples, unchanged from `docs/requirements.md`: base fare $25.00 → $29.99 floor; base fare $30.00 → $27.00 → $29.99 floor.)

Each pricing method (`ApplyGlobalAirPricing`, `ApplyBudgetWingsPricing`) is a named, isolated method taking only a `decimal baseFare` and returning a `decimal` — independently unit-testable per DP-019/NFR-TEST-003 without any dependency on this fixed dataset. The worked examples above are illustrative fixture data for Phase 13's integration-level tests, not the unit-test inputs (which use hand-picked boundary values per `docs/testing/test-strategy.md` Section 3).

---

## 5. Datetime Construction (**Gap-fill PA-03**)

Each fixed flight's schedule (Section 3) stores only a **local time-of-day** and a **duration** — not a full fixed calendar date. At response-construction time, the provider combines:

- the fixed time-of-day (e.g., GA101's `09:00`), with
- the **date the user actually searched for** (`SearchRequest.departureDate`, e.g., `2026-08-01`),

to produce `departureDateTime` (e.g., `"2026-08-01T09:00:00Z"`), and adds `durationMinutes` to derive `arrivalDateTime`.

This does **not** contradict ASM-006: the *set of flights returned* and their *time-of-day* are still entirely fixed and independent of the requested date/route (nothing about which flights appear, or at what hour, changes based on input) — only the *calendar date* portion of the two timestamp fields reflects what the user searched for. Rationale: without this, every response would show a fixed, arbitrary, likely-past hardcoded date, which would look broken in the UI (e.g., a "2026-01-01" departure date shown for a search made for "2026-09-15") — an outcome ASM-006 never intended to require and that no FR/BR mandates. Anchoring the date to the request while keeping the schedule itself fixed is the smallest change that keeps both the approved fixed-schedule behavior and a sane-looking UI.

`arrivalDateTime` may fall on the following calendar day if `departureTime + durationMinutes` crosses midnight (e.g., BW225: departs `23:00`, duration `780` minutes/13h, arrives at `12:00` the next day) — this must be computed correctly, not truncated.

---

## 6. Aggregation and Fault Isolation (BR-007, FR-007–009, FR-049–050)

Restating `docs/architecture/architecture-plan.md` Section 3.2 (AD-010) as the binding implementation shape — not a new decision:

```csharp
public async Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken ct)
{
    var tasks = _providers.Select(p => SafeInvokeAsync(p, request, ct));
    var results = await Task.WhenAll(tasks);          // FR-049 — concurrent invocation
    return results.SelectMany(r => r).ToList();
}

private async Task<IReadOnlyList<FlightResult>> SafeInvokeAsync(
    IFlightProvider provider, SearchRequest request, CancellationToken ct)
{
    try
    {
        return await provider.SearchAsync(request, ct);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Provider {ProviderName} failed during search", provider.ProviderName);
        return Array.Empty<FlightResult>();             // FR-050 — treated as empty collection
    }
}
```

**Critical implementation detail (AD-010):** the per-task `try`/`catch` inside `SafeInvokeAsync` must be evaluated *before* the task is handed to `Task.WhenAll` — `Task.WhenAll` alone does not swallow an individual task's exception; without this structure, one provider's exception would propagate and the whole search would fail with a 500, violating BR-007.

### 6.1 Worked Fault-Isolation Example

**Scenario:** `BudgetWingsProvider.SearchAsync` throws (e.g., a simulated internal fault, or a real bug) during a search for `LHR → JFK`, Economy, 2 passengers, `2026-08-01`.

1. `SafeInvokeAsync` catches the exception inside its own task, before `Task.WhenAll` completes.
2. A warning-level log entry is written: `Provider {ProviderName} failed during search` with `ProviderName = "BudgetWings"` and the exception object attached (message + type; no passenger/document PII is ever part of this log call, since none is in scope at search time) — NFR-OBS-001, NFR-AVAIL-003.
3. `SafeInvokeAsync` returns `Array.Empty<FlightResult>()` for BudgetWings's contribution.
4. `GlobalAirProvider.SearchAsync` completes normally, returning its 4 fixed flights (Section 3.1), each in Economy fare (Section 4).
5. `FlightAggregatorService.SearchAsync` merges the two provider results: `[] + GlobalAir's 4 flights = GlobalAir's 4 flights`.
6. `SearchController` returns **`200 OK`** — not 500 — with the following body (illustrative, prices from Section 4's worked examples):

```json
[
  { "provider": "GlobalAir", "flightNumber": "GA101", "origin": "LHR", "destination": "JFK",
    "departureDateTime": "2026-08-01T09:00:00Z", "arrivalDateTime": "2026-08-01T17:30:00Z",
    "durationMinutes": 510, "cabinClass": "Economy", "baseFare": 250.00, "pricePerPassenger": 287.50 },
  { "provider": "GlobalAir", "flightNumber": "GA204", "origin": "LHR", "destination": "DXB",
    "departureDateTime": "2026-08-01T22:00:00Z", "arrivalDateTime": "2026-08-02T05:30:00Z",
    "durationMinutes": 450, "cabinClass": "Economy", "baseFare": 300.00, "pricePerPassenger": 345.00 },
  { "provider": "GlobalAir", "flightNumber": "GA309", "origin": "JFK", "destination": "LAX",
    "departureDateTime": "2026-08-01T07:15:00Z", "arrivalDateTime": "2026-08-01T12:45:00Z",
    "durationMinutes": 330, "cabinClass": "Economy", "baseFare": 180.00, "pricePerPassenger": 207.00 },
  { "provider": "GlobalAir", "flightNumber": "GA412", "origin": "MAN", "destination": "LHR",
    "departureDateTime": "2026-08-01T06:45:00Z", "arrivalDateTime": "2026-08-01T07:55:00Z",
    "durationMinutes": 70, "cabinClass": "Economy", "baseFare": 80.00, "pricePerPassenger": 92.00 }
]
```

7. **Nothing in this response body indicates BudgetWings failed** (FR-070 — provider failures are silently degraded, never surfaced to the client). No `warnings` field, no `partial: true` flag, no error object of any kind. The failure is observable only in the server-side log, never in the API response or the UI.
8. If a client repeats the identical search a second time (BudgetWings still failing), the result is deterministic and repeatable — the same 4 GlobalAir flights, since the underlying schedule is fixed (Section 2).

This scenario, and its integration-level equivalent through the full `POST /api/search` pipeline, is the required automated test coverage for BR-007/NFR-TEST-006/NFR-AVAIL-002 (`docs/testing/test-strategy.md` Section 6) — this document supplies the exact provider names, flight numbers, and prices Phase 13 should use when writing that test, rather than inventing new placeholder data.

---

## 7. Compliance Statement

This document introduces no new interface, endpoint, or business rule beyond `docs/requirements.md` v1.4 (US-007, FR-046–053, BR-001, BR-002, BR-007, BR-009, ASM-006, ASM-007) and `docs/architecture/architecture-plan.md` v1.0 Section 3.1–3.2 (AD-009, AD-010). The concrete mock dataset, cabin multipliers, and datetime-construction rule are explicitly labelled Gap-fill decisions (Section 8) because ASM-006/FR-048 deliberately left them unspecified for implementation-time judgment — they introduce no new user-facing capability and do not change any approved formula, format, or fault-isolation behavior.

---

## 8. Gap-Fill Decisions (Summary)

| ID | Decision | Rationale |
|---|---|---|
| GAP-PA-01 | Each provider returns a fixed set of exactly 4 hardcoded flights per search (Section 3), with the specific routes/times/durations/base fares given in the tables above. | ASM-006/FR-048 require "a realistic, hardcoded set of flights" but deliberately leave the specific content to implementation; a documented dataset prevents divergent/ad hoc data across Phase 12 and gives Phase 13 test authors a stable fixture, consistent with `docs/testing/test-strategy.md` Section 3's expectation of a "known, fixed set of flights." |
| GAP-PA-02 | Cabin class fare multipliers: Economy ×1.0, Business ×2.0, First Class ×3.5, applied to the Economy base fare before the provider's pricing rule (BR-001/BR-002). | BR-009 requires flights to be available "for the requested cabin class" but neither `docs/requirements.md` nor the architecture plan define how a single mock flight's fare varies by class; fixed multipliers guarantee every flight is available in every class (no artificial cabin-class gaps) with deterministic, testable numbers. |
| GAP-PA-03 | `departureDateTime`/`arrivalDateTime` combine the flight's fixed local time-of-day with the requester's `SearchRequest.departureDate`; the set of flights and their times-of-day remain fixed and independent of the requested date/route (ASM-006 preserved). | ASM-006 establishes that date/route don't filter which flights are returned but is silent on what date value appears in the response; anchoring only the date component avoids a nonsensical, stale hardcoded date in the UI while keeping the approved fixed-schedule design fully intact. |
| GAP-PA-04 | `ProviderName` literal values are exactly `"GlobalAir"` and `"BudgetWings"` (used verbatim in the `provider` field on every result). | FR-052 and the architecture plan's Section 5 example imply this but never state the exact literal spelling/casing as a binding rule; fixing it prevents inconsistent literals across the two provider classes, DI registration, and test doubles. |

---

*End of Feature Specification — Provider Aggregation and Mock Provider Data v1.0.*
