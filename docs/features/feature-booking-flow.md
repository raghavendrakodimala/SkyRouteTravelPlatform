# Feature Specification — Booking Flow (Selection, Passenger Details, Confirmation)

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | FEAT-BF-001 |
| Version | 1.1 |
| Date | 2026-07-03 (v1.1 amendments 2026-07-07) |
| Status | Implemented (2026-07-07) — v1.1 amendments: passenger details are collected one passenger at a time through a single in-place form with saved-passenger summary cards (Product Owner UX direction 2026-07-07, Sections 1.2, 2, 2.4), replacing the original all-at-once one-section-per-passenger layout; wire contracts (Sections 3–7) unchanged |
| Owner | solution-architect |
| Source | `docs/requirements.md` v1.4 (US-004, US-005, US-006, FR-025–045, BR-003, BR-004, BR-005, BR-006, DP-015, DP-016), `docs/architecture/architecture-plan.md` v1.0 (Section 3.3, Section 5) |
| Phase | Phase 10 — Feature Specifications |
| User Stories | US-004 (Select a Flight and Initiate Booking), US-005 (Enter Passenger Details), US-006 (Confirm Booking and Receive Reference) |
| Backlog Items Made Implementation-Ready | BL-002, BL-005, BL-006, BL-011, BL-012, BL-013, BL-014, BL-015, BL-018, BL-020, BL-023, BL-024, BL-029, BL-031, BL-032, BL-034, BL-035, BL-036, BL-037, BL-038 |

### Purpose and Scope

This document concretizes the entire booking journey: flight selection → booking screen → passenger details → confirmation. It does not reopen or contradict `docs/requirements.md` v1.4 or `docs/architecture/architecture-plan.md` v1.0. Gap-fill decisions are labelled inline and summarized in Section 9.

---

## 1. Booking Screen — Data Carried From Search (US-004, FR-025)

No API call is made to populate the booking screen (FR-025, US-004 AC4). The selected `FlightResult` (from `docs/features/feature-search-results-and-sorting.md` Section 1), the `passengerCount` from the original search request (always `1` since the 2026-07-07 amendment — see `docs/features/feature-flight-search.md` Section 1), and the `cabinClass` are written into `BookingStateService` (BL-032, a Signal-based Angular service) at the moment the user clicks "Select"/"Book" on a result row (BL-029). The booking screen (`BookingFormComponent`) reads exclusively from this state — never re-fetches or re-derives it from the network. **v1.1 amendment:** the searched `passengerCount` no longer sizes the passenger form — the number of passengers is determined on the booking screen itself by adding passengers one at a time (Section 2).

### 1.1 Flight Summary Display (US-004 AC2, FR-026)

Displayed fields: route (`origin → destination`), provider name, flight number, departure time, arrival time, cabin class — using the same `HH:mm`/duration formatting rules as `docs/features/feature-search-results-and-sorting.md` Section 2.2.

### 1.2 Price Breakdown Display (US-004 AC3, FR-027)

Displayed fields: per-passenger price, number of passengers, total price — computed via the **same** `pricing.util.ts` function (BL-023, DP-011) used on the results screen; total price is again the visually dominant figure (NFR-USE-002). **v1.1 amendment:** the breakdown is **live** — the count is `max(saved passengers, 1)` so a blank in-progress form never inflates the total (while entering passenger 1 it shows `× 1`), and it updates as passengers are added, edited, or removed. After a booking is confirmed (e.g., back-navigation to `/booking`), the breakdown shows the actually **booked** figures taken from `BookingResponse.totalPrice`/`passengers.length`, never a recomputation.

---

## 2. Passenger Detail Form — Field-by-Field Rules (US-005, FR-028–031)

**v1.1 amendment (Product Owner UX direction 2026-07-07 — implemented in `BookingFormComponent`):** passenger details are collected **one passenger at a time**, not through simultaneously rendered per-passenger sections. Exactly one `PassengerFormSectionComponent` (BL-034) instance is ever active — a single reusable form rendered in place below the saved-passenger cards — with two persistent actions under it:

1. **"Add another passenger"** (secondary): validates the active form; if valid, the passenger is appended as a compact summary card (full name, email, document number shown unmasked, with **Edit** and **Remove** actions and positional renumbering) and the same form resets in place for the next passenger, with focus moved to its first field. At the cap of **9 passengers** the blank form and the add action are removed and Confirm Booking is the only remaining action.
2. **"Confirm Booking"** (primary): a filled active form is validated and saved first, then **all** saved passengers are submitted; a blank form with at least one saved passenger submits as-is; a blank form with nothing saved surfaces the required-field errors for passenger 1.

While a card is being edited, the actions become **"Save changes"** / **"Cancel edit"** in the same slot; editing another card while the edit form is dirty is blocked (`aria-disabled` with an explanatory accessible-name suffix — a dirty form is never silently discarded), and an in-progress new-passenger draft is parked and restored when an edit resolves. All state changes are announced through a single persistent polite live region, every DOM-removing transition names an explicit focus target (focus never drops to `<body>`), and a `canDeactivate` router guard (`bookingLeaveGuard`) plus a `beforeunload` listener confirm before unconfirmed passenger data is destroyed.

Passengers are numbered "Passenger 1", "Passenger 2", … in card order. Passenger 1 is the lead passenger; their email is also the primary booking contact (US-005 AC3, BR-005) — this is a display/labelling distinction only, not a different form field (every passenger, including Passenger 1, submits their own `email`).

### 2.1 Full Name

| Rule | Detail | Source |
|---|---|---|
| Required | Non-empty | FR-029 |
| Minimum length | 2 characters | US-005 AC7, FR-029 |
| Maximum length | 100 characters (**Gap-fill BF-02**) | — |
| Must contain at least one letter | Rejects numeric-only input (e.g., `"12"`) | FR-064 |
| Named validator (both layers, DP-015) | Backend: `FullNameValidator` / regex `^(?=.*[A-Za-z]).{2,100}$`. Frontend: mirrored constant in `document-number.validators.ts`'s sibling file or an equivalent named validator — same pattern string. | FR-064, DP-015 |

### 2.2 Email Address

| Rule | Detail | Source |
|---|---|---|
| Required | Non-empty | FR-029 |
| Format | "Standard email format" / RFC 5322 simplified (FR-065 explicitly permits .NET's built-in email validation) | US-005 AC8, FR-065 |
| Named pattern (**Gap-fill BF-01**) | `^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$` — defined once as a named constant/validator on the backend (referenced by `BookingRequestValidator`) and mirrored once on the frontend (DP-015); if the backend instead uses ASP.NET Core's built-in `[EmailAddress]` DataAnnotation per FR-065's explicit allowance, this regex is still the frontend's named mirror and must produce equivalent accept/reject behavior for all test cases in `docs/testing/test-strategy.md` Section 5. | FR-065, DP-014, DP-015 |

### 2.3 Document Number — Label and Validation by Route Type (US-005 AC5–6, BR-003)

The document type (and therefore the field's label and validation pattern) is determined **once**, at booking-screen load time, by `RouteTypeResolver` (BL-005) evaluating the selected flight's `origin`/`destination` country fields (via the airport data — `AirportDataService`/`airports.constants.ts`), and does not change for the remainder of the booking session (BR-003, "fixed for a given booking").

| Route Type | Determination Rule | Field Label | Validation Pattern | Character Set |
|---|---|---|---|---|
| International | Origin country ≠ Destination country | `"Passport Number"` | `^[A-Z0-9]{6,9}$` | 6–9 characters, **uppercase** letters and digits only, no spaces, no hyphens |
| Domestic | Origin country = Destination country | `"National ID"` | `^[A-Za-z0-9-]{5,20}$` (**Gap-fill BF-04**) | 5–20 characters, letters (either case) and digits, hyphens permitted, no spaces |

**Gap-fill BF-04 rationale:** BR-003/US-005 AC6 state the Passport rule explicitly as "uppercase letters and digits only," but state the National ID rule only as "5–20 alphanumeric characters, may include hyphens" — with no case restriction. The literal absence of a case restriction where the sibling rule explicitly has one is read as intentional; the National ID pattern therefore permits both cases.

Both patterns are named constants (`DocumentPatterns.PassportPattern`, `DocumentPatterns.NationalIdPattern` — BL-006) referenced identically by the backend `BookingRequestValidator` (BL-014) and the frontend `document-number.validators.ts` (BL-024) — never duplicated inline (DP-015).

#### 2.3.1 Worked Example — Document Type Routing (BR-003)

- Selected flight: `LHR → JFK` (from `docs/features/feature-provider-aggregation.md` Section 3.1, GA101). `LHR`'s country is `"United Kingdom"`; `JFK`'s country is `"United States"`. Countries differ → **International** → field label `"Passport Number"`, pattern `^[A-Z0-9]{6,9}$`. A submitted document number `"AB1234C"` (7 chars, uppercase alphanumeric) is **valid**; `"ab1234c"` (lowercase) is **invalid**; `"AB12"` (4 chars) is **invalid** (below minimum).
- Selected flight: `MAN → LHR` (GA412). Both airports' country is `"United Kingdom"`. Countries match → **Domestic** → field label `"National ID"`, pattern `^[A-Za-z0-9-]{5,20}$`. A submitted document number `"AB-1234"` (7 chars, includes hyphen) is **valid**; `"AB12"` (4 chars) is **invalid**.

### 2.4 Submission Gating (US-005 AC10, FR-031) — v1.1 Amendment

The "Confirm Booking" button is **never natively disabled** (consistent with the A11Y-007/A11Y-008 pattern adopted across the app — a focused button that becomes `disabled` drops keyboard focus to `<body>`). Gating is enforced at submit time instead: activating Confirm Booking validates and saves the active form when it contains input (invalid input keeps the flow on the form, with touched-field errors shown and focus moved to the first invalid control), and a blank form with no saved passengers surfaces the required-field errors for passenger 1 — the API request is only ever sent with a fully valid saved-passenger list. While the POST is in flight, every mutating control (Confirm, Add another, Edit, Remove, Save changes, Cancel edit) is locked via `aria-disabled` plus click guards. Server-side validation errors keyed `passengers[{i}].*` are mapped back to the offending passenger: an error-summary banner (`role="alert"`) is focused, flagged cards show a "Needs correction" badge, and the lowest-indexed flagged passenger reopens in the edit form, chaining through the remaining flagged passengers as each is corrected.

---

## 3. Booking Request — Exact JSON Shape (FR-039, FR-040, AD-004)

`POST /api/bookings`

```json
{
  "flight": {
    "provider": "GlobalAir",
    "flightNumber": "GA101",
    "origin": "LHR",
    "destination": "JFK",
    "departureDateTime": "2026-08-01T09:00:00Z",
    "arrivalDateTime": "2026-08-01T17:30:00Z",
    "durationMinutes": 510,
    "cabinClass": "Economy",
    "pricePerPassenger": 287.50
  },
  "passengerCount": 2,
  "passengers": [
    { "fullName": "Jane Doe", "email": "jane@example.com", "documentType": "Passport", "documentNumber": "AB1234C" },
    { "fullName": "John Doe", "email": "john@example.com", "documentType": "Passport", "documentNumber": "CD5678E" }
  ]
}
```

| Field | Type | Notes |
|---|---|---|
| `flight` | object | The full flight-detail snapshot carried from search (AD-004) — not an opaque ID. Required sub-fields: `provider`, `flightNumber`, `origin`, `destination`, `departureDateTime`, `arrivalDateTime`, `cabinClass`, `pricePerPassenger`. `durationMinutes`/`baseFare` may be included but are not required by the backend for booking creation. |
| `passengerCount` | integer | Must equal `passengers.length` (FR-062) |
| `passengers[].fullName` | string | Section 2.1 rules |
| `passengers[].email` | string | Section 2.2 rules |
| `passengers[].documentType` | string | Exactly `"Passport"` or `"National ID"` (FR-040's literal enum values — note the space in `"National ID"`) — submitted by the frontend based on its own client-side `RouteTypeResolver`-equivalent determination, but **not trusted**; the backend independently re-resolves route type from `flight.origin`/`flight.destination` and rejects a mismatch (see Section 5, DP-016, NFR-DATA-004) |
| `passengers[].documentNumber` | string | Section 2.3 rules, validated against the *backend-resolved* route type, not the client-submitted `documentType` |

No client-submitted total price exists in this contract (AD-004/AD-005) — the backend always computes its own (Section 6).

---

## 4. Booking Response — Exact JSON Shape (FR-044)

`201 Created`

```json
{
  "bookingReference": "SKY-INT-AB1C2D",
  "flight": {
    "provider": "GlobalAir",
    "flightNumber": "GA101",
    "origin": "LHR",
    "destination": "JFK",
    "departureDateTime": "2026-08-01T09:00:00Z",
    "arrivalDateTime": "2026-08-01T17:30:00Z",
    "cabinClass": "Economy",
    "pricePerPassenger": 287.50
  },
  "totalPrice": 575.00,
  "passengers": [
    { "fullName": "Jane Doe" },
    { "fullName": "John Doe" }
  ],
  "createdAtUtc": "2026-07-03T12:00:00Z"
}
```

Note: the response's `passengers` array intentionally carries only `fullName` per passenger (matching the architecture plan's example and FR-035's confirmation-screen field list, "the full name of each passenger") — email/document data is not echoed back in the booking response body, even though it was submitted and persisted (data-minimization consistent with NFR-PRIV-001/002 spirit; no requirement mandates echoing it back).

**Gap-fill BF-05:** the controller returns `StatusCode(201, response)` rather than `CreatedAtAction(...)`/`Created(uri, response)` with a populated `Location` header, because no `GET /api/bookings/{reference}` endpoint exists in this MVP (Out of Scope item 3 — no booking retrieval in the UI; AD-002-style scope boundary) to point a `Location` header at. An empty or omitted `Location` header is acceptable.

---

## 5. Server-Side Booking Orchestration (BookingService, BL-015)

Restating `docs/architecture/architecture-plan.md` Section 3.3's 7-step flow as the binding sequence — not a new design:

1. Run `BookingRequestValidator` (BL-014) — structural checks: `passengerCount == passengers.length`, per-field format (Sections 2.1–2.2) → 400 with field errors if invalid (FR-041, FR-062, FR-063).
2. Re-resolve route type server-side via `RouteTypeResolver` (BL-005) from `flight.origin`/`flight.destination` — **authoritative**, independent of the client-submitted `documentType` (BR-003, DP-016, NFR-DATA-004).
3. Validate each passenger's `documentType`/`documentNumber` against the *server-resolved* route type and the named pattern constants (DP-015) → 400 if the client-submitted `documentType` does not match the server-resolved route type, or if `documentNumber` fails the applicable pattern.
4. Recompute `totalPrice = flight.pricePerPassenger × passengerCount`, rounded to 2 decimal places server-side (BR-006, NFR-DATA-002) — this is a pure computation, since there is no client-submitted total to trust or distrust in this contract.
5. Call `BookingReferenceGenerator.GenerateBookingReference(routeType)` (BL-013); check `IBookingStore.ExistsAsync(reference, tenantId)`; on collision, regenerate (BR-004, NFR-DATA-001) — see Section 6 for the exact algorithm and the collision-retry bound.
6. Build a `Booking` domain object with `TenantId = tenantContext.TenantId` (DP-TENANT-003/005) and exactly `passengerCount` `PassengerDetail` records (BR-005, NFR-DATA-003); persist via `IBookingStore.CreateAsync(...)` (BL-011).
7. Map the persisted `Booking` to the `BookingResponse` shape in Section 4 (FR-044).

---

## 6. Booking Reference Generation — Exact Algorithm (BR-004, BL-013)

```text
Format:     SKY-[TYPE]-[XXXXXX]
TYPE:       "INT" if server-resolved route type is International; "DOM" if Domestic
XXXXXX:     6 characters, drawn from the 36-character set [A-Z0-9], generated using
            System.Security.Cryptography.RandomNumberGenerator (NOT System.Random)
Length:     14 characters total (e.g., SKY-INT-AB1C2D, SKY-DOM-XY9Z8K)
```

**Generation mechanics:** for each of the 6 output characters, draw a cryptographically random index into the 36-character alphabet `"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"` using `RandomNumberGenerator.GetInt32(0, 36)` (or an equivalent unbiased method drawing from `RandomNumberGenerator`'s byte stream — implementers must avoid modulo-bias if using raw random bytes directly; `RandomNumberGenerator.GetInt32(int, int)` already handles this correctly and is the recommended API).

**Collision handling:** after generating a candidate reference, call `IBookingStore.ExistsAsync(candidate, tenantId)`. If it already exists, generate a new candidate and check again.

**Gap-fill BF-03 — retry bound:** the regeneration loop is capped at **10 attempts**. If all 10 attempts collide (a probability on the order of 10⁻⁷⁷ given a keyspace of 36⁶ ≈ 2.176 billion combinations against, at most, a few dozen bookings created in a single local MVP session), the generator throws an exception, which propagates to and is handled by the centralized `ApiExceptionMiddleware` as a generic 500 (see `docs/features/feature-error-handling-and-validation.md`). Rationale: BR-004 requires "regenerate on collision" but does not specify a bound; an unbounded loop is a latent hang risk with zero corresponding benefit, since a collision is not expected to occur in practice at this scale — the cap exists purely for code hygiene.

`BookingReferenceGenerator.GenerateBookingReference(RouteType routeType)` is a standalone class with no constructor dependencies (AD-008) — independently unit-testable (DP-018, NFR-TEST-002) for both `RouteType.International` and `RouteType.Domestic`, asserting the `SKY-[TYPE]-XXXXXX` format, 14-character length, and uppercase-alphanumeric suffix.

---

## 7. Booking Request Validation Errors (400) — Full List

All entries below use the canonical error envelope defined in `docs/features/feature-error-handling-and-validation.md` Section 1.

| Rule | Field Key | Exact Error Message |
|---|---|---|
| `passengerCount` does not equal `passengers.length` | `passengerCount` | `"Passenger count must match the number of passenger records submitted."` |
| Passenger full name missing, too short, too long, or numeric-only | `passengers[{i}].fullName` | `"Full name is required, must be 2–100 characters, and must contain at least one letter."` |
| Passenger email missing or malformed | `passengers[{i}].email` | `"A valid email address is required."` |
| Passenger document number missing or failing the applicable pattern | `passengers[{i}].documentNumber` | For International: `"Passport number must be 6–9 uppercase letters and digits, with no spaces."` For Domestic: `"National ID must be 5–20 letters, digits, or hyphens, with no spaces."` |
| Submitted `documentType` does not match the server-resolved route type | `passengers[{i}].documentType` | `"Document type does not match the route for this booking."` |
| Required flight snapshot field missing (`provider`, `flightNumber`, `origin`, `destination`, `departureDateTime`, `arrivalDateTime`, `cabinClass`, or `pricePerPassenger`) | `flight` | `"Flight details are incomplete."` |

`{i}` is the zero-based index of the offending passenger in the `passengers` array (e.g., `passengers[1].documentNumber`), matching the field-error key style already established in `docs/architecture/architecture-plan.md` Section 5.

---

## 8. Confirmation Screen — Exact Display Fields (US-006 AC4–5, FR-035–036)

Reads exclusively from `BookingStateService`'s `bookingResponse` signal (BL-032/035) — no new API call.

| Field | Source | Display Note |
|---|---|---|
| Booking reference | `bookingReference` | Visually prominent (large font/distinct styling) — US-006 AC5, FR-036. Exact styling is a UI-styling choice, not specified further here. |
| Flight summary | `flight.origin` → `flight.destination`, `flight.provider`, `flight.flightNumber`, `flight.departureDateTime`, `flight.arrivalDateTime`, `flight.cabinClass` | Same `HH:mm` formatting as Section 1.1 |
| Total price | `totalPrice` | Same `"USD {amount}"` formatting as `docs/features/feature-search-results-and-sorting.md` Section 2.1 |
| Passenger list | `passengers[].fullName` | Full name of each passenger, in submission order (US-006 AC4 — "a list of all passengers with their names") |

**Re-submission guard (US-006 AC7, FR-038):** once the confirmation screen is displayed, there is no control that re-submits the same booking. Navigating away and returning to `/search` (a deliberate navigation action) is the only path back into a new booking flow; the confirmation screen itself has no "back" action that resubmits the prior `BookingRequest`. A route guard (per `docs/architecture/architecture-plan.md` Section 4.4, Should Have) redirecting `/booking`/`/confirmation` to `/search` when no `selectedFlight`/`bookingResponse` signal is populated is the recommended (not mandated) mechanism to prevent a broken UI on direct URL re-entry.

---

## 9. Compliance Statement

This document introduces no new field, endpoint, or business rule beyond `docs/requirements.md` v1.4 (US-004, US-005, US-006, FR-025–045, BR-003–006) and `docs/architecture/architecture-plan.md` v1.0 Section 3.3/Section 5 (AD-004, AD-008). Exact regex patterns, message text, and bounded-retry logic not already specified at that level of detail are labelled Gap-fill decisions (Section 10).

---

## 10. Gap-Fill Decisions (Summary)

| ID | Decision | Rationale |
|---|---|---|
| GAP-BF-01 | Email validation regex, defined once as a named constant/validator on each layer: `^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$`. | FR-065 permits "standard .NET email validation" without mandating a literal pattern; DP-015 requires a named, non-duplicated pattern — a concrete regex satisfies both without a new dependency. |
| GAP-BF-02 | Full name maximum length: 100 characters. | FR-029/FR-064 specify a minimum and a letter requirement but no maximum; an unbounded field has no product value and is a minor data-hygiene risk. |
| GAP-BF-03 | Booking reference collision-retry loop capped at 10 attempts; exhaustion throws, handled as a 500 by the centralized middleware. | BR-004 requires "regenerate on collision" without a bound; an unbounded loop is a latent hang risk with a keyspace (36⁶ ≈ 2.176 billion) that makes exhaustion effectively unreachable — the cap is code hygiene, not an anticipated real path. |
| GAP-BF-04 | National ID pattern permits both letter cases: `^[A-Za-z0-9-]{5,20}$` (unlike Passport, which is explicitly uppercase-only). | US-005 AC6/BR-003 state the Passport case restriction explicitly but are silent on case for National ID — the asymmetry between the two rules' wording is read as intentional. |
| GAP-BF-05 | Booking creation returns `StatusCode(201, response)` with no populated `Location` header (rather than `CreatedAtAction`). | No `GET /api/bookings/{reference}` endpoint exists in this MVP for a `Location` header to reference (Out of Scope item 3); avoids Phase 12 needing to build or fake a route target. |

---

*End of Feature Specification — Booking Flow v1.1 (2026-07-07 amendments: single in-place passenger form with saved-passenger cards, live price breakdown, submit-time gating).*
