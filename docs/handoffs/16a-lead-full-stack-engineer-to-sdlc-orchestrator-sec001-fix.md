# Handoff 16a — Lead Full Stack Engineer to SDLC Orchestrator — SEC-001 Fix

| Field | Value |
|---|---|
| Handoff ID | 16a |
| Date | 2026-07-07 |
| Branch | `sdlc/16-security-review-skyroute-mvp` |
| Phase | Phase 16 fix loop (SEC-001 minimal mitigation) |
| From agent | lead-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete |

## Work Completed

Implemented the "at minimum" mitigation for **SEC-001 (High)** from `docs/reviews/security-review-phase-16.md` — `POST /api/bookings` previously trusted a client-supplied flight-fare snapshot (`PricePerPassenger`, `BaseFare`, `CabinClass`) with only presence checks, never range/allow-list checks, in `BookingRequestValidator`.

Changes:

1. Created a shared cabin-class allow-list constant so `SearchRequestValidator` and `BookingRequestValidator` reference one source of truth instead of duplicating the list (per DP-015, per the review's explicit instruction).
2. Moved `SearchRequestValidator`'s private `ValidCabinClasses` array out to this shared location and updated its one call site to use it.
3. Added three new checks to `BookingRequestValidator.ValidateStructure`, gated on `request.Flight is not null` (to avoid NPE) and deliberately **not** gated on flight-completeness, so a partially-complete flight snapshot still reports every applicable structural problem, consistent with this class's documented "does not short-circuit" (FR-063) behavior:
   - `PricePerPassenger <= 0` → field key `flight.pricePerPassenger`, message "Price per passenger must be greater than zero."
   - `BaseFare <= 0` (only when `BaseFare` has a value — it is not a required field on the snapshot per `IsFlightSnapshotComplete`, so `null` is not an error) → field key `flight.baseFare`, message "Base fare must be greater than zero."
   - `CabinClass` not in the shared allow-list (only checked when non-null/non-whitespace, since emptiness is already reported via the existing `flight` "incomplete" error) → field key `flight.cabinClass`, message "Cabin class must be one of: Economy, Business, First Class."
4. Added 9 new unit tests to the existing `BookingRequestValidatorTests.cs` covering zero/negative `PricePerPassenger`, zero/negative `BaseFare`, `BaseFare = null` (no error), invalid/unknown `CabinClass` values (4 theory cases), valid `CabinClass` values from the allow-list (3 theory cases), and a happy-path case with valid price/base-fare/cabin-class values still passing with an empty error dictionary.
5. Ran `dotnet build` (0 warnings, 0 errors) and `dotnet test` for the full solution — **127/127 passed, 0 failed, 0 skipped** (116 in `SkyRoute.Application.Tests`, 11 in `SkyRoute.Api.IntegrationTests`). No existing test broke; scanned all other test files referencing `CabinClass`/`PricePerPassenger`/`BaseFare` and confirmed none relied on an invalid cabin class or non-positive price value.

This is the review's own stated **minimal** fix (Required fix option (b) in the SEC-001 finding). Option (a) — full server-side price re-resolution against provider/aggregator pricing logic — was explicitly out of scope per the task brief and is not attempted here. `BookingService.CreateBookingAsync` still computes `TotalPrice` from the client-submitted `PricePerPassenger`; that value is now guaranteed positive and the cabin class is now guaranteed to be one of the three valid values, but the price is still not cross-checked against what the aggregator/providers would actually charge for that flight. This residual gap is exactly what the review's Recommendation/Required-fix text already flags as the structurally correct follow-up (re-resolving price server-side, mirroring `RouteTypeResolver`'s pattern for route type) — it is not resolved by this change and should remain tracked under SEC-001 for the security-reviewer to assess when deciding the finding's final status (Resolved vs. Partially Resolved).

## Artifacts Created or Updated

Created:
- `src/SkyRoute.Application/Validation/CabinClasses.cs` — new shared static class holding `ValidCabinClasses` (allow-list constant), documented as the single source of truth referenced by both validators.
- `docs/handoffs/16a-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-fix.md` (this file).

Updated:
- `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` — added the three range/allow-list checks described above inside `ValidateStructure`.
- `src/SkyRoute.Application/Validation/SearchRequestValidator.cs` — removed its private `ValidCabinClasses` array; `ValidateCabinClass` now calls `CabinClasses.ValidCabinClasses.Contains(...)`.
- `tests/SkyRoute.Application.Tests/Validation/BookingRequestValidatorTests.cs` — added 9 new `[Fact]`/`[Theory]` tests under a new "flight-fare snapshot range/allow-list checks (SEC-001)" section, placed immediately before the existing null-flight defensive test.

Not touched (per instruction): `docs/reviews/security-review-phase-16.md`, SEC-002/003/004, `docs/handoffs/current-handoff.md`, `docs/handoffs/handoff-index.md`, `docs/handoffs/workflow-state.md`.

## Decisions Made

- Reused the existing `AddError`/`Dictionary<string, List<string>>`/`ToArrayDictionary` pattern already in `BookingRequestValidator` rather than inventing a new error shape, per the task brief.
- Used field keys `flight.pricePerPassenger`, `flight.baseFare`, `flight.cabinClass` (dotted, mirroring the existing `passengers[{i}].fullName` nested-field naming convention already used in this same method) rather than reusing the single `flight` key, so each specific problem is independently addressable/testable and does not collide with or get masked by the existing "Flight details are incomplete." presence-check message.
- Moved (not duplicated) the `ValidCabinClasses` allow-list to a new `CabinClasses` static class in the `Validation` namespace, following the same pattern as the existing `DocumentPatterns` static class in the same folder, since the review explicitly required reuse rather than copy-pasting (DP-015).
- Made the new checks independent of the existing flight-completeness gate (not nested inside an `else`) so they still fire even when other flight fields are also missing/invalid, consistent with this class's documented non-short-circuiting behavior (FR-063). Confirmed via the added tests that this does not produce unexpected interactions with the existing `flight` "incomplete" error.
- Left `BaseFare`'s optionality as-is (unchanged from current behavior) — only added a `>0` check when a value is present, per the task brief's explicit instruction ("If BaseFare is present on the model, the same > 0 check").

## Open Questions

- None from an implementation standpoint. The one substantive open question is a product/architecture one already flagged by the review itself: whether option (a) (full server-side price re-resolution) should be scheduled as a follow-up item, and whether the residual gap (price still not cross-checked against provider pricing) is an acceptable risk for this MVP. That decision belongs to the human Product Owner / security-reviewer per the review's own recommendation, not to this fix.

## Risks and Impediments

- None encountered. Build and full test suite are green.
- Residual risk (unchanged by this fix, already known/flagged by the review): a client can still submit an internally-consistent but fabricated positive `PricePerPassenger` (e.g., `$0.01` for a flight that should cost $500) with a valid cabin class string, and it will still be accepted and used to compute `TotalPrice`. This fix closes the zero/negative/invalid-cabin-class door specifically identified as the "minimal" required fix; it does not close full price-tampering exposure. This matches the review's own framing of option (b) as a partial mitigation, not a complete fix.

## Required Next Agent Action

Hand back to sdlc-orchestrator / security-reviewer to re-assess SEC-001's status (likely "Partially Resolved" given option (a) remains open, or "Resolved" if the human Product Owner/security-reviewer judges option (b) sufficient for MVP scope) and continue the Phase 16 fix-and-retest loop for the remaining findings (SEC-002/003/004) and any Phase 15 code-review findings tracked separately.

## Completion Criteria for Next Step

- Security-reviewer re-reads `src/SkyRoute.Application/Validation/BookingRequestValidator.cs`, `src/SkyRoute.Application/Validation/CabinClasses.cs`, and `tests/SkyRoute.Application.Tests/Validation/BookingRequestValidatorTests.cs` and updates SEC-001's status in `docs/reviews/security-review-phase-16.md`.
- Orchestrator updates `docs/handoffs/workflow-state.md` and `docs/handoffs/handoff-index.md` to reflect this handoff.

## Relevant Files

- `src/SkyRoute.Application/Validation/CabinClasses.cs` (new)
- `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (updated)
- `src/SkyRoute.Application/Validation/SearchRequestValidator.cs` (updated)
- `tests/SkyRoute.Application.Tests/Validation/BookingRequestValidatorTests.cs` (updated)
- `docs/reviews/security-review-phase-16.md` (read only, not modified — SEC-001 finding source)
