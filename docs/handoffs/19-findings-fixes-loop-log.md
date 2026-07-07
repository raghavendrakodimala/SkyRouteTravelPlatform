# Phase 19 — Findings Fixes Loop Log

## Entry 1 — senior-full-stack-engineer — 2026-07-07

- **Branch:** `sdlc/19-findings-fixes-skyroute-mvp`
- **Phase:** 19 — Fix review/test findings (consolidation sweep for `QA-*` findings)
- **Scope:** exactly the four Open QA findings from `docs/testing/execution/phase-14-test-execution-summary.md` (Section 7). No review-report statuses were edited — the functional-tester re-verifies in Phase 20.

---

### QA-001 (Medium) — `"passengers": null` caused unhandled 500 — **VERDICT: FIXED (validator guard pre-existed; end-to-end 400 now proven by new integration tests)**

**Verification against current tree:** `BookingRequestValidator.ValidateStructure` already null-coalesces `request.Passengers` (`?? new List<PassengerRequest>()`) and null-guards `request.Flight` (comments cite BR-011 / HO-012A decision 7), with unit coverage in `ValidateStructure_NullFlightAndNullPassengers_ReturnsFlightError_DoesNotThrow`. `BookingService.ValidateDocuments`/`CreateBookingAsync` still dereference `request.Passengers` directly, but are unreachable with null passengers: structural validation always fails first (`passengerCount` mismatch when count ≥ 1, or the 1–9 bound when count = 0), so the controller returns 400 before the service runs. What was missing was end-to-end proof against the exact crafted raw-JSON payload from the finding (typed `BookingRequest` test objects cannot express `"passengers": null`).

**Change:** added two full-stack integration tests to `tests/SkyRoute.Api.IntegrationTests/Controllers/BookingControllerTests.cs`, posting raw JSON via `StringContent`:

- `CreateBooking_ExplicitNullPassengers_Returns400ValidationProblem_Not500` — complete valid flight snapshot + `"passengerCount": 1` + `"passengers": null` → asserts 400, `ValidationProblemDetails` body, and a passenger-related field error key.
- `CreateBooking_ExplicitNullFlightAndNullPassengers_Returns400ValidationProblem_Not500` — every nullable top-level member (`flight`, `passengers`) explicitly nulled at once (sibling nullable coverage) → asserts 400 with non-empty field errors.

**Evidence:** both tests pass — `SkyRoute.Api.IntegrationTests.dll`: Failed: 0, Passed: 15, Total: 15 (was 13 before this entry).

---

### QA-002 (Low) — `ApiExceptionMiddleware` content type overwritten to `application/json` — **VERDICT: FIXED**

**Verification against current tree:** bug confirmed present — `context.Response.ContentType = "application/problem+json"` was assigned before `WriteAsJsonAsync(body)`, which (with no explicit `contentType` argument) unconditionally overwrites the header with `application/json; charset=utf-8`. The pre-existing test `InvokeAsync_WhenNextThrows_SetsJsonContentType` explicitly documented the buggy behavior and deferred the fix to this phase.

**Change:** `src/SkyRoute.Api/Middleware/ApiExceptionMiddleware.cs` — removed the dead `ContentType` assignment and pass the content type through the write call itself: `WriteAsJsonAsync(body, options: null, contentType: "application/problem+json")`. Updated the middleware test (renamed to `InvokeAsync_WhenNextThrows_SetsProblemJsonContentType`) to assert `Assert.Equal("application/problem+json", context.Response.ContentType)` — a strictly stronger assertion than the old `StartsWith("application/json")` documenting-the-bug check.

**Evidence:** `tests/SkyRoute.Api.IntegrationTests/Middleware/ApiExceptionMiddlewareTests.cs` — all 4 middleware tests pass within the 15/15 integration run; the other three (500 status, generic body with no exception detail, no rethrow) unchanged and green, proving no behavioral regression in the error contract.

---

### QA-004 (Low) — same-airport inline error paragraph dead code — **VERDICT: MOOT (no change; finding overtaken by A11Y-007/A11Y-008 rework)**

**Verification against current tree:** the finding was filed when the submit button was natively `disabled` while the form was invalid, making the paragraph unreachable. The form was since reworked (A11Y-007/A11Y-008): `frontend/src/app/features/search/search-form/search-form.component.html` uses `[attr.aria-disabled]` only (comment block at the submit button, lines 72–76 — "never native `disabled`"), so an invalid submit reaches `onSubmit()`, which sets `submitted(true)`; the paragraph at lines 36–38 renders under `@if (submitted() && sameAirportSelected())`. The branch is therefore REACHABLE and load-bearing.

**Evidence (no change made):** existing spec `search-form.component.spec.ts` — `'surfaces the same-airport alert on submit instead of silently blocking Search (US-001 AC8, A11Y-008)'` asserts the button has no `disabled` attribute, submits with LHR→LHR, and asserts the alert text `'Origin and destination airports must be different.'` is rendered and the API is not called. Passing in this session's 181/181 Vitest run.

---

### QA-005 (Low) — `passengerCount` submitted as JSON string via `<select>` — **VERDICT: MOOT (no change; fixed by construction)**

**Verification against current tree:** the `<select>` no longer exists. `frontend/src/app/features/search/search-form/search-form.component.ts` line 85 submits the numeric literal `passengerCount: 1` (PO decision 2026-07-07 — passenger count is determined during booking). No string-typed path remains.

**Evidence (no change made):** existing specs in `search-form.component.spec.ts` — `'renders no passenger field at all — passenger count is determined during booking (PO decision 2026-07-07)'` and `'always submits SearchRequest.passengerCount as the numeric constant 1 (PO decision 2026-07-07)'` (the latter uses strict matching where number `1` would not match string `'1'`, pinning the type). Both passing in this session's 181/181 Vitest run.

---

### Files changed

- `src/SkyRoute.Api/Middleware/ApiExceptionMiddleware.cs` (QA-002 fix)
- `tests/SkyRoute.Api.IntegrationTests/Middleware/ApiExceptionMiddlewareTests.cs` (QA-002 test strengthened)
- `tests/SkyRoute.Api.IntegrationTests/Controllers/BookingControllerTests.cs` (QA-001: +2 raw-JSON integration tests)
- No frontend source/spec changes (QA-004/QA-005 moot).

### Validation commands and results (this session, 2026-07-07)

- `dotnet build` (repo root): Build succeeded — 0 Warning(s), 0 Error(s).
- `dotnet test` (repo root): `SkyRoute.Application.Tests` Failed: 0, Passed: **157**, Total: 157; `SkyRoute.Api.IntegrationTests` Failed: 0, Passed: **15**, Total: 15.
- `npm test -- --watch=false` (frontend/, run for moot-verdict evidence despite no frontend change): **181 passed (181)**, 17/17 test files.

### Out of scope, noticed, not touched

- `docs/testing/execution/phase-14-test-execution-summary.md` statuses left as-is (Open) per phase rules — functional-tester re-verifies and updates in Phase 20.
- The Phase 14 summary's counts (114 backend / 145 frontend) are historical; suites have since grown through Phases 15–18 fix loops to 172 backend / 181 frontend — a documentation-currency note only.
