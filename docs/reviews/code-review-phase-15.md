# Code Review Report — Phase 15

| Field | Value |
|---|---|
| Document ID | CR-PHASE-15 |
| Date | 2026-07-06 |
| Branch | `sdlc/15-code-review-skyroute-mvp` |
| Phase | Phase 15 — Code Review |
| Reviewer | code-reviewer |
| Scope | `src/SkyRoute.Api/`, `src/SkyRoute.Application/`, `src/SkyRoute.Infrastructure/`, `frontend/src/app/`, plus a light pass over `tests/` and `frontend/src/app/**/*.spec.ts` |
| Reference baselines | `docs/architecture/architecture-plan.md` v1.0, `docs/requirements.md` v1.4, `docs/features/*.md`, HO-012A/HO-012B, HO-013/13C/13D/13E |
| Prior findings referenced (not re-reported) | QA-001 (Medium, `BookingRequestValidator` null-handling — Open), QA-002 (Low, `ApiExceptionMiddleware` content-type — Open), QA-003 (Critical — Resolved), QA-004 (Low, dead-code error paragraph — Open), QA-005 (Low, `passengerCount` string vs number — Open) |

---

## Summary

This review independently re-verified the architectural gates claimed in HO-012A/HO-012B (project-reference structure, absence of `Microsoft.AspNetCore.*`/`ClaimsPrincipal`/`IIdentity` in `SkyRoute.Application`, single HTTP boundary per frontend feature, single Observable→Signal conversion point, single pricing-calculation point) and found all of them structurally sound and consistent with `docs/architecture/architecture-plan.md`. Code across both the backend and frontend is consistently shaped: providers, validators, state services, and HTTP-calling services each follow the same pattern file-to-file, comments consistently trace back to FR-/BR-/DP-/AD- IDs, and no dead scaffolding or stray template artifacts were found outside what HO-012A already documented as removed.

Five findings were raised, all **Low or Medium severity**, all `Open`. **No Critical or High finding was identified in this review.** One finding (CR-003) is a genuine — if low-probability-to-trigger-in-practice — production-readiness/thread-safety gap and is called out explicitly below with a recommendation that it be prioritized in Phase 19 alongside QA-001, even though it does not rise to a level requiring an earlier human decision gate on its own.

---

## Findings

### CR-001 — Duplicated `ToModelState(...)` helper across `SearchController` and `BookingController`

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `src/SkyRoute.Api/Controllers/SearchController.cs` (lines 40–52), `src/SkyRoute.Api/Controllers/BookingController.cs` (lines 56–68) |
| Status | Open |

**Evidence:** Both controllers declare a byte-for-byte identical private static method:

```csharp
private static ModelStateDictionary ToModelState(IDictionary<string, string[]> errors)
{
    var modelState = new ModelStateDictionary();
    foreach (var (field, messages) in errors)
    {
        foreach (var message in messages)
        {
            modelState.AddModelError(field, message);
        }
    }
    return modelState;
}
```

**Impact:** Pure duplication (DRY/NFR-MAINT-002 violation, the same principle explicitly invoked elsewhere in this codebase, e.g. `CabinClassMultipliers`'s own doc comment). Low risk today (only two controllers, unlikely to diverge accidentally), but any future controller that also needs to convert a field-error dictionary to a `ValidationProblem` (e.g. a future `GET /api/bookings/{reference}` per AD-004/Gap-fill BF-05's forward-looking note) would either duplicate it a third time or require a retroactive refactor.

**Recommendation:** Extract to a shared `internal static` extension method (e.g. `IDictionary<string,string[]>.ToModelState()`) in a small `Controllers/ValidationProblemExtensions.cs`, or a shared `ApiControllerBase` both controllers derive from.

**Required fix:** Extract the duplicated method to one shared location; update both controllers to call it. No behavior change.

---

### CR-002 — `GlobalAirProvider`/`BudgetWingsProvider` duplicate the entire schedule-to-`FlightResult` mapping pipeline

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs`, `src/SkyRoute.Infrastructure/Providers/BudgetWingsProvider.cs` |
| Status | Open |

**Evidence:** Both classes declare an identical private `ScheduledFlight` record shape and an identical `SearchAsync` body (departure-date fallback, cabin multiplier lookup, `baseFare` rounding, UTC `DateTime.SpecifyKind` construction, arrival-time computation, and the `FlightResult` object-initializer) — the only differences are the `Schedule` data and the named pricing method (`ApplyGlobalAirPricing` vs. `ApplyBudgetWingsPricing`).

**Impact:** Architecture-plan.md Section 3.8 states "Adding a third provider (US-007) requires exactly one new class ... no change to `FlightAggregatorService`" — true for the aggregator, but a third provider class would in practice also duplicate this ~25-line mapping pipeline a third time, since there is currently no shared base to inherit from. This is a real (if modest) extensibility cost that will compound with each additional provider, working against the "extensible provider architecture" goal explicitly named in `docs/requirements.md` Section 1.2.

**Recommendation:** Extract a small shared, non-abstract helper (e.g. a `protected static FlightResult BuildResult(...)` on a common internal base class `ProviderBase`, or a static helper method taking `(ScheduledFlight, DateOnly, string cabinClass, string providerName, Func<decimal, decimal> pricingFn)`) that both providers call, while keeping each provider's own named pricing method distinct (preserving DP-006/DP-019's per-provider testability intent — this refactor does not change that).

**Required fix:** Introduce one shared mapping helper; both providers call it, passing their own `Schedule` and pricing delegate. No behavior/pricing/schedule change.

---

### CR-003 — `InMemoryBookingStore.CreateAsync` performs a blind overwrite, creating a TOCTOU race with `BookingService`'s reference-uniqueness check

| Field | Value |
|---|---|
| Severity | Medium |
| File/area | `src/SkyRoute.Infrastructure/Persistence/InMemoryBookingStore.cs` (line 20), `src/SkyRoute.Application/Services/BookingService.cs` (`GenerateUniqueReferenceAsync`, lines 106–121) |
| Status | Open |

**Evidence:** `BookingService.GenerateUniqueReferenceAsync` implements a check-then-act pattern: it calls `_store.ExistsAsync(candidate, ...)`, and only if that returns `false` does it return the candidate to the caller, which later calls `_store.CreateAsync(booking, ...)` in a separate step. `InMemoryBookingStore.CreateAsync` is:

```csharp
public Task<Booking> CreateAsync(Booking booking, string tenantId, CancellationToken cancellationToken)
{
    _bookings[booking.BookingReference] = booking;   // indexer assignment — always overwrites
    return Task.FromResult(booking);
}
```

This uses `ConcurrentDictionary`'s indexer (upsert semantics), not `TryAdd`. If two concurrent requests independently generate the same reference and both pass `ExistsAsync` before either calls `CreateAsync` (a real, if narrow, race window under concurrent load), the second `CreateAsync` call silently overwrites the first booking record in the dictionary — the first booking is permanently lost from the store even though its booking reference was already returned to that caller as "confirmed."

**Impact:** This is a genuine correctness gap in exactly the area (`BR-004` reference uniqueness, `BR-008` thread-safety) the architecture and NFR docs specifically call out as requiring care (`NFR-SCALE-002`, and `InMemoryBookingStoreTests.cs`'s own "~50-way concurrent-create thread-safety smoke test," which exercises concurrent creates with *distinct* references and therefore would not catch this specific race). Given the reference keyspace is `36^6 ≈ 2.18 billion` combinations, the practical collision probability in this MVP's demo-scale usage is extremely low — this is why it did not surface in testing — but the store's `CreateAsync` method does not itself enforce uniqueness atomically, which is a structural gap independent of collision probability, and is exactly the kind of latent defect that becomes a real incident under higher concurrent load in a future iteration.

**Recommendation:** Make `CreateAsync` atomic and correctness-enforcing on its own, rather than relying entirely on the caller's separate `ExistsAsync` pre-check: use `_bookings.TryAdd(booking.BookingReference, booking)` and return/throw based on the result (e.g. throw a `InvalidOperationException`/dedicated exception on `false`, which `BookingService.GenerateUniqueReferenceAsync`'s existing retry loop can catch and treat as "collision, retry" — collapsing the two-step check-then-act into a single atomic operation and closing the race entirely).

**Required fix:** Change `InMemoryBookingStore.CreateAsync` to use `ConcurrentDictionary.TryAdd`; adjust `BookingService.GenerateUniqueReferenceAsync`'s retry loop to treat a failed add as a collision (retry) rather than relying solely on the preceding `ExistsAsync` check. Add a unit test that proves two "colliding" concurrent `CreateAsync` calls for the same reference result in exactly one stored booking and one signalled failure/retry, not a silent overwrite.

---

### CR-004 — Frontend has no production environment file / no `fileReplacements` wiring; a "production" build still targets the local dev backend

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `frontend/src/environments/environment.ts`, `frontend/angular.json` (`build.configurations.production`) |
| Status | Open |

**Evidence:** Only one environment file exists (`frontend/src/environments/environment.ts`, `production: false`, `apiBaseUrl: 'http://localhost:5094/api'`). `frontend/angular.json`'s `production` build configuration has no `fileReplacements` entry, so `ng build` (which defaults to the `production` configuration per `"defaultConfiguration": "production"`) still bundles the dev environment file verbatim — the resulting bundle reports `environment.production === false` and points at `localhost:5094` regardless of build configuration.

**Impact:** No functional impact today — `docs/requirements.md` Section 1.3 states deployment is "Local only — no cloud deployment required" for this MVP, and `environment.production` is not read anywhere in the codebase (confirmed via search — zero references), so nothing currently branches on it. This is a config-hygiene gap relative to the environment-externalization intent behind `DP-DEPLOY-001` (referenced in this same file's own doc comment) rather than an active defect — flagged so it is not carried forward silently if/when a non-local target environment is ever introduced.

**Recommendation:** Add `frontend/src/environments/environment.prod.ts` (or equivalent) with `production: true` and a configurable `apiBaseUrl`, and wire a `fileReplacements` entry into `angular.json`'s `production` configuration, consistent with the standard Angular CLI convention already partially scaffolded here.

**Required fix:** Add the missing production environment file and `fileReplacements` wiring, or explicitly document (e.g. in `frontend/README` or this environment file's own comment) that this MVP intentionally has no distinct production target and why, so the gap is a documented decision rather than an oversight.

---

### CR-005 — Reflection-based unit tests on private provider pricing methods duplicate coverage already available through the public API

| Field | Value |
|---|---|
| Severity | Low (informational — test-quality, not a functional defect) |
| File/area | `tests/SkyRoute.Application.Tests/Providers/GlobalAirProviderTests.cs` (`ApplyGlobalAirPricing_RoundsToTwoDecimalPlaces_PerBR001`), `tests/SkyRoute.Application.Tests/Providers/BudgetWingsProviderTests.cs` (equivalent `ApplyBudgetWingsPricing_...` test) |
| Status | Open |

**Evidence:** Both test classes use `typeof(...).GetMethod("ApplyGlobalAirPricing"/"ApplyBudgetWingsPricing", BindingFlags.NonPublic | BindingFlags.Static)` plus `MethodInfo.Invoke(...)` to unit-test the private pricing methods directly with hand-picked boundary values, in addition to the same classes' own `SearchAsync_AppliesBR001PricingFormula_PerWorkedExamples`-style tests, which already exercise the identical pricing formula end-to-end through the public `SearchAsync` entry point using the fixed schedule's own base fares.

**Impact:** This is a deliberate, explicitly-documented choice (test-strategy.md's DP-019/AD-008 "independently unit-testable given only a decimal base fare" intent, called out in the test file's own doc comment) and is not a defect — the reflection call is guarded with `Assert.NotNull(method)` so a rename fails loudly rather than silently. It is flagged here only as a light maintainability note: reflection-based tests against private members are inherently more brittle to internal refactoring (a rename requires updating a string literal in the test, not just the IDE's automatic reference tracking) than the equivalent coverage already achieved through `SearchAsync`'s public surface with additional boundary values not present in the fixed schedule (e.g. the `87.50 → 100.63` generic example already used). Given the small number of test files and the deadline-driven MVP context, this is a low-priority note, not a blocking concern.

**Recommendation:** If revisited post-MVP, consider covering the same boundary-value cases via a lightweight `[InternalsVisibleTo]`-based approach or by parameterizing `SearchAsync`'s existing worked-example tests with additional synthetic schedule entries, rather than reflection — but this is optional polish, not a required fix for this MVP.

**Required fix:** None required for MVP; optional follow-up only if the test suite is revisited for long-term maintainability beyond the hiring-challenge deadline.

---

## Findings Summary Table

| ID | Severity | Area | Status |
|---|---|---|---|
| CR-001 | Low | `SearchController`/`BookingController` — duplicated `ToModelState` helper | Open |
| CR-002 | Low | `GlobalAirProvider`/`BudgetWingsProvider` — duplicated mapping pipeline | Open |
| CR-003 | Medium | `InMemoryBookingStore`/`BookingService` — TOCTOU race on reference uniqueness | Open |
| CR-004 | Low | Frontend environment/build config — no production environment file | Open |
| CR-005 | Low (informational) | Provider tests — reflection-based private-method tests | Open |

**Totals: 0 Critical, 0 High, 1 Medium, 4 Low.**

---

## Areas Explicitly Re-Verified (No New Finding)

- No `Microsoft.AspNetCore.*` reference or using directive found in `src/SkyRoute.Application` (only the doc-comment mention in `BookingValidationException.cs` describing the constraint itself, as HO-012A also reported).
- No `ClaimsPrincipal`/`IIdentity` usage anywhere in `src/`.
- `SkyRoute.Application.csproj`/`SkyRoute.Infrastructure.csproj` are plain `Sdk="Microsoft.NET.Sdk"` class libraries; only `SkyRoute.Api.csproj` is `Sdk.Web`.
- DI composition root (`Program.cs`) matches architecture-plan.md Section 3.8 exactly — singleton lifetimes for `IBookingStore`/`ITenantContext`/`AirportDataService`/`RouteTypeResolver`/`BookingReferenceGenerator`, scoped for validators/providers/orchestration services.
- CORS is origin-restricted via `appsettings.json` (`Cors:AllowedOrigins`), never a wildcard.
- Frontend: exactly 3 `HttpClient` references (`app.config.ts`, `flight-search.service.ts`, `booking.service.ts`); zero `.subscribe(` calls (both state services use `firstValueFrom`); zero real `| async` usages; one shared pricing calculation point (`pricing.util.ts`); one shared airports constant; no UI component library dependency added.
- `InMemoryBookingStoreTests.cs`'s concurrent-create smoke test and `BookingReferenceGeneratorTests.cs`'s non-degenerate-randomness test were reviewed directly — both are well-formed, though the former does not cover the specific same-reference race described in CR-003 (by design, it uses distinct references per concurrent create).
- QA-001, QA-002, QA-004, QA-005 (Open) and QA-003 (Resolved) were cross-checked against current source and confirmed still accurately described — not re-reported here.

---

## Overall Recommendation

No finding in this review requires escalation for an earlier human decision gate ahead of Phase 16 (Security Review). All five findings are Medium/Low, consistent with an MVP built against a hard deadline, and are appropriately deferred to Phase 19 (Findings Fixes) alongside the existing QA-001/QA-002/QA-004/QA-005 backlog. CR-003 is the one finding worth flagging for priority attention within Phase 19 specifically because it touches the same booking-uniqueness guarantee (`BR-004`/`BR-008`) that Phase 13's own test suite already treats as a first-class concern, even though it does not block progression to Phase 16 on its own.

---

*End of Code Review Report — Phase 15.*
