# Feature Specification — Flight Search

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | FEAT-FS-001 |
| Version | 1.0 |
| Date | 2026-07-03 |
| Status | Draft — Ready for Phase 11 Spec Readiness Check |
| Owner | solution-architect |
| Source | `docs/requirements.md` v1.4, `docs/specs/non-functional-requirements.md` v1.0, `docs/architecture/architecture-plan.md` v1.0, `docs/testing/test-strategy.md` v1.0 |
| Phase | Phase 10 — Feature Specifications |
| User Stories | US-001 (Search for Available Flights), US-008 (View Airport Selection) |
| Backlog Items Made Implementation-Ready | BL-003, BL-004, BL-010, BL-017, BL-020, BL-021, BL-022, BL-026, BL-027, BL-028 |

### Purpose and Scope

This document is the final concretization layer for the search-request side of US-001/US-008. It does not reopen, alter, or add scope beyond `docs/requirements.md` v1.4 or `docs/architecture/architecture-plan.md` v1.0. Every exact string, regex, JSON shape, and UI-state rule below is either a direct restatement of an already-approved FR/BR/AD, or an explicitly labelled **Gap-fill decision** — a concrete implementation-detail choice made within already-approved flexibility, per the task brief governing this phase. Gap-fill decisions do not require a new Product Owner approval gate (they are not scope changes) but are flagged so Phase 12 does not re-debate them. See Section 8 for the full list.

Search **results** display and sorting are specified separately in `docs/features/feature-search-results-and-sorting.md` — this document covers only the search form, the request contract, and the request-lifecycle UI states (loading/empty/error triggers).

---

## 1. Search Form — Fields

| Field | Control Type | Values / Range | Required | Client-side Rule (mirror only — DP-014) | Source |
|---|---|---|---|---|---|
| Origin | Dropdown (`<select>`), populated from `AIRPORTS` constant (BL-022) | One of the static airport codes (min. 6 airports, min. 2 countries — FR-056) | Yes | Must differ from Destination (US-001 AC8) | US-001 AC1, FR-006, FR-056–059 |
| Destination | Dropdown (`<select>`), same source as Origin | Same as Origin | Yes | Must differ from Origin (US-001 AC8) | US-001 AC1, FR-006 |
| Departure Date | Native browser date input (`<input type="date">`, ASM-010 — no custom calendar widget) | Today or any future date | Yes | Cannot be in the past (US-001 AC9); browser `min` attribute set to today's date as a convenience affordance | US-001 AC2, AC9, FR-003 |
| Passenger Count | Dropdown (`<select>`) with exactly 9 options, values `1`–`9` | Integer, 1–9 inclusive | Yes | Range is unrepresentable outside 1–9 by construction (Gap-fill FS-02, Section 8) | US-001 AC3, FR-004 |
| Cabin Class | Dropdown (`<select>`) or radio group, exactly 3 options | `Economy`, `Business`, `First Class` (exact literal strings, including the space in "First Class") | Yes | N/A — fixed option set | US-001 AC4, FR-005 |
| Trip Type | **Not a visible form control** (Gap-fill FS-01, Section 8) | Fixed literal `OneWay` | Sent on every request, not user-editable | N/A | FR-001, FR-005b, BR-013 |

The "Search" submit button is disabled while any required field is empty or fails its client-side mirror rule (US-001 AC5, DP-014).

### 1.1 Airport Dropdown Data Shape (US-008)

Single authoritative frontend source: `shared/constants/airports.constants.ts` (BL-022, AD-002, DP-012). Each entry:

```typescript
interface Airport {
  code: string;        // IATA code, 3 uppercase letters, e.g. "LHR"
  city: string;         // e.g. "London"
  country: string;      // e.g. "United Kingdom"
  displayName: string;  // e.g. "London Heathrow (LHR)"
}
```

Minimum data set (per FR-056–059, restating the recommended list from `docs/requirements.md` Section 3.7 verbatim — not a new decision):

| code | city | country | displayName |
|---|---|---|---|
| LHR | London | United Kingdom | London Heathrow (LHR) |
| MAN | Manchester | United Kingdom | Manchester (MAN) |
| JFK | New York | United States | New York JFK (JFK) |
| LAX | Los Angeles | United States | Los Angeles (LAX) |
| DXB | Dubai | United Arab Emirates | Dubai (DXB) |
| SYD | Sydney | Australia | Sydney (SYD) |

Each dropdown option must render, at minimum, code + city + country (US-008 AC1) — the `displayName` field satisfies this directly and is the recommended rendered label.

**Same-airport guard (US-001 AC8 / US-008 AC4):** when the user selects an airport in one dropdown that is currently selected in the other, the form must either (a) disable that option in the second dropdown, or (b) clear the second dropdown's selection and show a validation message. Either implementation is acceptable — this is a UI-affordance choice, not a behavioral requirement beyond "cannot select the same airport as both."

The backend independently maintains its own static `AirportDataService` (BL-004) for FR-006 validation — this is not the same object reference as the frontend constant; each is the single source for its own layer (FR-055).

---

## 2. Search Request — Exact JSON Shape

`POST /api/search`

```json
{
  "origin": "LHR",
  "destination": "JFK",
  "departureDate": "2026-08-01",
  "passengerCount": 2,
  "cabinClass": "Economy",
  "tripType": "OneWay"
}
```

| Field | Type | Format | Notes |
|---|---|---|---|
| `origin` | string | 3 uppercase letters | Must be a known airport code (FR-006) |
| `destination` | string | 3 uppercase letters | Must be a known airport code, must differ from `origin` (FR-002) |
| `departureDate` | string | `YYYY-MM-DD` (ISO 8601 date, no time component) | Must not be in the past (FR-003) |
| `passengerCount` | integer | 1–9 inclusive | FR-004 |
| `cabinClass` | string | Exactly one of `"Economy"`, `"Business"`, `"First Class"` | FR-005 |
| `tripType` | string | Exactly `"OneWay"` | FR-005b, BR-013 — the only currently valid value; any other value or absence is a 400 |

This shape is taken verbatim from `docs/architecture/architecture-plan.md` Section 5 — no new field is introduced.

---

## 3. Search Response — Success (200)

Full response shape (per flight result) and sorting behavior are specified in `docs/features/feature-search-results-and-sorting.md`. This document only confirms: `POST /api/search` returns `200 OK` with a JSON array of `FlightResult` objects (possibly empty — see Section 5.2) on any successful, valid request, regardless of whether one or more providers failed (BR-007; see `docs/features/feature-provider-aggregation.md`).

---

## 4. Search Request — Validation Error Response (400)

All validation failures return `400 Bad Request` using the canonical error envelope defined in `docs/features/feature-error-handling-and-validation.md` Section 1 (ASP.NET Core `ValidationProblemDetails` shape, produced via `ValidationProblem(...)` — AD-003). Field-error keys use camelCase matching the request body property names (**Gap-fill FS-05**).

### 4.1 Exact Validation Rules and Field-Error Messages

| Rule | Requirement | Field Key | Exact Error Message (**Gap-fill FS-03**) |
|---|---|---|---|
| Origin missing or not 3 uppercase letters | FR-006, FR-061 | `origin` | `"Origin airport code is required and must be a valid 3-letter airport code."` |
| Destination missing or not 3 uppercase letters | FR-006, FR-061 | `destination` | `"Destination airport code is required and must be a valid 3-letter airport code."` |
| Origin equals destination | FR-002 | `destination` | `"Origin and destination airports must be different."` |
| Origin not a known airport code | FR-006 | `origin` | `"Origin airport code is not recognized."` |
| Destination not a known airport code | FR-006 | `destination` | `"Destination airport code is not recognized."` |
| Departure date missing or malformed | FR-003, FR-061 | `departureDate` | `"Departure date is required and must be a valid date."` |
| Departure date in the past | FR-003 | `departureDate` | `"Departure date cannot be in the past."` |
| Passenger count missing, non-integer, or outside 1–9 | FR-004 | `passengerCount` | `"Passenger count must be a whole number between 1 and 9."` |
| Cabin class missing or not one of the 3 permitted values | FR-005 | `cabinClass` | `"Cabin class must be one of: Economy, Business, First Class."` |
| Trip type missing or not `"OneWay"` | FR-005b | `tripType` | `"Trip type must be 'OneWay'."` |

### 4.2 Worked Example — 400 Response Body

Request with identical origin/destination:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "destination": ["Origin and destination airports must be different."]
  }
}
```

Multiple simultaneous failures (e.g., past date AND passenger count out of range) return all failing fields in the same `errors` object — the validator does not short-circuit after the first failure (FR-063 requires identifying "which field(s)" — plural).

---

## 5. Frontend UI States (FR-013–015)

### 5.1 Loading State

- Triggered: immediately on valid form submission, before the HTTP call resolves.
- Duration: from submission until the response (success or error) is received (FR-013).
- Behavior: submit button is disabled and shows a loading affordance (e.g., spinner + "Searching…" label — exact visual treatment is a UI-styling choice, not a behavioral requirement); the results area shows a loading indicator, not stale prior results, while a request is in flight for a *new* search. (Note: US-002 AC8 — results from a *previous completed* search remain visible until the user *initiates a new search*; once a new search is initiated, the loading state supersedes the prior results view.)

### 5.2 Empty State

- Triggered: `200 OK` response with an empty array (`[]`) — a legitimate outcome, not an error, e.g., if every provider result for the requested cabin class happens to be filtered out at a future point, or (in the current fixed-mock-data design) never in practice, but the UI must still handle it per FR-014.
- Exact message (already specified verbatim in US-002 AC6 — not a new decision): `"No flights found for your search. Please try different criteria."`

### 5.3 Error State

- Triggered: the HTTP call to `POST /api/search` fails — either a non-2xx response (400 validation failure surfaced inline on the form per Section 4, or an unexpected 500) or a network-level failure (timeout, connection refused).
- **400 validation errors** are rendered inline, next to the offending field(s), using the field-error messages from Section 4.1 — the form is not submitted; no generic error banner is shown for a 400 (this is a form-correction flow, not a service-failure flow).
- **500 / unexpected server error and network failures** are rendered as a single, generic banner-style message, without exposing backend details (FR-015, FR-071):
  - Server error (500): `"We couldn't complete your search. Please try again."` (**Gap-fill FS-04**)
  - Network failure (timeout/connection refused): `"Network error. Please check your connection and try again."` (**Gap-fill FS-04**, satisfies FR-072's "Should Have" requirement for actionable guidance)
- Raw HTTP status codes and exception messages must never appear in the rendered UI (FR-071).

---

## 6. Backend Validation Authority (DP-014)

Frontend validation (Section 1's "Client-side Rule" column) is a convenience layer only. The backend `SearchRequestValidator` (BL-010) is authoritative and re-validates every rule in Section 4.1 independently of what the frontend already checked — a request that somehow bypasses the frontend (e.g., a direct API call) must be rejected identically. No behavior described in this document may be implemented as frontend-only.

---

## 7. Compliance Statement

This document introduces no new endpoint, field, or business rule beyond `docs/requirements.md` v1.4 (US-001, US-008, FR-001–FR-006, FR-013–015, FR-054–059, FR-061, BR-013) and `docs/architecture/architecture-plan.md` v1.0 (AD-002, AD-003, Section 5 API contract). All exact strings and widget choices below the requirements' level of detail are labelled Gap-fill decisions (Section 8) and remain within already-approved flexibility.

---

## 8. Gap-Fill Decisions (Summary)

| ID | Decision | Rationale |
|---|---|---|
| GAP-FS-01 | `tripType` is not a visible/editable form control; the frontend sends the fixed literal `"OneWay"` on every request. | BR-013 permits only one valid value for MVP; a single-option control adds no user value and costs implementation time against the EOD deadline. |
| GAP-FS-02 | Passenger count uses a `<select>` dropdown with exactly the 9 options `1`–`9`, not a free numeric input. | Makes FR-004's 1–9 range structurally unrepresentable outside the valid set, removing a class of client-side edge cases (e.g., decimals, negative numbers) at zero extra cost. |
| GAP-FS-03 | Exact validation error message strings (Section 4.1). | FR-063 requires field-level detail but not exact wording; fixing the wording now prevents inconsistent messages between the backend validator and the frontend's mirrored validation (DP-014). |
| GAP-FS-04 | Exact server-error and network-failure user-facing message strings (Section 5.3). | FR-015/FR-072 require a user-facing message but not exact wording. |
| GAP-FS-05 | Field-error JSON keys use camelCase matching the request body property names (e.g., `origin`, `departureDate`, `passengerCount`). | FR-063 does not specify a key-naming convention; camelCase matches the request/response body convention already established in the architecture plan. |

---

*End of Feature Specification — Flight Search v1.0.*
