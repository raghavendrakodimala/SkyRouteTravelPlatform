# Test Execution Summary — Frontend Unit/Component Tests (Vitest)

| Field | Value |
|---|---|
| Scope | Phase 13 extension — IMP-002 resolution: install the frontend test-runner packages (`vitest`, `jsdom`) and execute the 16 Vitest/Angular-TestBed spec files (145 test cases) written earlier in Phase 13, per explicit Human PO approval (2026-07-06) |
| Branch | `sdlc/13-test-writing-skyroute-mvp` |
| Commit (working tree base) | `0575a7c964fba9a9d5d807ed47956070057a3595` |
| Author | lead-full-stack-engineer |
| Date | 2026-07-06 |
| Governance | `.claude/rules/review-and-test-reporting.md`, CLAUDE.md Section 13 |
| Related | `docs/testing/execution/e2e-playwright-test-execution-summary.md` (Playwright/E2E — separate suite, separate runner, executed earlier in Phase 13) |

---

## 1. Test Environment

- **Runner:** Vitest `4.1.10`, invoked via Angular's `@angular/build:unit-test` builder (`runner: "vitest"` in `frontend/angular.json`'s `test` architect target).
- **DOM environment:** `jsdom` `29.1.1` (peer dependency of the Angular/Vitest unit-test builder; provides `document`/`window`/`Element` etc. for `TestBed`-rendered components without a real browser).
- **Node/npm:** `npm 11.17.0` (per `frontend/package.json`'s `packageManager` pin).
- **No servers required** — this suite runs entirely in-process against compiled TypeScript via `TestBed`; no backend or dev server is started (unlike the Playwright E2E suite).

## 2. Commands Executed

```
cd frontend
npm install --save-dev vitest jsdom
# added 60 packages; installed vitest@4.1.10, jsdom@29.1.1

npm test          # → ng test → @angular/build:unit-test (runner: vitest)

npm run build      # → ng build (verifies the separate build target is unaffected)
```

## 3. Result Summary

### 3.1 First run (immediately after `npm install`) — Application bundle generation failed (compile error, not a test failure)

4 TypeScript compile errors (`TS2345`) across 3 spec files, all of the same shape: `Array.from(fixture.nativeElement.querySelectorAll(...)).map((el: Element) => ...)` failed to compile because `Array.from()`'s inferred element type resolved to `unknown` rather than `Element` (the `map` callback parameter's explicit `Element` annotation was then incompatible with the inferred `unknown[]` array). No tests executed — the Angular/Vitest build step failed before any spec file ran.

| File | Line | Error |
|---|---|---|
| `src/app/features/booking/passenger-form-section/passenger-form-section.component.spec.ts` | 119 | `TS2345` |
| `src/app/features/confirmation/confirmation/confirmation.component.spec.ts` | 98 | `TS2345` |
| `src/app/features/results/results-list/results-list.component.spec.ts` | 111, 118 | `TS2345` (x2) |

**Classification:** test-authoring defect (TypeScript generic-inference gap in the spec files themselves), not an application defect. Fixed in this same session — see Section 4, fixes 1-3.

### 3.2 Second run (after fixes 1-3) — 144 passed, 1 failed, 1 unhandled rejection

```
Test Files  1 failed | 15 passed (16)
     Tests  1 failed | 144 passed (145)
    Errors  1 error
```

- **Failed:** `src/app/features/search/search-form/search-form.component.spec.ts` → `'calls SearchStateService.search with a OneWay SearchRequest built from the form on a valid submit'` — threw `Error: NG04002: Cannot match any routes. URL Segment: 'results'`.
- **Unhandled rejection (separate from the above, attributed by Vitest to `results-list.component.spec.ts`):** `Error: NG04002: Cannot match any routes. URL Segment: 'booking'`.

**Root cause (both):** the two spec files register an empty route table (`provideRouter([])`) in their `TestBed` configuration, but the components under test call `router.navigate(['/results'])` (`SearchFormComponent.onSubmit()`) and `router.navigate(['/booking'])` (`ResultsListComponent.selectFlight()`) respectively on success paths that the tests deliberately exercise (mocking the state-service call to resolve `'success'`). With no matching route registered, real `Router.navigate()` rejects with `NG04002`.

**Classification:** test-authoring defect (missing route registration in test setup), not an application defect — the application's real route table (defined in `frontend/src/app/app.routes.ts`) does register `/results` and `/booking`; only the isolated component-test harnesses omitted them. Fixed in this same session — see Section 4, fixes 4-5.

### 3.3 Third/final run (after fixes 4-5) — clean

```
Test Files  16 passed (16)
     Tests  145 passed (145)
   Start at 21:24:59
   Duration 5.67s (transform 2.68s, setup 14.60s, import 4.36s, tests 3.78s, environment 39.09s)
```

Exit code `0`. No errors, no unhandled rejections, no skipped tests. **Final, authoritative result: 145/145 passing, 0 failed, 0 skipped, across all 16 spec files.**

### 3.4 Per-file breakdown (final run — all passing)

| # | Spec file |
|---|---|
| 1 | `src/app/core/guards/booking-flow.guards.spec.ts` |
| 2 | `src/app/features/booking/booking.service.spec.ts` |
| 3 | `src/app/features/booking/booking-form/booking-form.component.spec.ts` |
| 4 | `src/app/features/booking/booking-state.service.spec.ts` |
| 5 | `src/app/features/booking/passenger-form-section/passenger-form-section.component.spec.ts` |
| 6 | `src/app/features/confirmation/confirmation/confirmation.component.spec.ts` |
| 7 | `src/app/features/results/results-list/results-list.component.spec.ts` |
| 8 | `src/app/features/results/sort-control/sort-control.component.spec.ts` |
| 9 | `src/app/features/search/flight-search.service.spec.ts` |
| 10 | `src/app/features/search/search-form/search-form.component.spec.ts` |
| 11 | `src/app/features/search/search-state.service.spec.ts` |
| 12 | `src/app/shared/utils/datetime-format.util.spec.ts` |
| 13 | `src/app/shared/utils/http-error.util.spec.ts` |
| 14 | `src/app/shared/utils/pricing.util.spec.ts` |
| 15 | `src/app/shared/utils/sort-flights.util.spec.ts` |
| 16 | `src/app/shared/validators/document-number.validators.spec.ts` |

145 test cases total across these 16 files (no change from the count recorded at authoring time in HO-013 — no test cases were added or removed, only 5 lines across 3 files were corrected, plus 2 files gained a route-registration fix).

## 4. Spec-File Fixes Made (test-authoring defects only — no application code touched)

1. **`src/app/features/booking/passenger-form-section/passenger-form-section.component.spec.ts`** (line 118) — changed `Array.from(fixture.nativeElement.querySelectorAll('.error'))` to `Array.from<Element>(fixture.nativeElement.querySelectorAll('.error'))`. Reason: without the explicit type argument, TypeScript inferred the array as `unknown[]`, making the subsequent `.map((el: Element) => ...)` a compile error.
2. **`src/app/features/confirmation/confirmation/confirmation.component.spec.ts`** (line 97) — same fix, on `querySelectorAll('.passengers li')`. Same reason.
3. **`src/app/features/results/results-list/results-list.component.spec.ts`** (lines 110, 118) — same fix, on both `querySelectorAll('.provider')` calls in the sort-reorder test. Same reason.
4. **`src/app/features/search/search-form/search-form.component.spec.ts`** — added a trivial stub `@Component({ standalone: true, template: '' }) class ResultsStubComponent {}` and changed `provideRouter([])` to `provideRouter([{ path: 'results', component: ResultsStubComponent }])`. Reason: the "valid submit" test intentionally drives `onSubmit()` down the success path, which calls `router.navigate(['/results'])`; an empty test route table caused that real navigation call to reject with `NG04002`, failing the test even though the assertions themselves (`fakeSearchState.search` called with the correct `SearchRequest`) were correct and passing.
5. **`src/app/features/results/results-list/results-list.component.spec.ts`** — added a trivial stub `@Component({ standalone: true, template: '' }) class BookingStubComponent {}` and changed `provideRouter([])` to `provideRouter([{ path: 'booking', component: BookingStubComponent }])`. Reason: the `selectFlight` test calls the component method directly, which internally calls `router.navigate(['/booking'])`; the same empty-route-table gap produced an unhandled `NG04002` rejection that Vitest flagged (attributed to this file) even though the test's own assertions passed.

No assertion, mock, or expected-value in any spec file was changed — only compile-time typing (fixes 1-3) and test-harness router configuration (fixes 4-5). No `frontend/src/app/**` non-spec (application) file was modified as part of this task.

## 5. New QA Findings

None. Both classes of failure encountered were confirmed to be test-authoring gaps (TypeScript inference and incomplete test-route registration), not defects in application behavior — the application's real route table already registers `/results` and `/booking` (`frontend/src/app/app.routes.ts`), and this was independently corroborated by the Playwright E2E suite (`docs/testing/execution/e2e-playwright-test-execution-summary.md`), which exercises those same navigations against the real router and passes 11/11. QA-001, QA-002, QA-004, and QA-005 (recorded earlier in Phase 13) remain open and unaffected by this task; they are unrelated files/behaviors. No QA-006+ raised.

## 6. Build Verification

`npm run build` (→ `ng build`) run after all fixes above: **succeeded, 0 errors, 0 warnings.**

```
Application bundle generation complete. [5.200 seconds]
Initial chunk files | Names | Raw size | Estimated transfer size
main-AL565KSF.js    | main  | 311.14 kB | 80.33 kB
styles-5INURTSO.css | styles |  0 bytes |  0 bytes
Output location: frontend/dist/frontend
```

The `build` and `test` architect targets are independent (`@angular/build:application` vs. `@angular/build:unit-test`); installing `vitest`/`jsdom` as devDependencies and editing only spec files + `angular.json`'s `test` target (already done in the prior Phase 13 step, unchanged here) had no effect on the build target, confirmed by this clean re-run.

## 7. Defects

None raised against application code. See Section 5.

## 8. Risks

None introduced. IMP-002 (blocked frontend unit-test execution pending Human PO approval to install `vitest`/`jsdom`) is now fully resolved — the packages are installed, the full suite executes cleanly, and the result (145/145 passing) is available for Phase 14 to reference directly.

## 9. Final QA Recommendation

**Pass.** All 145 written frontend unit/component test cases execute successfully against the current codebase (16/16 files, 0 failed, 0 skipped). Combined with the Playwright E2E result (11/11 passing, per `docs/testing/execution/e2e-playwright-test-execution-summary.md`) and the backend xUnit result (114/114 passing, per HO-013), Phase 13's full test suite is now green end-to-end. This raw evidence is provided for Phase 14 (Test Execution Summary, owned by functional-tester) to consume directly; this document does not itself constitute the formal Phase 14 report.
