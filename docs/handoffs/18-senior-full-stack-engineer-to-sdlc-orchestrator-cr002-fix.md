# Handoff HO-018 — CR-002 Fix (Duplicated Provider Mapping Pipeline)

| Field | Value |
|---|---|
| Handoff ID | HO-018 |
| Date | 2026-07-07 |
| Branch | `sdlc/15a-code-review-fixes-skyroute-mvp` |
| Phase | Phase 15a — Code Review Fix Loop |
| From agent | senior-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Completed — ready for code-reviewer verification |

---

## Work Completed

Fixed **CR-002** (Low severity, `docs/reviews/code-review-phase-15.md`): `GlobalAirProvider` and `BudgetWingsProvider` duplicated the entire schedule-to-`FlightResult` mapping pipeline (identical `ScheduledFlight` record shape and an identical `SearchAsync` body — departure-date fallback, cabin multiplier lookup, `baseFare` rounding, UTC `DateTime.SpecifyKind` construction, arrival-time computation, and the `FlightResult` object-initializer).

Introduced one shared, non-abstract mapping helper — `internal static class ProviderScheduleMapper` in `src/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs` — with:

- A single shared `internal sealed record ScheduledFlight(...)` (previously duplicated as an identical private nested record in each provider).
- A single shared `public static IReadOnlyList<FlightResult> BuildResults(IReadOnlyList<ScheduledFlight> schedule, SearchRequest request, string providerName, Func<decimal, decimal> applyPricing)` method containing the previously-duplicated mapping pipeline (departure-date fallback, cabin multiplier lookup, rounding, UTC timestamp construction, arrival-time computation, `FlightResult` construction).

Both `GlobalAirProvider.SearchAsync` and `BudgetWingsProvider.SearchAsync` were reduced to a single line each:

```csharp
var results = ProviderScheduleMapper.BuildResults(Schedule, request, ProviderName, ApplyGlobalAirPricing);
return Task.FromResult(results);
```

(and the `BudgetWingsProvider` equivalent passing `ApplyBudgetWingsPricing`).

**Critical constraint preserved:** `ApplyGlobalAirPricing` (in `GlobalAirProvider`) and `ApplyBudgetWingsPricing` (in `BudgetWingsProvider`) remain distinctly-named, `private static` methods with their original `decimal -> decimal` signatures on their respective provider classes, passed into the shared helper as a `Func<decimal, decimal>` delegate. Neither method was renamed, merged, or moved. This preserves both DP-006/DP-019's per-provider testability intent (per the CR-002 recommendation) and the reflection-based unit tests in `GlobalAirProviderTests.cs`/`BudgetWingsProviderTests.cs` (`typeof(GlobalAirProvider).GetMethod("ApplyGlobalAirPricing", BindingFlags.NonPublic | BindingFlags.Static)` and the `BudgetWings` equivalent), which continue to resolve and invoke the methods by name without modification.

No behavior, pricing, or schedule change — each provider's own fixed `Schedule` data and pricing formula are unchanged; only the previously-duplicated surrounding mapping/loop logic was extracted and shared.

---

## Artifacts Created or Updated

**Created:**
- `src/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs` — new shared internal static helper class (`ScheduledFlight` record + `BuildResults` method).
- `docs/handoffs/18-senior-full-stack-engineer-to-sdlc-orchestrator-cr002-fix.md` (this file).

**Updated:**
- `src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs` — removed duplicated `ScheduledFlight` record and mapping loop from `SearchAsync`; now delegates to `ProviderScheduleMapper.BuildResults`. `ApplyGlobalAirPricing` unchanged (still private static, same signature).
- `src/SkyRoute.Infrastructure/Providers/BudgetWingsProvider.cs` — same change; `ApplyBudgetWingsPricing` unchanged.

**Not touched (out of scope for this task, per instructions):**
- `docs/reviews/code-review-phase-15.md` — left for code-reviewer to update CR-002's status.
- `docs/handoffs/current-handoff.md`, `docs/handoffs/workflow-state.md`, `docs/handoffs/handoff-index.md` — left for orchestrator to update centrally.

---

## Decisions Made

1. Named the shared helper `ProviderScheduleMapper` (internal static class, consistent with the existing `CabinClassMultipliers` internal-static-helper pattern already in `src/SkyRoute.Infrastructure/Providers/`) rather than a shared abstract/base class, since a static helper was sufficient and avoids introducing an inheritance hierarchy for two sealed provider classes.
2. Moved the previously-duplicated `ScheduledFlight` record onto the shared helper (as `ProviderScheduleMapper.ScheduledFlight`) rather than leaving two identical private copies, since the review finding explicitly called out the duplicated record shape as part of the duplication, not just the mapping loop. Each provider's own `Schedule` field still declares its own distinct data (`GA101`/`GA204`/... vs `BW210`/`BW225`/...); only the *shape* is shared.
3. Passed the pricing method as a `Func<decimal, decimal>` delegate parameter (as suggested in the review's own recommendation text) rather than an interface/strategy object, keeping the call site (`ApplyGlobalAirPricing`/`ApplyBudgetWingsPricing` method group conversion) minimal and preserving the exact original method declarations untouched.

---

## Open Questions

None.

---

## Risks and Impediments

None identified. This is a pure internal refactor with no behavior change; build and full test suite confirm no regression.

---

## Required Next Agent Action

`code-reviewer` to re-review the changed files (`src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs`, `src/SkyRoute.Infrastructure/Providers/BudgetWingsProvider.cs`, `src/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs`) scoped to CR-002, and update `docs/reviews/code-review-phase-15.md` to mark CR-002 `Resolved` if verified.

---

## Completion Criteria for Next Step

- code-reviewer confirms the shared mapping helper eliminates the duplication described in CR-002.
- code-reviewer confirms `ApplyGlobalAirPricing`/`ApplyBudgetWingsPricing` remain distinctly-named private static methods on their original classes (unchanged signatures), consistent with the "critical constraint" in the fix instructions.
- code-reviewer sets CR-002 status to `Resolved` in `docs/reviews/code-review-phase-15.md`.

---

## Relevant Files

- `src/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs` (new)
- `src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs` (modified)
- `src/SkyRoute.Infrastructure/Providers/BudgetWingsProvider.cs` (modified)
- `tests/SkyRoute.Application.Tests/Providers/GlobalAirProviderTests.cs` (unchanged — verified passing, including reflection-based test)
- `tests/SkyRoute.Application.Tests/Providers/BudgetWingsProviderTests.cs` (unchanged — verified passing, including reflection-based test)
- `docs/reviews/code-review-phase-15.md` (CR-002 finding — not edited by this agent)

---

## Validation Evidence

**Build:**

```text
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Full test suite:**

```text
dotnet test
Passed!  - Failed: 0, Passed: 103, Skipped: 0, Total: 103 - SkyRoute.Application.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed:  11, Skipped: 0, Total:  11 - SkyRoute.Api.IntegrationTests.dll (net10.0)
```

Total: 114/114 passing — matches the stated baseline, zero regressions.

**Provider-scoped test run (confirms reflection-based tests still resolve by name):**

```text
dotnet test tests/SkyRoute.Application.Tests --filter "FullyQualifiedName~GlobalAirProviderTests|FullyQualifiedName~BudgetWingsProviderTests" -v normal
Total tests: 24
     Passed: 24
```

Includes, passing:
- `GlobalAirProviderTests.ApplyGlobalAirPricing_RoundsToTwoDecimalPlaces_PerBR001` (3 cases, reflection-based)
- `BudgetWingsProviderTests.ApplyBudgetWingsPricing_AppliesDiscountThenFloor_PerBR002` (4 cases, reflection-based)
- All `SearchAsync_*` tests for both providers (schedule, pricing, date/time, cabin-class assertions)

---

*End of Handoff HO-018.*
