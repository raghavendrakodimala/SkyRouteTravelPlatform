# Feature Specification — Search Results Display and Sorting

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | FEAT-SR-001 |
| Version | 1.0 |
| Date | 2026-07-03 |
| Status | Draft — Ready for Phase 11 Spec Readiness Check |
| Owner | solution-architect |
| Source | `docs/requirements.md` v1.4, `docs/architecture/architecture-plan.md` v1.0 (Section 5 API Contract Summary), `docs/specs/non-functional-requirements.md` v1.0 |
| Phase | Phase 10 — Feature Specifications |
| User Stories | US-002 (View Flight Search Results), US-003 (Sort Flight Results) |
| Backlog Items Made Implementation-Ready | BL-002, BL-008, BL-009, BL-023, BL-027, BL-029, BL-030 |

### Purpose and Scope

This document concretizes the display and client-side sorting of the array returned by `POST /api/search` (request/validation side specified in `docs/features/feature-flight-search.md`). It does not reopen or contradict `docs/requirements.md` v1.4 or `docs/architecture/architecture-plan.md` v1.0. Gap-fill decisions are labelled inline and summarized in Section 6.

---

## 1. Search Response — Exact JSON Shape (per result)

`POST /api/search` → `200 OK` → `FlightResult[]`. Taken verbatim from `docs/architecture/architecture-plan.md` Section 5:

```json
[
  {
    "provider": "GlobalAir",
    "flightNumber": "GA123",
    "origin": "LHR",
    "destination": "JFK",
    "departureDateTime": "2026-08-01T09:00:00Z",
    "arrivalDateTime": "2026-08-01T17:30:00Z",
    "durationMinutes": 510,
    "cabinClass": "Economy",
    "baseFare": 100.00,
    "pricePerPassenger": 115.00
  }
]
```

| Field | Type | Notes |
|---|---|---|
| `provider` | string | `"GlobalAir"` or `"BudgetWings"` — see `docs/features/feature-provider-aggregation.md` for exact literal values (FR-052) |
| `flightNumber` | string | Provider-assigned flight number |
| `origin` / `destination` | string | 3-letter airport code of the *flight's own route* — see Provider Aggregation spec Section 3 for the important caveat that this does not necessarily match the requested search route (ASM-006) |
| `departureDateTime` / `arrivalDateTime` | string | ISO 8601 datetime, `Z`-suffixed | See Section 2.2 for display formatting |
| `durationMinutes` | integer | ASM-011 — minutes, integer |
| `cabinClass` | string | Echo of the requested cabin class |
| `baseFare` | decimal, 2dp | Present in the payload for traceability/testability (FR-010); **not required to be rendered in the UI** (**Gap-fill SR-04**) |
| `pricePerPassenger` | decimal, 2dp | Post-pricing-rule (BR-001/BR-002) per-passenger price; this is the figure the frontend multiplies by `passengerCount` (FR-012, FR-017) |

No `totalPrice` field exists in the response — computed on the frontend only (FR-012, DP-011).

---

## 2. Result Card/Row — Display Fields and Formatting (FR-016–019)

Each result must display: provider name, flight number, departure time, arrival time, duration, cabin class, total price (primary), per-passenger price (secondary), and origin/destination (at minimum codes; city names preferred, FR-019 Should Have).

### 2.1 Price Display (FR-017, FR-018, US-002 AC2–4)

- **Single calculation location (DP-011):** `shared/utils/pricing.util.ts` (BL-023) is the only place `totalPrice = pricePerPassenger × passengerCount` is computed on the frontend. Every screen that shows a total (results, booking, confirmation) calls this one function.
- Formatting: `"USD {amount}"` with the amount formatted to exactly 2 decimal places (e.g., `"USD 320.00"`) — this is the literal format from FR-017/US-002 AC2, not a currency-symbol format (i.e., not `"$320.00"`).
- Combined label (FR-018, verbatim from the requirement's own example): `"USD 320.00 total / USD 160.00 per person"`.
- Total price must be the visually dominant element (larger font size and/or bolder weight and/or positioned first) relative to the per-passenger price on every screen it appears (US-002 AC4, NFR-USE-002). Exact typography is a styling decision outside this document's scope — the *requirement* is dominance, not a specific font size.
- Rounding: `pricePerPassenger` arrives already rounded to 2dp from the backend (BR-001/BR-002). `totalPrice = pricePerPassenger × passengerCount` is computed on an already-2dp value multiplied by an integer, so no additional rounding step introduces new precision — the frontend still applies `.toFixed(2)` defensively for consistent display.

### 2.2 Time and Duration Formatting (FR-016)

- **Departure/arrival time:** rendered as `HH:mm` (24-hour). **Gap-fill SR-02:** the displayed time is the literal `HH:mm` substring taken directly from the ISO datetime string's time component — no timezone conversion is performed. Rationale: the mock providers carry no real timezone/airport-local-time data model (ASM-006/ASM-007); performing a browser-local timezone conversion on data that isn't real would produce misleading, inconsistent times rather than more correct ones. The string is treated as "the local departure/arrival time as provided," full stop.
- **Duration:** rendered as `"Xh Ym"` (e.g., `"2h 15m"`, matching the FR-016 example exactly). **Gap-fill SR-01:** both components are always shown, even when one is zero (e.g., a 180-minute duration renders as `"3h 0m"`, not `"3h"`) — a single, unconditional formatting rule with no branching, consistent with KISS.
  - Formula: `hours = floor(durationMinutes / 60)`, `minutes = durationMinutes % 60`.

### 2.3 Origin/Destination Display (FR-019, Should Have)

Display airport codes at minimum (e.g., `"LHR → JFK"`); city names are preferred if the `Airport` lookup (from `airports.constants.ts`, BL-022) is consulted for display purposes. This is Should Have — a codes-only display satisfies the Must Have bar.

---

## 3. Empty and Error States

Specified fully in `docs/features/feature-flight-search.md` Section 5.2–5.3 (this is the same results-area component displaying those states; not re-specified here to avoid duplication).

Additional rule specific to this document: **results persist until a new search is initiated** (US-002 AC8) — navigating to the booking screen and back, or any other in-app navigation that does not re-submit the search form, must not clear the previously displayed result set held in `SearchStateService` (BL-027).

---

## 4. Sorting (US-003, FR-020–024)

### 4.1 Sort Options

| Option | Sort Key | Direction |
|---|---|---|
| Price: low to high (**default**, FR-022) | `pricePerPassenger` | Ascending |
| Price: high to low | `pricePerPassenger` | Descending |
| Duration: shortest first | `durationMinutes` | Ascending |
| Departure time: earliest first | `departureDateTime` | Ascending (chronological, ISO string comparison is sufficient since all values share the same format) |

Sorting by `pricePerPassenger` produces an identical ordering to sorting by total price within a single result set, because every result in a given response shares the same `passengerCount` (the value the user searched with) — multiplying every element by the same constant does not change relative order. The sort key is therefore `pricePerPassenger`, not a computed total, avoiding a redundant per-item calculation at sort time.

### 4.2 Behavior

- Sorting is a pure client-side `computed()` recombination of the existing results signal (AD-006) — zero additional HTTP calls (FR-021), matching the example in `docs/architecture/architecture-plan.md` Section 4.3: `sortedResults = computed(() => sortFlights(this.searchState.results(), this.sortOption()));`.
- Default sort on initial results display: Price low to high (FR-022, US-003 AC5).
- The currently active sort option is visually indicated in the sort control (FR-023, US-003 AC3) — exact visual treatment (e.g., a highlighted/selected button state) is a styling decision, not specified further here.
- Sorting re-orders the **entire** current result set, not a subset or a paginated slice (FR-024, US-003 AC4) — there is no pagination in this MVP.
- **Gap-fill SR-03 — tie-breaking:** the sort function must be a *stable* sort (i.e., elements with equal key values retain their relative order from before the sort was applied — which, before any sort has ever been applied, is the original provider-aggregation order). No secondary/tertiary sort key is introduced. Rationale: FR-020–024 specify the four sort keys but not a tie-breaking rule; a stable sort is the simplest possible choice (KISS) and requires inventing no new comparison logic. JavaScript's `Array.prototype.sort` (used by `sortFlights`) is guaranteed stable per the ECMAScript specification, so no additional implementation effort is required to satisfy this — it only needs to be *preserved* (i.e., the comparator must return `0` for equal keys, not an arbitrary tiebreak).

---

## 5. Compliance Statement

This document introduces no new field, endpoint, or sort option beyond `docs/requirements.md` v1.4 (US-002, US-003, FR-010, FR-012, FR-016–024) and `docs/architecture/architecture-plan.md` v1.0 Section 5/4.3. All formatting and tie-breaking decisions not already specified at that level of detail are labelled Gap-fill decisions (Section 6).

---

## 6. Gap-Fill Decisions (Summary)

| ID | Decision | Rationale |
|---|---|---|
| GAP-SR-01 | Duration always renders both hour and minute components (e.g., `"3h 0m"`, never `"3h"`). | FR-016 gives one example without a zero-component rule; one unconditional formatting rule avoids branching complexity (KISS). |
| GAP-SR-02 | Departure/arrival times render as the literal `HH:mm` substring of the ISO datetime, with no timezone conversion. | Mock providers carry no real timezone data; converting fictitious data through a real timezone pipeline would introduce misleading precision, not correctness. |
| GAP-SR-03 | Sort is a stable sort; equal-key results retain their original (pre-sort/aggregation) relative order. No secondary sort key is introduced. | FR-020–024 specify sort keys but not tie-breaking; stable sort is the simplest default and requires no new comparison logic to be invented. |
| GAP-SR-04 | `baseFare` is present in the API response payload (per FR-010) but is not required to be rendered anywhere in the UI. | FR-016–019 (the UI display requirements) do not list `baseFare` among the fields to display; it exists in the contract for traceability/testability of the pricing rules (DP-019), not for end-user consumption. |

---

*End of Feature Specification — Search Results Display and Sorting v1.0.*
