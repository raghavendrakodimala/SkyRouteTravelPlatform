# Handoff 16d — Lead Full Stack Engineer to SDLC Orchestrator — SEC-001 Full Fix

| Field | Value |
|---|---|
| Handoff ID | 16d |
| Date | 2026-07-07 |
| Branch | `sdlc/16-security-review-skyroute-mvp` |
| Phase | Phase 16 fix loop (SEC-001 residual gap — option (a) full server-side price re-resolution) |
| From agent | lead-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete |

## Work Completed

### Exploration (before any code change)

Read `docs/reviews/security-review-phase-16.md` (SEC-001 finding + Re-Verification Note), `docs/handoffs/16a-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-fix.md`, and `docs/handoffs/16c-security-reviewer-to-sdlc-orchestrator-sec-reverification.md` for full history, then read the actual code to answer the two exploration questions in the task brief:

1. **How search prices a flight offer, and whether pricing is deterministic/re-priceable.** Read `GlobalAirProvider.cs` and `BudgetWingsProvider.cs` in full. Both hold a small, fixed, hardcoded `Schedule` (4 flights each) keyed by `FlightNumber`, with a fixed `EconomyBaseFare` per flight. `SearchAsync` computes `baseFare = EconomyBaseFare x CabinClassMultiplier(cabinClass)`, then calls a private, named pricing method (`ApplyGlobalAirPricing` / `ApplyBudgetWingsPricing`) that applies BR-001 (`x1.15`) or BR-002 (`max(x0.90, 29.99)`). **Pricing is fully deterministic given `Provider + FlightNumber + CabinClass`** — departure date only affects the *calendar date* of the returned `DepartureDateTime`/`ArrivalDateTime` timestamps, never the price. There is no randomization, no time-of-day/session dependence, and no persisted offer-to-price mapping is needed because the fare can be re-derived on demand from the same three inputs, any number of times, with an identical result. `FlightAggregatorService` just fans this out to all registered `IFlightProvider`s and flattens the results — no separate pricing logic lives there.
2. **How `BookingFlightRequest` reaches `BookingService`.** Confirmed exactly as described in the finding: `BookingController.CreateBooking` runs `BookingRequestValidator.ValidateStructure` (presence + the Phase-16-minimal-fix range/allow-list checks) then calls `BookingService.CreateBookingAsync` directly with the client-supplied `BookingRequest`, including the full `BookingFlightRequest` snapshot (`Provider`, `FlightNumber`, `Origin`, `Destination`, dates, `CabinClass`, `BaseFare`, `PricePerPassenger`) verbatim — nothing re-resolves any of it against a provider before this fix. `RouteTypeResolver` was the one existing precedent for "resolve authoritatively server-side instead of trusting the snapshot," used only for route type, not price.

**Conclusion:** this architecture *does* support a clean fix without a redesign — pricing is deterministic and keyed by fields already present on `BookingFlightRequest` (`Provider`/`FlightNumber`/`CabinClass`), so the server can re-derive the authoritative fare on demand at booking time exactly as the review's Required-fix option (a) and Recommendation text describe ("mirroring how `RouteTypeResolver` already re-resolves route type authoritatively server-side"). No blocker was found. Implemented the full fix.

### Fix implemented

1. **`src/SkyRoute.Application/Interfaces/IFlightProvider.cs`** — added `bool TryResolveFare(string flightNumber, string cabinClass, out decimal baseFare, out decimal pricePerPassenger)` to the provider contract. Returns `false` (zeroed out-values) for an unknown flight number — a fabricated flight number cannot be used to bypass verification.
2. **`src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs`** / **`BudgetWingsProvider.cs`** — implemented `TryResolveFare` by looking up the schedule entry by `FlightNumber` (ordinal match) and re-running the exact same cabin-multiplier + `ApplyGlobalAirPricing`/`ApplyBudgetWingsPricing` logic `SearchAsync` already uses. No new pricing rule was invented; the existing private pricing methods are reused as-is.
3. **New: `src/SkyRoute.Application/Services/FlightFareResolver.cs`** — a new resolver, deliberately mirroring `RouteTypeResolver`'s shape (constructor takes the collaborators it needs, one public `TryResolveFare` method). Matches `providerName` against the registered `IEnumerable<IFlightProvider>` (ordinal), then delegates to that provider's `TryResolveFare`. Returns `false` if the provider name itself is unknown or any identifying field is null/blank.
4. **`src/SkyRoute.Application/Validation/BookingRequestValidator.cs`** — added `ValidateFare(BookingRequest request, bool fareResolved, decimal expectedBaseFare, decimal expectedPricePerPassenger)`, mirroring the existing `ValidateDocuments(request, routeType)` shape (resolution happens in the service; this method only compares the already-resolved fact against the request). Produces field-keyed errors `flight.flightNumber` (fare could not be resolved at all), `flight.pricePerPassenger`, and `flight.baseFare` (each independently, non-short-circuiting, consistent with FR-063).
5. **`src/SkyRoute.Application/Services/BookingService.cs`** — added a new step (3b) between the existing document-validation throw (step 3) and total-price computation (step 4): calls `FlightFareResolver.TryResolveFare`, then `BookingRequestValidator.ValidateFare`, and throws the existing `BookingValidationException` (same exception type/handling path as document errors — caught by `BookingController`, converted to a `ValidationProblem` 400, never reaches `ApiExceptionMiddleware`) if there is any mismatch. When the check passes, both the persisted `BookingFlightSnapshot.PricePerPassenger` and the `TotalPrice` computation now use the **server-resolved** value, not the client-submitted one (identical by definition whenever the check passes, but this keeps the authoritative source unambiguous, exactly mirroring how `RouteTypeResolver`'s resolved `routeType` — not any client-submitted route type — is what actually gets used downstream).
6. **`src/SkyRoute.Api/Program.cs`** — registered `FlightFareResolver` as `AddScoped` (it depends on `IEnumerable<IFlightProvider>`, which are themselves scoped, so it cannot be a singleton).
7. **Test doubles updated to satisfy the widened `IFlightProvider` interface:** `tests/SkyRoute.Application.Tests/TestDoubles/StubFlightProvider.cs` and `tests/SkyRoute.Api.IntegrationTests/TestDoubles/ThrowingFlightProvider.cs` both got a trivial `TryResolveFare` that always returns `false` — both doubles are used only for search/fault-isolation tests, never booking-flow tests, so this is a compile-satisfying no-op, not new test-relevant behavior.
8. **`tests/SkyRoute.Application.Tests/TestDoubles/FakeBookingStore.cs`** — added a `CreatedBookings` read-only accessor so new tests can assert a rejected booking never reached persistence at all (not just that a 4xx was returned).

### Decisions made

- **Rejection, not silent price substitution, on mismatch.** Chose to throw `BookingValidationException` (reusing the exact existing document-validation convention) rather than silently overwriting the client's price with the resolved one and proceeding to a 201. Rationale: (a) this is the pattern the codebase's own `ValidateDocuments` → `BookingController` catch → `ValidationProblem` already establishes for "business-rule validation outcome, not an unhandled exception" (per `BookingValidationException`'s own doc comment); (b) silently substituting the price would mask a tampering attempt as if it were a normal, successful booking, which is worse from a security-observability standpoint than a clear 400; (c) the review's own Required-fix text explicitly names both options and this codebase already has a working, tested convention for "reject" (option matching `ValidateDocuments`), not for "silently correct and proceed."
- **Exact match, no tolerance.** Pricing is fully deterministic given `Provider+FlightNumber+CabinClass` (confirmed by exploration above) and both the provider and the resolver use the exact same `Math.Round(..., 2, MidpointRounding.AwayFromZero)` rule, so there is no floating-point or independent-rounding source of legitimate divergence. An exact `!=` decimal comparison is therefore correct and was used for both `PricePerPassenger` and `BaseFare` — any tolerance would only reopen a (smaller) version of the same gap.
- **Placed the fare check after document validation, before total-price computation**, not before route-type resolution. This preserves the existing `CreateBooking_DocumentRouteMismatch_Returns400ViaValidationProblem_Not500` integration test's behavior unchanged (a document-type mismatch is still reported first when both problems exist in the same request) while still gating the price computation and persistence steps that follow.
- **Unknown flight number is treated as a validation failure (`flight.flightNumber`), not silently ignored or defaulted.** Without a resolvable schedule entry there is no authoritative fare to compare against, so trusting the client's snapshot in that case would reopen exactly the gap being closed.
- **Ordinal string comparisons** for both provider-name and flight-number matching in `FlightFareResolver`/provider `TryResolveFare`, consistent with these being fixed, server-defined identifiers rather than user free text (same rationale already used elsewhere in this codebase, e.g. `BookingRequestValidator`'s `DocumentType` comparison).

### Test evidence

Ran `dotnet build` and `dotnet test` for the full solution twice (once after the minimal production-code change, once after adding all new tests) — final run:

```
dotnet build
  Build succeeded.
      0 Warning(s)
      0 Error(s)

dotnet test
  Passed! - Failed: 0, Passed: 146, Skipped: 0, Total: 146  - SkyRoute.Application.Tests.dll (net10.0)
  Passed! - Failed: 0, Passed:  13, Skipped: 0, Total:  13  - SkyRoute.Api.IntegrationTests.dll (net10.0)
```

**159/159 tests passed, 0 failed, 0 skipped** — includes all 134 previously-passing tests (confirmed unchanged in behavior, only two existing `BookingServiceTests` fixtures were updated to use a real, matching fare instead of an arbitrary placeholder price — see below) plus 25 new tests:

- `tests/SkyRoute.Application.Tests/Services/BookingServiceTests.cs` (+5): `CreateBookingAsync_FabricatedPositivePricePerPassenger_ThrowsBookingValidationException` (the exact scenario named in the task — same GlobalAir/GA101/LHR-JFK/Economy/International inputs as the existing happy-path test, price fabricated to `$0.01`), `CreateBookingAsync_FabricatedHigherPricePerPassenger_ThrowsBookingValidationException` (inflated, not just deflated, fabrication), `CreateBookingAsync_FabricatedBaseFare_ThrowsBookingValidationException`, `CreateBookingAsync_UnknownFlightNumberForProvider_ThrowsBookingValidationException`, `CreateBookingAsync_BudgetWingsFare_ResolvedAndAcceptedWhenItMatches_ReturnsExpectedResponse` (positive control for the second provider). Each fabricated-price test also asserts `store.CreatedBookings` is empty — the rejected booking never reached persistence.
- `tests/SkyRoute.Api.IntegrationTests/Controllers/BookingControllerTests.cs` (+1): `CreateBooking_FabricatedPricePerPassenger_Returns400WithFlightPriceError` — full HTTP round-trip through `SkyRouteApiFactory`, same inputs as `CreateBooking_InternationalHappyPath_Returns201WithDataMinimizedPassengers` but `PricePerPassenger = 0.01m`; asserts `400` and a `flight.pricePerPassenger` `ValidationProblemDetails` error key.
- `tests/SkyRoute.Application.Tests/Providers/GlobalAirProviderTests.cs` (+6) and `BudgetWingsProviderTests.cs` (+3): direct unit tests for the new `TryResolveFare` method on both real providers (known flight/cabin-class combinations matching the existing `SearchAsync` worked examples, unknown flight number, case-sensitivity).
- New file `tests/SkyRoute.Application.Tests/Services/FlightFareResolverTests.cs` (+10): direct unit tests for `FlightFareResolver` composed with the real providers — known GlobalAir/BudgetWings flights resolve correctly, an unknown provider name fails closed, a flight number that belongs to the *other* provider fails closed (not a false-positive cross-provider match), and every combination of missing identifying fields fails closed without throwing.

**Existing tests updated (not behavior changes to production logic — fixture corrections only):** Two pre-existing `BookingServiceTests` fixtures (`CreateBookingAsync_DomesticRoute_HappyPath_ReferenceUsesDomPrefix`, `CreateBookingAsync_TotalPrice_IsServerRecomputedFromPricePerPassengerTimesCount`) had previously used arbitrary placeholder `pricePerPassenger` values (`60.00`, `115.00`) that never matched GA101/Economy's real fare (`287.50`) — harmless before this fix because nothing checked it, but now correctly rejected by the new fare validation. Updated both to use the real fare (either by dropping the override to fall back to the fixture's already-correct `287.50` default, or by recalculating the expected `TotalPrice` for the non-trivial-count test: `3 x 287.50 = 862.50`). Confirmed neither test's actual purpose (DOM reference prefix; total-price multiplication for a non-trivial passenger count) was weakened — only the specific price value used was corrected to a real one.

## Artifacts Created or Updated

Created:
- `src/SkyRoute.Application/Services/FlightFareResolver.cs`
- `tests/SkyRoute.Application.Tests/Services/FlightFareResolverTests.cs`
- `docs/handoffs/16d-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-full-fix.md` (this file)

Updated:
- `src/SkyRoute.Application/Interfaces/IFlightProvider.cs`
- `src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs`
- `src/SkyRoute.Infrastructure/Providers/BudgetWingsProvider.cs`
- `src/SkyRoute.Application/Validation/BookingRequestValidator.cs`
- `src/SkyRoute.Application/Services/BookingService.cs`
- `src/SkyRoute.Api/Program.cs`
- `tests/SkyRoute.Application.Tests/Services/BookingServiceTests.cs`
- `tests/SkyRoute.Application.Tests/Providers/GlobalAirProviderTests.cs`
- `tests/SkyRoute.Application.Tests/Providers/BudgetWingsProviderTests.cs`
- `tests/SkyRoute.Application.Tests/TestDoubles/StubFlightProvider.cs`
- `tests/SkyRoute.Application.Tests/TestDoubles/FakeBookingStore.cs`
- `tests/SkyRoute.Api.IntegrationTests/TestDoubles/ThrowingFlightProvider.cs`
- `tests/SkyRoute.Api.IntegrationTests/Controllers/BookingControllerTests.cs`

Not touched (per instruction): `docs/reviews/security-review-phase-16.md` (SEC-001 status update is the security-reviewer's/orchestrator's decision, not mine), `docs/handoffs/current-handoff.md`, `docs/handoffs/handoff-index.md`, `docs/handoffs/workflow-state.md`.

## Decisions Made

See "Decisions made" under Work Completed above (rejection vs. silent substitution, exact-match tolerance, check ordering relative to document validation, unknown-flight-number handling, ordinal comparisons).

## Open Questions

- None from an implementation standpoint — the residual gap explicitly left open by handoff 16a/16c (option (a), full server-side price re-resolution) has now been implemented and tested.
- One process question for the security-reviewer/orchestrator: should this now be re-verified and SEC-001's status updated from `Partially Resolved` to `Resolved` in `docs/reviews/security-review-phase-16.md`? This developer does not have authority to make that call (consistent with 16a/16c's own framing) — flagging for the next re-verification pass.

## Risks and Impediments

- None encountered. Build and full test suite are green (159/159).
- No architectural blocker was found — flagging this explicitly since the task brief allowed for the possibility of one. Pricing turned out to be deterministic and keyed by fields already on `BookingFlightRequest`, so no offer-persistence/expiry redesign was needed.
- Residual note (not a new risk, just documenting scope): this fix verifies fare *correctness* against the provider's schedule; it does not add any new check on `Origin`/`Destination` consistency against that same schedule entry (e.g., a client could still submit a `FlightNumber` with a mismatched `Origin`/`Destination` pair, since `RouteTypeResolver` uses the client-submitted origin/destination directly, not the schedule's). This was out of scope for SEC-001 (which is specifically about price/fare tampering) and was not something the task brief asked to be fixed — noting it only so it isn't mistaken for something this fix already covers.

## Required Next Agent Action

Hand back to sdlc-orchestrator / security-reviewer to independently re-verify this fix against the actual code (not just this handoff) and update SEC-001's status in `docs/reviews/security-review-phase-16.md` (likely `Resolved`, given option (a) is now fully implemented and tested — but that determination belongs to the security-reviewer, not this developer).

## Completion Criteria for Next Step

- Security-reviewer independently reads `src/SkyRoute.Application/Interfaces/IFlightProvider.cs`, `src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs`/`BudgetWingsProvider.cs`, `src/SkyRoute.Application/Services/FlightFareResolver.cs`, `src/SkyRoute.Application/Services/BookingService.cs`, `src/SkyRoute.Application/Validation/BookingRequestValidator.cs`, and the new/updated test files, independently re-runs `dotnet build`/`dotnet test`, and updates SEC-001's status.
- Orchestrator updates `docs/handoffs/workflow-state.md` and `docs/handoffs/handoff-index.md` to reflect this handoff.

## Relevant Files

- `src/SkyRoute.Application/Interfaces/IFlightProvider.cs` (updated)
- `src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs` (updated)
- `src/SkyRoute.Infrastructure/Providers/BudgetWingsProvider.cs` (updated)
- `src/SkyRoute.Application/Services/FlightFareResolver.cs` (new)
- `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (updated)
- `src/SkyRoute.Application/Services/BookingService.cs` (updated)
- `src/SkyRoute.Api/Program.cs` (updated)
- `tests/SkyRoute.Application.Tests/Services/FlightFareResolverTests.cs` (new)
- `tests/SkyRoute.Application.Tests/Services/BookingServiceTests.cs` (updated)
- `tests/SkyRoute.Application.Tests/Providers/GlobalAirProviderTests.cs` (updated)
- `tests/SkyRoute.Application.Tests/Providers/BudgetWingsProviderTests.cs` (updated)
- `tests/SkyRoute.Application.Tests/TestDoubles/StubFlightProvider.cs` (updated)
- `tests/SkyRoute.Application.Tests/TestDoubles/FakeBookingStore.cs` (updated)
- `tests/SkyRoute.Api.IntegrationTests/TestDoubles/ThrowingFlightProvider.cs` (updated)
- `tests/SkyRoute.Api.IntegrationTests/Controllers/BookingControllerTests.cs` (updated)
- `docs/reviews/security-review-phase-16.md` (read only, not modified — SEC-001 finding source)
- `docs/handoffs/16a-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-fix.md` (read only)
- `docs/handoffs/16c-security-reviewer-to-sdlc-orchestrator-sec-reverification.md` (read only)
