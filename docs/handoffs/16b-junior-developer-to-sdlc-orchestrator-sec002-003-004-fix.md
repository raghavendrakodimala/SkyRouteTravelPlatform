# Handoff 16b — Junior Developer to SDLC Orchestrator — SEC-002/003/004 Fix

| Field | Value |
|---|---|
| Handoff ID | 16b |
| Date | 2026-07-07 |
| Branch | `sdlc/16-security-review-skyroute-mvp` |
| Phase | Phase 16 (fix loop) — Security Review findings SEC-002, SEC-003, SEC-004 |
| From agent | junior-developer |
| To agent | sdlc-orchestrator |
| Status | Complete — all three findings fixed, build/tests green |

## Work Completed

Fixed the three Medium/Low findings from `docs/reviews/security-review-phase-16.md` (SEC-001, High, was already fixed by a prior agent and left untouched — confirmed by reading `BookingRequestValidator.cs`/`CabinClasses.cs` current state before editing).

### SEC-002 (Medium) — no upper bound on booking passenger count

Added a passenger-count range check to `BookingRequestValidator.ValidateStructure`, mirroring `SearchRequestValidator.ValidatePassengerCount` exactly (same 1–9 bound, same error message text: "Passenger count must be a whole number between 1 and 9."). This check runs alongside (does not replace) the existing `PassengerCount != passengers.Count` mismatch check, and does not short-circuit (FR-063 convention preserved).

### SEC-003 (Low) — no HTTP security response headers / no frontend CSP

- `src/SkyRoute.Api/Program.cs`: added an inline `app.Use(...)` middleware (registered right after `ApiExceptionMiddleware`, before `UseHttpsRedirection()`) that sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and `Referrer-Policy: no-referrer` on every response. HSTS was deliberately skipped per the task brief — this is local-only HTTP dev, not TLS-terminated, so `Strict-Transport-Security` would have no meaningful effect.
- `frontend/src/index.html`: added a `<meta http-equiv="Content-Security-Policy">` tag with `default-src 'self'` plus `connect-src 'self' http://localhost:5094` (the API base URL from `frontend/src/environments/environment.ts`), `style-src 'self' 'unsafe-inline'` (Angular CLI scaffold emits inline component styles), `img-src 'self' data:`, `frame-ancestors 'none'`, `base-uri 'self'`, `form-action 'self'`.
- Added `tests/SkyRoute.Api.IntegrationTests/Middleware/SecurityHeadersTests.cs`, following the `IClassFixture<SkyRouteApiFactory>` pattern used by `SearchControllerTests`/`BookingControllerTests`, asserting all three headers are present on a real `POST /api/search` response through the full pipeline.

### SEC-004 (Low) — `EmailPattern` has no length upper bound

Tightened `DocumentPatterns.EmailPattern` itself (rather than adding a separate call-site length check) by prepending a `(?=.{1,254}$)` zero-width lookahead, bounding the overall address to 254 characters (RFC 5321 practical max) before the existing local-part/domain-part matching runs. This follows the same "explicit numeric bound directly in the pattern" convention already used by `PassportPattern` (`{6,9}`), `NationalIdPattern` (`{5,20}`), and `FullNamePattern` (`.{2,100}`) in the same file, rather than introducing an inconsistent second style (a separate `Length <= 254` guard in `BookingRequestValidator`). No call-site change was needed in `BookingRequestValidator.cs` — the existing `EmailRegex.IsMatch(passenger.Email)` call now enforces the bound automatically.

Added two tests to `BookingRequestValidatorTests.cs`: an oversized (255-char) email is rejected, and a boundary-valid (exactly 254-char) email still passes.

## Artifacts Created or Updated

- Updated: `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (SEC-002 range check)
- Updated: `src/SkyRoute.Application/Validation/DocumentPatterns.cs` (SEC-004 `EmailPattern` bound)
- Updated: `src/SkyRoute.Api/Program.cs` (SEC-003 security headers middleware)
- Updated: `frontend/src/index.html` (SEC-003 CSP meta tag)
- Updated: `tests/SkyRoute.Application.Tests/Validation/BookingRequestValidatorTests.cs` (SEC-002: 3 new tests; SEC-004: 2 new tests)
- Created: `tests/SkyRoute.Api.IntegrationTests/Middleware/SecurityHeadersTests.cs` (SEC-003: 1 new test)

Not touched (per task instruction): `docs/reviews/security-review-phase-16.md`, `docs/handoffs/current-handoff.md`, `docs/handoffs/handoff-index.md`, `docs/handoffs/workflow-state.md`.

Not touched (pre-existing, from prior SEC-001 fix by another agent, verified but left as-is): `src/SkyRoute.Application/Validation/CabinClasses.cs`, the SEC-001-related portions of `BookingRequestValidator.ValidateStructure`, `src/SkyRoute.Application/Validation/SearchRequestValidator.cs` (uncommitted `CabinClasses` refactor already present before I started).

## Decisions Made

- SEC-004 fixed via regex-embedded length bound (lookahead) rather than a separate call-site guard, to match the file's existing convention (other three patterns bound length directly in their quantifiers, not via external code).
- SEC-003 CSP is scoped to this MVP's known single backend origin (`http://localhost:5094`, read from `environment.ts`) rather than a wildcard or overly permissive policy.
- HSTS intentionally omitted per task brief (local-only HTTP dev).
- New integration test placed under `tests/SkyRoute.Api.IntegrationTests/Middleware/` (not `Controllers/`) since it verifies pipeline-level middleware behavior, not endpoint-specific business logic, while still reusing an existing endpoint (`POST /api/search`) to drive a realistic request through the full pipeline.

## Open Questions

None.

## Risks and Impediments

None identified. All three findings are defense-in-depth/hardening fixes with no behavior change to existing valid requests (verified by full regression run below).

## Required Next Agent Action

Security-reviewer (or orchestrator acting on its behalf) should update `docs/reviews/security-review-phase-16.md` status for SEC-002, SEC-003, SEC-004 from Open to Resolved, referencing this handoff and the commits it produces. SEC-001 (High) remains Open pending the separate human Product Owner approval gate noted in that review's Overall Recommendation — this handoff does not change that.

## Completion Criteria for Next Step

- Security reviewer confirms SEC-002/003/004 fixes satisfy the "Required fix" text for each finding in the review report.
- Orchestrator updates `docs/handoffs/current-handoff.md`, `handoff-index.md`, `workflow-state.md`.
- Orchestrator decides on commit/merge per CLAUDE.md §17/§21 (not performed by this agent).

## Relevant Files

- `src/SkyRoute.Application/Validation/BookingRequestValidator.cs`
- `src/SkyRoute.Application/Validation/DocumentPatterns.cs`
- `src/SkyRoute.Application/Validation/SearchRequestValidator.cs` (untouched carry-over from prior SEC-001 fix)
- `src/SkyRoute.Api/Program.cs`
- `frontend/src/index.html`
- `frontend/src/environments/environment.ts` (read, not modified — API base URL source for CSP `connect-src`)
- `tests/SkyRoute.Application.Tests/Validation/BookingRequestValidatorTests.cs`
- `tests/SkyRoute.Api.IntegrationTests/Middleware/SecurityHeadersTests.cs`
- `docs/reviews/security-review-phase-16.md` (read only, not modified)

## Build/Test Evidence

`dotnet build` (full solution): Build succeeded, 0 Warning(s), 0 Error(s).

`dotnet test` (full solution):
- `SkyRoute.Application.Tests.dll`: Passed! Failed: 0, Passed: 122, Skipped: 0, Total: 122
- `SkyRoute.Api.IntegrationTests.dll`: Passed! Failed: 0, Passed: 12, Skipped: 0, Total: 12

`npm run build` (frontend, `ng build`): "Application bundle generation complete" — 0 errors, initial bundle 311.14 kB raw / 80.33 kB estimated transfer. Frontend unit tests were not run per task scope (no `.spec.ts` files were touched — only `index.html`'s `<head>` meta tag changed).
