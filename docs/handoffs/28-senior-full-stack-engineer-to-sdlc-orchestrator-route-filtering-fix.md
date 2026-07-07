# Handoff HO-028 — Route Filtering Fix for Mock Flight Providers (Reversing ASM-006/OQ-003)

| Field | Value |
|---|---|
| Handoff ID | HO-028 |
| Date | 2026-07-07 |
| Branch | `fix/requirements-compliance-gaps-skyroute-mvp` |
| Phase | Requirements Compliance Fix (post-Phase-17) |
| From agent | senior-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete — backend fix and backend tests done; frontend e2e impact flagged, not fixed (out of scope) |

## Work completed

The human Product Owner reversed the previously-approved MVP assumption ASM-006/OQ-003 ("mock providers return their entire fixed schedule regardless of requested route") and asked for real route filtering ahead of any other cleanup.

Added route filtering to `ProviderScheduleMapper.BuildResults` — the single shared mapping pipeline both `GlobalAirProvider` and `BudgetWingsProvider` call — so only schedule entries whose `Origin`/`Destination` exactly match the request's `Origin`/`Destination` are mapped to `FlightResult`s. Matching is case-insensitive (`StringComparison.OrdinalIgnoreCase`) on both fields.

Case-handling investigation (per task instruction to stay consistent, not invent a new convention): `SearchRequestValidator.AirportCodeFormat` is the regex `^[A-Z]{3}$` — case-sensitive, uppercase-only. Any request that reaches a provider via the normal `/api/search` flow has therefore already been validated to be uppercase before it gets here; nothing normalizes case anywhere in this codebase (no `.ToUpper()`/`.ToLower()` calls found on `Origin`/`Destination`). `SearchRequestValidator` line 69 compares `Origin`/`Destination` to each other with `StringComparison.Ordinal`, relying on that upstream uppercase-only guarantee rather than normalizing. I used `OrdinalIgnoreCase` in the mapper specifically because the task instruction explicitly required case-insensitive matching there; it does not conflict with the validator's convention (the validator only ever sees already-submitted, pre-validation values and compares Origin to Destination, not to a schedule) and it adds defense-in-depth for any caller that reaches `BuildResults` without going through the HTTP validator (e.g. a direct unit test or a future caller). I did not change the validator or introduce any new normalization step elsewhere.

Did not touch either provider's fixed 4-flight schedule arrays — `GlobalAirProvider.cs` and `BudgetWingsProvider.cs` are otherwise unmodified; the filter lives only in the shared `ProviderScheduleMapper.BuildResults`.

### Test updates

Every existing unit/integration test that asserted against the old unfiltered "always return the full schedule" behavior was updated to pass the origin/destination that actually matches the flight under test, and new tests were added for the filtering behavior itself, case-insensitivity, and the empty-result contract.

**`GlobalAirProviderTests.cs`** (schedule: GA101 LHR→JFK, GA204 LHR→DXB, GA309 JFK→LAX, GA412 MAN→LHR):
- `MakeRequest` gained `origin`/`destination` parameters (default `LHR`/`JFK`, matching GA101) so existing positional/named calls (`MakeRequest(cabinClass)`, `MakeRequest(departureDate: ...)`, `MakeRequest()`) keep working unchanged.
- Replaced `SearchAsync_ReturnsExactlyFourFixedFlights` (asserted all 4 flights present regardless of route — no longer true) with a `[Theory]` `SearchAsync_FiltersToRequestedRoute_ReturnsOnlyTheMatchingFixedFlight` covering all 4 known GlobalAir routes, each asserting exactly one matching result.
- Added `SearchAsync_MatchesRoute_CaseInsensitively` (lowercase `lhr`/`jfk` still resolves GA101).
- Added `SearchAsync_NoScheduledFlightForRoute_ReturnsEmptyList` (LHR→MAN — the reverse of GA412's MAN→LHR — has no fixed entry in either provider, asserts `Assert.Empty`).
- Fixed `SearchAsync_AppliesBR001PricingFormula_PerWorkedExamples` (added origin/destination per InlineData row — GA412's row now searches MAN→LHR, not LHR→JFK).
- Fixed `SearchAsync_CabinClassMultipliers_ScaleBaseFareRelativeToEconomy` (now searches JFK→LAX to reach GA309).
- Fixed `SearchAsync_ArrivalDateTime_RollsOverToNextCalendarDay_WhenDurationCrossesMidnight` (now searches LHR→DXB to reach GA204).
- `SearchAsync_DepartureDateTime_UsesRequestedDate_WithFixedTimeOfDay` (GA101, default route) and `SearchAsync_EchoesRequestedCabinClass_OnEveryResult` needed no change — already on the default LHR→JFK route.
- `ApplyGlobalAirPricing_*` (reflection-based) and `TryResolveFare_*` tests: unaffected — they never call `SearchAsync`/`BuildResults`.

**`BudgetWingsProviderTests.cs`** (schedule: BW210 LHR→JFK, BW225 SYD→LAX, BW238 LAX→JFK, BW241 MAN→LHR): same pattern — `MakeRequest` gained `origin`/`destination` (default LHR/JFK, matching BW210); replaced the "exactly four" test with a route-filtering `[Theory]`; added case-insensitivity and empty-list tests; fixed `SearchAsync_AppliesBR002PricingFormula_PerWorkedExamples` (BW241 row now MAN→LHR, BW238 row now LAX→JFK), `SearchAsync_CabinClassMultipliers_ScaleBaseFareRelativeToEconomy` (now LAX→JFK for BW238), `SearchAsync_ArrivalDateTime_RollsOverToNextCalendarDay_WhenDurationCrossesMidnight` (now SYD→LAX for BW225), and `SearchAsync_DepartureDateTime_UsesRequestedDate_WithFixedTimeOfDay` (now MAN→LHR for BW241 — this one was failing after the source change since it previously relied on the default LHR/JFK route while asserting against BW241).

**`tests/SkyRoute.Api.IntegrationTests/Controllers/SearchControllerTests.cs`**:
- `Search_ValidRequest_Returns200WithEightMergedResultsFromBothProviders` renamed to `Search_ValidRequest_Returns200WithResultsFilteredToRequestedRouteFromBothProviders`. `MakeValidRequest()` searches LHR→JFK, which now matches exactly one flight per provider (GA101, BW210) — updated assertions from "8 total, 4 per provider" to "2 total, 1 GlobalAir (GA101) + 1 BudgetWings (BW210), both on LHR→JFK".
- `Search_OneProviderThrows_Returns200WithOnlySurvivingProviderResults`: GlobalAir survives with the same LHR→JFK request, which now matches only GA101 — updated assertion from "4 results, all GlobalAir" to "exactly 1 result (GA101), GlobalAir".
- `Search_OriginEqualsDestination_Returns400WithDestinationError`: unaffected (pure validation test, never reaches a provider).

No changes were made to `StubFlightProvider.cs`, `BookingServiceTests.cs`, or `FlightFareResolverTests.cs` — all three either use a test double that bypasses `BuildResults` entirely, or exercise `TryResolveFare` (a separate, unfiltered, flight-number-keyed lookup path used by `BookingService`/`FlightFareResolver` for SEC-001 server-side fare re-resolution), neither of which routes through the new filter.

## Artifacts created or updated

- `src/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs` — added the `.Where(...)` route filter (case-insensitive Origin/Destination match) in `BuildResults`, plus updated XML doc comments.
- `tests/SkyRoute.Application.Tests/Providers/GlobalAirProviderTests.cs` — updated/added tests (see above).
- `tests/SkyRoute.Application.Tests/Providers/BudgetWingsProviderTests.cs` — updated/added tests (see above).
- `tests/SkyRoute.Api.IntegrationTests/Controllers/SearchControllerTests.cs` — updated two tests (see above).
- `docs/handoffs/28-senior-full-stack-engineer-to-sdlc-orchestrator-route-filtering-fix.md` — this file.

No changes to `GlobalAirProvider.cs`, `BudgetWingsProvider.cs` (fixed schedules untouched, as instructed), `SearchRequestValidator.cs`, or any documentation file.

## Decisions made

1. Filter placement: inside `BuildResults`'s LINQ pipeline (`.Where(...)` before `.Select(...)`), not as a separate method — smallest possible diff, and keeps the filtering logic co-located with the one place both providers already share (per `CR-002`'s original centralization rationale referenced in the file's own doc comment).
2. Case-insensitive match via `StringComparison.OrdinalIgnoreCase` on both `Origin` and `Destination` — per explicit task instruction, and as defense-in-depth given the validator already guarantees uppercase for any request that reaches this code through the real HTTP path.
3. Did not add a dedicated `ProviderScheduleMapperTests.cs` file — there was none before this change (the mapper is `internal static` and has always been exercised indirectly through `GlobalAirProviderTests`/`BudgetWingsProviderTests`); kept that existing test-coverage pattern rather than introducing a new one.
4. Replaced (rather than deleted) each "returns exactly four fixed flights" test with a route-filtering theory test that still exercises every one of the 4 fixed schedule entries per provider, just scoped to each entry's own matching route — preserves full schedule-data coverage while asserting the new, correct behavior.

## Open questions

None for this fix itself. One item is explicitly deferred to the next agent (see "Required next agent action").

## Risks and impediments

**Frontend Playwright e2e suite (`frontend/e2e/*.spec.ts`) is now substantially stale — flagged, not fixed (backend-only scope per task instruction).** These are full-stack, real-backend tests, so they are directly affected by the route filtering behavior change, unlike the Angular component unit tests under `frontend/src/app/**/*.spec.ts` (checked — those use fabricated `FlightResult` fixtures via a faked `SearchStateService`, never a real search, so they are unaffected; confirmed no frontend unit spec references any real fixture flight number like `GA101`/`BW210`/etc.).

The core issue: both providers' only `MAN`-involving fixed entries are `MAN→LHR` (GA412, BW241) — there is no `LHR→MAN` entry in either schedule. Several e2e tests search `LHR→MAN` specifically to reach GA412 as "the domestic worked example," relying on the old unfiltered behavior returning the reverse-direction flight too. After this fix, `LHR→MAN` returns an **empty result list**, and `GA412` can no longer be selected after that search at all. Specifically, at minimum:

- `frontend/e2e/full-journey-domestic.spec.ts` — searches `LHR→MAN`, asserts `cards).toHaveCount(8)`, then selects `GA412`. Will now get 0 results and fail immediately at the count assertion (never reaches flight selection).
- `frontend/e2e/booking-validation.spec.ts` — searches `LHR→MAN`, then `selectFlightByNumber(page, 'GA412')`. Will fail — no cards to select from.
- `frontend/e2e/error-states.spec.ts` (test `US-006 AC6`, line ~66) — searches `LHR→MAN`, then `selectFlightByNumber(page, 'GA412')`. Same failure mode.
- `frontend/e2e/full-journey-international.spec.ts` — searches `LHR→JFK`, asserts `cards).toHaveCount(8)`. Will now get exactly 2 (GA101 + BW210), not 8.
- `frontend/e2e/results-persistence.spec.ts` — searches `LHR→JFK`, asserts `toHaveCount(8)` twice (before and after navigating away/back). Will now get 2.
- `frontend/e2e/search-form.spec.ts` (line ~89) — searches `LHR→JFK`, asserts `page.locator('li.result-card')).toHaveCount(8)`. Will now get 2.

This is a broad break across most of the "happy path" e2e journey suite, not an isolated ripple — essentially every e2e test that relies on the previously-unfiltered result set to reach a specific flight or assert a specific count needs its route and/or expected count reconsidered (e.g. picking an `origin`/`destination` pair that actually has a scheduled flight in the intended direction, such as `MAN→LHR` instead of `LHR→MAN` if GA412/BW241 are still meant to be "the domestic worked example," or accepting a much smaller result count for `LHR→JFK`). I did not touch any file under `frontend/e2e/` — this is explicitly out of my backend-only scope, and fixing it well requires a UX/product-level decision about which routes the e2e fixtures should exercise going forward, not just count arithmetic.

**Pre-existing environment issue (not caused by this change):** a `SkyRoute.Api.exe` process (PID 40344) was already running and holding a lock on `src/SkyRoute.Api/bin/Debug/net10.0/SkyRoute.Infrastructure.dll`, which makes `dotnet build`/`dotnet test` against the in-place `bin` output fail with `MSB3027`/`MSB3021` file-lock errors whenever the solution (or anything referencing `SkyRoute.Api.csproj`, including the integration test project) is rebuilt in place. I did not stop this process (no explicit approval, and it may be the human's own manual-verification session for this exact fix). I worked around it for validation purposes only by building/testing `SkyRoute.Api.IntegrationTests.csproj` with `-o <isolated temp directory>`, which does not touch the locked in-place `bin` folder. The orchestrator/human should be aware that a normal in-place `dotnet build`/`dotnet test` will still fail until that process is stopped, restarted, or the workaround is repeated.

No new dependencies were introduced. No destructive commands were run. No files were deleted.

## Required next agent action

1. **solution-architect** must update `docs/requirements.md` (ASM-006 row and OQ-003 resolution, lines ~866/933) and `docs/features/feature-provider-aggregation.md` Section 2 ("Fixed-Schedule Behavior") to reflect that mock providers now filter by requested route (Origin/Destination exact match, case-insensitive) rather than always returning the full fixed schedule. Also consider `docs/requirements.md` Section 7 "Out of Scope for MVP" item 17 ("Flight schedule filtering by date/route... regardless of search date") — route filtering is no longer out of scope (date filtering still is; this fix only filters by route, not by date). I did not edit either document, per task instruction.
2. **orchestrator** should decide how to route the frontend e2e staleness flagged above — likely a UX/UI-designer or lead-full-stack-engineer task to pick correct routes/counts for each affected spec (see the Risks section for the full list and root cause).
3. Recommend re-running `dotnet build`/`dotnet test` against the solution once the locked `SkyRoute.Api.exe` process (PID 40344) is no longer running, to get a clean in-place confirmation (functionally already verified via the isolated-output workaround below — same source, same result).

## Completion criteria for next step

- `docs/requirements.md` ASM-006/OQ-003 and `docs/features/feature-provider-aggregation.md` Section 2 accurately describe route-filtered (not fixed-full-schedule) provider behavior.
- A decision is made and executed on the stale `frontend/e2e/*.spec.ts` specs listed above (fix routes/counts, or explicitly accept as a tracked follow-up item with human sign-off).
- Solution and both test projects build/pass cleanly once verified without the file-lock workaround (should already hold — confirmed below).

## Relevant files

- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\src\SkyRoute.Infrastructure\Providers\ProviderScheduleMapper.cs`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\src\SkyRoute.Infrastructure\Providers\GlobalAirProvider.cs` (read-only reference, not modified)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\src\SkyRoute.Infrastructure\Providers\BudgetWingsProvider.cs` (read-only reference, not modified)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\src\SkyRoute.Application\Validation\SearchRequestValidator.cs` (read-only reference, not modified)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\tests\SkyRoute.Application.Tests\Providers\GlobalAirProviderTests.cs`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\tests\SkyRoute.Application.Tests\Providers\BudgetWingsProviderTests.cs`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\tests\SkyRoute.Api.IntegrationTests\Controllers\SearchControllerTests.cs`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\requirements.md` (ASM-006 ~line 866, OQ-003 ~line 933, Out-of-Scope item 17 ~line 907 — for solution-architect, not edited by this agent)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\features\feature-provider-aggregation.md` (Section 2 — for solution-architect, not edited by this agent)
- Flagged, not modified: `frontend/e2e/full-journey-domestic.spec.ts`, `frontend/e2e/full-journey-international.spec.ts`, `frontend/e2e/results-persistence.spec.ts`, `frontend/e2e/search-form.spec.ts`, `frontend/e2e/booking-validation.spec.ts`, `frontend/e2e/error-states.spec.ts`

## Commands run and results

Baseline (before the source change), from a clean checkout of the two affected test projects — captured by running the *original* test files against the *new* source change and reading which tests newly failed (all failures below are solely attributable to the new filter, not to any pre-existing defect):

```text
dotnet test tests/SkyRoute.Application.Tests/SkyRoute.Application.Tests.csproj --nologo
BEFORE test fixes (source changed, tests not yet updated):
  Failed!  - Failed: 10, Passed: 137, Skipped: 0, Total: 147, Duration: 105 ms

dotnet test tests/SkyRoute.Api.IntegrationTests/SkyRoute.Api.IntegrationTests.csproj -o <isolated temp dir> --nologo
  (isolated output dir used to work around the pre-existing SkyRoute.Api.exe file lock — see Risks)
BEFORE test fixes (source changed, tests not yet updated):
  Failed!  - Failed: 2, Passed: 11, Skipped: 0, Total: 13, Duration: 679 ms
```

This confirms the pre-fix baseline was 147 + 13 = 160 total tests, all passing before the source change (since these are the only 12 that failed once filtering was added, and I did not add or remove any test in this step — only in the subsequent fix step below).

After updating the test files (see "Work completed"):

```text
dotnet build tests/SkyRoute.Api.IntegrationTests/SkyRoute.Api.IntegrationTests.csproj -o <isolated temp dir> --nologo
  Build succeeded. 0 Warning(s). 0 Error(s).
  (proves the full dependency chain — Application, Infrastructure, Api, and the integration
  test project itself — builds clean; the in-place `dotnet build SkyRoute.slnx` at the repo
  root still fails with MSB3027/MSB3021 purely due to the pre-existing SkyRoute.Api.exe
  process lock described in Risks, unrelated to this change)

dotnet test tests/SkyRoute.Application.Tests/SkyRoute.Application.Tests.csproj --nologo
AFTER:
  Passed!  - Failed: 0, Passed: 157, Skipped: 0, Total: 157, Duration: 115 ms
  (147 original + 10 net new: 4-route filtering theory + 4-route filtering theory across the
  two provider suites contribute the count increase, alongside the 2 case-insensitivity tests
  and 2 empty-list tests added)

dotnet test tests/SkyRoute.Api.IntegrationTests/SkyRoute.Api.IntegrationTests.csproj -o <isolated temp dir> --nologo
AFTER (first pass — surfaced one analyzer warning, no failures):
  warning xUnit2013: Do not use Assert.Equal() to check for collection size. Use Assert.Single instead.
  Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 642 ms

  → fixed the warning (Assert.Single instead of Assert.Equal(1, ...)) and re-ran:
AFTER (final):
  Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 621 ms
```

**Final combined result: 170 / 170 tests passing (157 + 13), 0 build errors, 0 build warnings, across both affected test projects.**

Manual verification of task requirement #5 (done via focused unit/integration tests rather than a manually-run server, per the task's own stated allowance):
- `GlobalAirProviderTests.SearchAsync_FiltersToRequestedRoute_ReturnsOnlyTheMatchingFixedFlight` + `BudgetWingsProviderTests.SearchAsync_FiltersToRequestedRoute_ReturnsOnlyTheMatchingFixedFlight` (8 total theory cases) confirm each provider's route filtering in isolation.
- `SearchControllerTests.Search_ValidRequest_Returns200WithResultsFilteredToRequestedRouteFromBothProviders` confirms the combined GlobalAir+BudgetWings behavior for `LHR→JFK`: exactly `GA101` (GlobalAir) and `BW210` (BudgetWings), 2 results total — matches manual cross-referencing of both providers' hardcoded schedules.
- `GlobalAirProviderTests.SearchAsync_NoScheduledFlightForRoute_ReturnsEmptyList` + `BudgetWingsProviderTests.SearchAsync_NoScheduledFlightForRoute_ReturnsEmptyList` confirm `LHR→MAN` (no fixed entry in either provider) returns an empty list, not an error — preserving the existing empty-state UI contract.

No `git commit`/`git merge`/`git push` performed. No dependencies installed. No files deleted. No destructive commands run.
