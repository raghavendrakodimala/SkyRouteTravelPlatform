# Handoff HO-019 — CR-003 Fix (Booking Reference TOCTOU Race)

| Field | Value |
|---|---|
| Handoff ID | HO-019 |
| Date | 2026-07-07 |
| Branch | `sdlc/15a-code-review-fixes-skyroute-mvp` |
| Phase | Phase 15a — Code Review Fix Loop |
| From agent | senior-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete — awaiting code-reviewer verification |

---

## Work Completed

Fixed **CR-003** (Medium severity, `docs/reviews/code-review-phase-15.md`): `InMemoryBookingStore.CreateAsync`'s blind dictionary-indexer overwrite created a TOCTOU race with `BookingService.GenerateUniqueReferenceAsync`'s check-then-act `ExistsAsync` loop, allowing a second concurrent `CreateAsync` call for the same reference to silently overwrite the first booking.

1. Created `DuplicateBookingReferenceException` in `src/SkyRoute.Application/Exceptions/DuplicateBookingReferenceException.cs`, following the style/namespace placement of `BookingValidationException`. Carries the colliding `BookingReference`.
2. Changed `InMemoryBookingStore.CreateAsync` (`src/SkyRoute.Infrastructure/Persistence/InMemoryBookingStore.cs`) to use `ConcurrentDictionary.TryAdd` (atomic) and throw `DuplicateBookingReferenceException` on failure, instead of the indexer-assignment overwrite.
3. Restructured `BookingService` (`src/SkyRoute.Application/Services/BookingService.cs`): the booking-construction and `_store.CreateAsync` call now live *inside* the reference-generation retry loop (renamed to `CreateBookingWithUniqueReferenceAsync`), bounded by the existing `MaxReferenceGenerationAttempts`. `ExistsAsync` is retained as a fast-path optimization (`continue`s to the next candidate without paying for a `CreateAsync` round-trip on an obviously-taken candidate), but `CreateAsync`'s `TryAdd`/exception is the actual source of truth — a caught `DuplicateBookingReferenceException` triggers a retry with a freshly generated candidate. `CreateBookingAsync` now calls this single method for steps 5–6 instead of a separate `GenerateUniqueReferenceAsync` + inline object-construction + `CreateAsync`.
4. Updated `FakeBookingStore` (`tests/SkyRoute.Application.Tests/TestDoubles/FakeBookingStore.cs`) to mirror the new contract: `CreateAsync` now does a real `Dictionary.TryAdd` and throws `DuplicateBookingReferenceException` on an actual collision. Moved the `ForceCollisionForFirstNCalls`/`AlwaysCollide` forced-collision knobs from `ExistsAsync` onto `CreateAsync` (added `CreateAsyncCallCount`) so tests can simulate the TOCTOU race itself — i.e. `ExistsAsync` says "unused" but `CreateAsync` still collides — which is what the fix is meant to catch.
5. Updated the two collision tests in `tests/SkyRoute.Application.Tests/Services/BookingServiceTests.cs`:
   - `CreateBookingAsync_ReferenceCollision_RetriesUntilAUniqueReferenceIsFound` — now forces the first 3 `CreateAsync` calls to collide and asserts `store.CreateAsyncCallCount > 1`.
   - `CreateBookingAsync_ReferenceCollisionNeverResolves_ThrowsAfterExactlyTenAttempts` — now forces every `CreateAsync` call to collide and asserts exactly 10 `CreateAsyncCallCount` before `InvalidOperationException` (retry cap) is thrown.
6. Added a new test `CreateAsync_ConcurrentWritesForSameReference_ExactlyOneSucceedsAndOneThrows` in `tests/SkyRoute.Application.Tests/Persistence/InMemoryBookingStoreTests.cs`, alongside (not replacing) the existing distinct-reference concurrency smoke test. Two `CreateAsync` calls for the *same* reference are scheduled via `Task.Run` (genuine cross-thread race against the real `ConcurrentDictionary`-backed store) and each wrapped in an async local function so a synchronous throw from the losing call is captured as a faulted `Task` rather than escaping the call site. Asserts exactly one call succeeds, exactly one throws `DuplicateBookingReferenceException`, and the store ends up with exactly one booking for that reference.

## Artifacts Created or Updated

- Created: `src/SkyRoute.Application/Exceptions/DuplicateBookingReferenceException.cs`
- Updated: `src/SkyRoute.Infrastructure/Persistence/InMemoryBookingStore.cs`
- Updated: `src/SkyRoute.Application/Services/BookingService.cs`
- Updated: `tests/SkyRoute.Application.Tests/TestDoubles/FakeBookingStore.cs`
- Updated: `tests/SkyRoute.Application.Tests/Services/BookingServiceTests.cs`
- Updated: `tests/SkyRoute.Application.Tests/Persistence/InMemoryBookingStoreTests.cs`
- Not edited (per instruction): `docs/reviews/code-review-phase-15.md`, `docs/handoffs/current-handoff.md`, `docs/handoffs/workflow-state.md`, `docs/handoffs/handoff-index.md`

## Decisions Made

- Kept `ExistsAsync` as a fast-path pre-check inside the retry loop rather than removing it, per the finding's own recommendation and the task instructions — it avoids paying for a `CreateAsync` round-trip on an obviously-taken candidate, but is explicitly documented in code comments as *not* the source of truth for correctness.
- Repurposed `FakeBookingStore`'s forced-collision knobs (`ForceCollisionForFirstNCalls`/`AlwaysCollide`) to act on `CreateAsync` instead of `ExistsAsync`, and added `CreateAsyncCallCount`. This was necessary to actually exercise the new exception-driven retry path in tests (forcing only `ExistsAsync` to collide would never reach the `CreateAsync`/catch branch at all, since the fast-path `continue` would short-circuit every attempt).
- `DuplicateBookingReferenceException` is a plain `Exception` subclass (no ASP.NET Core dependency), consistent with `BookingValidationException`'s documented constraint (`DP-PROTOCOL-001`) for `SkyRoute.Application`.
- Did not change the `IBookingStore` interface contract signature — only its `CreateAsync` implementations' behavior on collision (throw vs. overwrite). This is a behavioral contract change but not a shape/signature change, so no other `IBookingStore` implementations (there is only the one, `InMemoryBookingStore`) needed touching beyond the fake.

## Open Questions

None. The fix and its required test coverage are complete per the finding's "Required fix" text.

## Risks and Impediments

None identified. `DuplicateBookingReferenceException` is caught entirely within `BookingService`'s retry loop under normal operation (36^6 keyspace) and is not expected to reach `ApiExceptionMiddleware`; if the retry cap is ever exhausted, the existing `InvalidOperationException` path (unchanged) still handles that terminal case.

## Required Next Agent Action

code-reviewer to verify CR-003 and mark Resolved (re-review scoped to the changed files below; report itself is not edited by this agent).

## Completion Criteria for Next Step

- code-reviewer re-reviews `src/SkyRoute.Infrastructure/Persistence/InMemoryBookingStore.cs`, `src/SkyRoute.Application/Services/BookingService.cs`, `src/SkyRoute.Application/Exceptions/DuplicateBookingReferenceException.cs`, and the updated test files.
- code-reviewer confirms the atomic `TryAdd`/exception-driven retry closes the TOCTOU race described in CR-003.
- code-reviewer updates `docs/reviews/code-review-phase-15.md`, setting CR-003's status to `Resolved` (or files a new incremented finding if something new is found).

## Relevant Files

- `src/SkyRoute.Application/Exceptions/DuplicateBookingReferenceException.cs` (new)
- `src/SkyRoute.Infrastructure/Persistence/InMemoryBookingStore.cs`
- `src/SkyRoute.Application/Services/BookingService.cs`
- `tests/SkyRoute.Application.Tests/TestDoubles/FakeBookingStore.cs`
- `tests/SkyRoute.Application.Tests/Services/BookingServiceTests.cs`
- `tests/SkyRoute.Application.Tests/Persistence/InMemoryBookingStoreTests.cs`
- `docs/reviews/code-review-phase-15.md` (reference only, not edited)

---

## Build/Test Evidence

Command: `dotnet build`

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Command: `dotnet test`

```
Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 151 ms - SkyRoute.Application.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 590 ms - SkyRoute.Api.IntegrationTests.dll (net10.0)
```

Total: 115/115 passing (baseline 114 + 1 new test: `InMemoryBookingStoreTests.CreateAsync_ConcurrentWritesForSameReference_ExactlyOneSucceedsAndOneThrows`). The two updated collision tests (`CreateBookingAsync_ReferenceCollision_RetriesUntilAUniqueReferenceIsFound`, `CreateBookingAsync_ReferenceCollisionNeverResolves_ThrowsAfterExactlyTenAttempts`) confirmed passing individually via a filtered detailed run.

*End of Handoff HO-019.*
