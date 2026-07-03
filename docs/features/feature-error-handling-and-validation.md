# Feature Specification — Error Handling and Validation (Cross-Cutting)

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | FEAT-EH-001 |
| Version | 1.0 |
| Date | 2026-07-03 |
| Status | Draft — Ready for Phase 11 Spec Readiness Check |
| Owner | solution-architect |
| Source | `docs/requirements.md` v1.4 (FR-060–072, BR-011), `docs/architecture/architecture-plan.md` v1.0 (Section 3.6, Section 6), `docs/specs/non-functional-requirements.md` v1.0 (Section 6, NFR-SEC-001/002) |
| Phase | Phase 10 — Feature Specifications |
| Applies To | All endpoints (`POST /api/search`, `POST /api/bookings`) and both frontend Angular HTTP-calling services |
| Backlog Items Made Implementation-Ready | BL-003 (contract model DataAnnotations), BL-010 (`SearchRequestValidator`), BL-014 (`BookingRequestValidator`), BL-016 (`ApiExceptionMiddleware`), BL-017 (`SearchController`), BL-018 (`BookingController`); frontend consumers: BL-026/BL-027/BL-028/BL-029 (search error/empty states), BL-031/BL-032/BL-038 (booking error states) |

### Purpose and Scope

This document is the single canonical reference for error response shape and error-handling behavior across the entire MVP backend and frontend. `docs/features/feature-flight-search.md` and `docs/features/feature-booking-flow.md` reference this document rather than restating the envelope, avoiding duplication (DRY) while each still lists its own endpoint-specific field-error messages. This document does not reopen or contradict `docs/requirements.md` v1.4 or `docs/architecture/architecture-plan.md` v1.0. Gap-fill decisions are labelled inline and summarized in Section 5.

---

## 1. Canonical Error Response Shapes

### 1.1 400 Bad Request — Validation Failure (FR-067, FR-063)

Produced by ASP.NET Core's built-in `ValidationProblemDetails` type, returned via `ValidationProblem(...)` from a controller (AD-003) — **not** routed through the centralized exception middleware (BR-011 explicitly distinguishes: "Business-rule validation errors (400 responses) may be returned directly from controllers... they are not unhandled exceptions"). Two sources produce a 400 with this identical shape:

1. **Model-binding / DataAnnotations failures** — automatic, triggered by `[ApiController]`'s built-in model validation (e.g., malformed JSON, wrong field type, a `[Range]`/`[Required]` DataAnnotation failing on a `Contracts` class per AD-003) — produced by the framework with zero custom code.
2. **Cross-field/context validator failures** — `SearchRequestValidator` (BL-010) or `BookingRequestValidator` (BL-014) populate a field-keyed error dictionary and call `ValidationProblem(...)` explicitly.

Both sources produce the same shape:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "origin": ["Origin and destination airports must be different."],
    "passengerCount": ["Passenger count must be a whole number between 1 and 9."]
  },
  "traceId": "00-<trace-id>-<span-id>-00"
}
```

**Gap-fill EH-03:** `type` and `traceId` are ASP.NET Core's own default `ValidationProblemDetails` fields, produced automatically by the framework — this document does not invent them and Phase 12 does not need to construct them manually. Consumers (the frontend, and any test asserting response shape) must only depend on `status`, `title`, and `errors` being present; `type`/`traceId` values may vary by environment/request and must not be asserted against a fixed string in tests.

Endpoint-specific field keys and exact message strings are defined in:
- `docs/features/feature-flight-search.md` Section 4.1 (search)
- `docs/features/feature-booking-flow.md` Section 7 (booking)

### 1.2 404 Not Found (FR-068)

Standard shape, for architectural completeness:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "The requested resource was not found.",
  "status": 404
}
```

**Gap-fill EH-02 — important scope note:** FR-068 is written generically ("a resource, e.g., flight by ID, booking by reference, is not found"), but the MVP's actual API surface consists of exactly two endpoints — `POST /api/search` and `POST /api/bookings` (per `docs/architecture/architecture-plan.md` Section 5) — **neither of which performs a lookup-by-identifier that could ever return "not found."** There is no `GET /api/bookings/{reference}` endpoint in this MVP (Out of Scope item 3 — no booking retrieval in the UI) and no `GET /api/flights/{id}` endpoint (AD-004 avoids opaque flight IDs entirely; AD-002 avoids a GET airports endpoint). **No 404 response is reachable by any MVP endpoint.** This document records the standard shape above only so a future endpoint (e.g., a booking-lookup endpoint, explicitly Out of Scope item 3) has an established, consistent shape to reuse — Phase 12 should not build any dead-code 404-handling path to satisfy FR-068, and Phase 13/14 should not expect to find a 404 test scenario to exercise in this MVP. This is flagged as an **open item for Phase 11 (Spec Readiness Check)** to explicitly acknowledge, not something requiring Human PO action (the underlying scope boundary — no booking retrieval — is already approved in Section 7 item 3).

### 1.3 500 Internal Server Error — Unhandled Exception (FR-069, BR-011)

Produced **only** by `ApiExceptionMiddleware` (BL-016) — the single, centralized location for this response shape across the entire application (BR-011, DP-007). No controller or service method constructs a 500 body directly.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An unexpected error occurred. Please try again later.",
  "status": 500,
  "traceId": "00-<trace-id>-<span-id>-00"
}
```

**Gap-fill EH-01:** the exact `title` text above. No exception type name, message, stack trace, or internal file path may ever appear in this body, in any environment (FR-069, NFR-SEC-002) — this is a hard requirement, not a Gap-fill; only the specific wording of the generic title is a Gap-fill decision.

---

## 2. Centralized Exception Middleware Behavior (BR-011, DP-007)

`ApiExceptionMiddleware` (`SkyRoute.Api/Middleware/ApiExceptionMiddleware.cs`, BL-016):

- Registered **first** in the ASP.NET Core pipeline (`app.UseMiddleware<ApiExceptionMiddleware>()`, before routing/endpoint execution), so it wraps every downstream request.
- Wraps the remainder of the pipeline in a `try`/`catch`. On any unhandled exception reaching it:
  1. Logs the exception (message + type + stack trace **server-side only**, at `Error` level) — this server-side log **may** include the stack trace; the *response body* never does.
  2. Writes the Section 1.3 JSON body with HTTP status `500`.
  3. Does not rethrow — the response is fully handled here.
- Does **not** catch or handle:
  - 400s from `ValidationProblem(...)` — these are normal controller return values, not exceptions (BR-011).
  - Provider-level exceptions inside `FlightAggregatorService.SafeInvokeAsync` — those are caught and neutralized at the aggregation layer itself, before they could ever reach this middleware (see `docs/features/feature-provider-aggregation.md` Section 6) — a provider failure must never produce a 500 (FR-070).
- Individual controllers (`SearchController`, `BookingController`) contain no `try`/`catch` around their own service calls for the purpose of producing a 500 body — any exception a service method allows to propagate is intentionally left to reach this middleware (DP-005, BR-011).

---

## 3. Complete List of 400/404/500 Scenarios

### 3.1 `POST /api/search`

| Scenario | Status | Field Key | Detail Reference |
|---|---|---|---|
| Origin missing/malformed | 400 | `origin` | `docs/features/feature-flight-search.md` §4.1 |
| Destination missing/malformed | 400 | `destination` | same |
| Origin equals destination | 400 | `destination` | same |
| Origin/destination unknown code | 400 | `origin`/`destination` | same |
| Departure date missing/malformed | 400 | `departureDate` | same |
| Departure date in the past | 400 | `departureDate` | same |
| Passenger count missing/out of 1–9 | 400 | `passengerCount` | same |
| Cabin class missing/invalid | 400 | `cabinClass` | same |
| Trip type missing/not `"OneWay"` | 400 | `tripType` | same |
| (Not an error) Zero results | 200, empty array | — | `docs/features/feature-flight-search.md` §5.2 |
| Unhandled exception (e.g., a bug in validation or aggregation code outside the provider fault-isolation boundary) | 500 | — | Section 1.3 above |
| Any resource lookup-by-ID failure | **N/A — unreachable** | — | Section 1.2 (Gap-fill EH-02) |

Note: a single provider throwing is explicitly **not** in this list as an error scenario — per BR-007/FR-070, it always results in `200 OK` with the surviving provider's results (see `docs/features/feature-provider-aggregation.md` Section 6).

### 3.2 `POST /api/bookings`

| Scenario | Status | Field Key | Detail Reference |
|---|---|---|---|
| `passengerCount` ≠ `passengers.length` | 400 | `passengerCount` | `docs/features/feature-booking-flow.md` §7 |
| Passenger full name invalid | 400 | `passengers[{i}].fullName` | same |
| Passenger email invalid | 400 | `passengers[{i}].email` | same |
| Passenger document number invalid for resolved route type | 400 | `passengers[{i}].documentNumber` | same |
| Submitted `documentType` mismatches server-resolved route type | 400 | `passengers[{i}].documentType` | same |
| Flight snapshot missing a required field | 400 | `flight` | same |
| Booking reference collision exhausts retry cap (Gap-fill BF-03, `docs/features/feature-booking-flow.md` §6) | 500 | — | Section 1.3 above — extremely low probability, not an anticipated real path |
| Unhandled exception (any other unexpected failure) | 500 | — | Section 1.3 above |
| Any resource lookup-by-reference failure | **N/A — unreachable** | — | Section 1.2 (Gap-fill EH-02) |

**Note on client-submitted values not being trusted:** neither endpoint returns a validation error for a client-submitted `pricePerPassenger`, `totalPrice`, or `documentType`/route-type mismatch being merely *inconsistent with what the server would compute* in the pricing case — pricing is silently recomputed server-side (`docs/features/feature-booking-flow.md` §5 step 4, BR-006, NFR-DATA-002), never rejected. Only the `documentType`-vs-resolved-route-type mismatch is treated as a validation error (§5 step 3) because there is no server-side "correct" value to silently substitute for a wrong document type/number pairing — the user must correct it.

---

## 4. Frontend Error Handling (FR-071, FR-072, DP-010)

- Every Angular HTTP-calling service (`FlightSearchService`, `BookingService` — Angular) catches HTTP errors at the service boundary and maps them to either (a) a structured, still-typed error object carrying the `errors` dictionary from Section 1.1, for 400s that a form should render field-by-field, or (b) a single user-facing string, for 500s and network failures (per `docs/features/feature-flight-search.md` §5.3 and `docs/features/feature-booking-flow.md` — booking uses the equivalent pattern: on booking failure, `"We couldn't complete your booking. Please try again."` as the generic message, mirroring the search error wording pattern established in the flight-search spec).
- Components never render a raw HTTP status code, exception message, or stack trace (FR-071) — every error surface in the UI uses one of the fixed strings defined in this document or the two endpoint-specific feature specs.
- Network-level failures (timeout, connection refused — no HTTP response at all) are distinguished from a 500 response only insofar as they use the network-specific message (`docs/features/feature-flight-search.md` §5.3); functionally, both are handled by the same "generic failure" code path in the calling service (DP-010's single HTTP-boundary rule) — there is no behavioral difference required beyond the message text (FR-072 is Should Have).

---

## 5. Compliance Statement

This document introduces no new response shape, endpoint, or status code beyond `docs/requirements.md` v1.4 (FR-060–072, BR-011) and `docs/architecture/architecture-plan.md` v1.0 Section 3.6/Section 6. The exact `title` text for the 500 response, and the explicit acknowledgement that FR-068's 404 path is unreachable given this MVP's two-endpoint surface, are labelled Gap-fill decisions (Section 6).

---

## 6. Gap-Fill Decisions (Summary)

| ID | Decision | Rationale |
|---|---|---|
| GAP-EH-01 | Exact 500-response `title` text: `"An unexpected error occurred. Please try again later."` | FR-069/BR-011 mandate the absence of internal detail but not the exact user-facing wording. |
| GAP-EH-02 | FR-068's 404 scenario is not reachable by any MVP endpoint (no GET-by-identifier endpoint exists for either flights or bookings per AD-002/AD-004 and Out of Scope item 3). The 404 shape is documented for completeness/future-readiness only; no code path needs to be built to produce it in this MVP. | Prevents Phase 12 from wastefully building dead-code 404 handling, and prevents Phase 13/14 from expecting a test scenario that has no corresponding endpoint to exercise. Flagged explicitly for Phase 11 (Spec Readiness Check) acknowledgement, though the underlying scope boundary (no booking retrieval) is already approved. |
| GAP-EH-03 | Consumers (frontend, tests) must rely only on `status`/`title`/`errors` in the 400/500 envelope; `type`/`traceId` are ASP.NET Core framework defaults, not custom fields, and must not be asserted against fixed values. | Avoids Phase 13 writing brittle tests against framework-generated, environment-variable values that were never a deliberate part of this design. |

---

*End of Feature Specification — Error Handling and Validation v1.0.*
