# Test Execution Summary — Automated E2E (Playwright)

| Field | Value |
|---|---|
| Scope | Phase 13 extension — automated E2E acceptance testing (Playwright), superseding manual-only E2E per Human PO approval (2026-07-06) |
| Branch | `sdlc/13-test-writing-skyroute-mvp` |
| Commit (working tree base) | `0575a7c964fba9a9d5d807ed47956070057a3595` |
| Author | functional-tester |
| Date | 2026-07-06 |
| Governance | `.claude/rules/review-and-test-reporting.md`, CLAUDE.md Section 13 |

---

## 1. Test Environment

- **Backend:** ASP.NET Core 10 (`src/SkyRoute.Api`), run via `dotnet run --no-build --launch-profile http`, listening on `http://localhost:5094`. Build verified clean immediately before this run: `dotnet build SkyRoute.slnx --no-incremental` → 0 Warning(s), 0 Error(s).
- **Frontend:** Angular 22 dev server (`frontend/`), run via `npm start` (`ng serve`), listening on `http://localhost:4200`. CORS on the backend is configured for this origin (`appsettings.json` `Cors:AllowedOrigins`).
- **E2E runner:** Playwright `@playwright/test@1.61.1`, Chromium browser only (`npx playwright install chromium`), 1 worker, no retries, `fullyParallel: false` (`frontend/playwright.config.ts`).
- Both servers were started manually before the run and stopped manually immediately after (see Section 5). No `webServer` auto-start is configured in `playwright.config.ts` by design (see that file's header comment).

## 2. Commands Executed

```
cd frontend
npm install --save-dev @playwright/test        # installs @playwright/test@1.61.1
npx playwright install chromium                # downloads Chromium 149.0.7827.55 + headless shell

dotnet build SkyRoute.slnx --no-incremental     # 0 Warning(s), 0 Error(s)

# Terminal 1 (backend)
cd src/SkyRoute.Api
dotnet run --no-build --launch-profile http

# Terminal 2 (frontend)
cd frontend
npm start

# Terminal 3 (test run, once both servers responded 200)
cd frontend
npx playwright test
```

## 3. Result by Test Area

11 tests total across 6 spec files under `frontend/e2e/`.

| # | Spec file | Test | Result |
|---|---|---|---|
| 1 | `booking-validation.spec.ts` | US-005 AC10 — submit stays disabled until full name, email, document number all valid | **Passed** |
| 2 | `error-states.spec.ts` | US-002 AC6 — zero-result response shows empty-state message | **Passed** |
| 3 | `error-states.spec.ts` | US-002 AC7 — search API failure shows generic error, no backend detail leaked | **Passed** |
| 4 | `error-states.spec.ts` | US-006 AC6 — booking API failure shows generic error, no backend detail leaked | **Failed** — see Section 4, QA-003 |
| 5 | `full-journey-domestic.spec.ts` | US-001–US-006 — full domestic single-passenger journey through to confirmation | **Failed** — see Section 4, QA-003 |
| 6 | `full-journey-international.spec.ts` | US-001–US-006 — full international 3-passenger journey through to confirmation | **Failed** — see Section 4, QA-003 |
| 7 | `results-persistence.spec.ts` | US-002 AC8 — results persist across in-app navigation, no new search call | **Passed** |
| 8 | `search-form.spec.ts` | US-008 AC1/AC2 — airport dropdown shows code/city/country, 2+ countries | **Passed** |
| 9 | `search-form.spec.ts` | US-001 AC8 / US-008 AC4 — same airport cannot be selected as both origin and destination | **Passed** |
| 10 | `search-form.spec.ts` | US-001 AC5 — submit disabled while any required field invalid/empty | **Passed** |
| 11 | `search-form.spec.ts` | US-001 AC6 — loading indicator shown during search call | **Passed** |

**Totals: 8 passed, 3 failed, 0 skipped.**

Raw run output (`npx playwright test`, final clean run after servers were restarted with no further file edits — see Section 6):

```
Running 11 tests using 1 worker
...
  8 passed (1.2m)
  3 failed
    [chromium] › e2e\error-states.spec.ts:63:7 › ... US-006 AC6: a booking API failure ...
    [chromium] › e2e\full-journey-domestic.spec.ts:22:7 › ...
    [chromium] › e2e\full-journey-international.spec.ts:14:7 › ...
```

## 4. Failed Tests — Root Cause

All 3 failures share one root cause: clicking the "Confirm Booking" button on `/booking` never reaches `BookingFormComponent.onSubmit()` at all in a real browser. Diagnosed via direct network/console/navigation-event tracing (see the reproduction steps below) — this is an application defect, not a test defect. It is recorded as **QA-003** (new finding, Critical severity) below and in the handoff. The 3 tests are left asserting the *intended, spec-required* behavior (a real booking confirmation) rather than being adjusted to route around the defect, per this role's mandate not to fix implementation code and not to misreport a real failure as a pass.

**QA-003 — `BookingFormComponent`'s `<form>` element has no `[formGroup]`/`NgForm` directive, so `(ngSubmit)` never intercepts the native `submit` event; clicking "Confirm Booking" triggers a full native page reload instead of the Angular submit handler.**

- **Severity:** Critical — no booking can ever be completed through the real running application; this blocks US-006 (and the money-path of US-004/US-005) entirely for any real user.
- **Area:** `frontend/src/app/features/booking/booking-form/booking-form.component.html`, line 19: `<form (ngSubmit)="onSubmit()" novalidate>`.
- **Evidence:** Direct reproduction (network/console/navigation tracing, not visible in the final suite output but performed during test debugging): after filling all passenger fields and clicking the enabled "Confirm Booking" button, no `POST /api/bookings` request is ever issued; the browser's `framenavigated` event fires for the *main frame* with URL `http://localhost:4200/booking?` (a native GET form submission to the current URL, with an empty query string because none of the reactive-forms-bound `<input>`/`<select>` elements carry a `name` attribute), followed immediately by a full page reload (Vite/Angular dev-server reconnect messages recur in the browser console), which resets all in-memory Signal state. The post-reload `hasSelectedFlightGuard` on `/booking` then sees `selectedFlight() === null` and redirects to `/search` — exactly the final state observed by all 3 failing E2E tests (`page.waitForURL('**/confirmation')` times out having "navigated to http://localhost:4200/search" instead).
- **Root cause:** `search-form.component.html`'s `<form>` correctly binds `[formGroup]="form"` (line 4), which activates Angular's `FormGroupDirective` — the directive that both defines the `ngSubmit` output and calls `event.preventDefault()` on the native `submit` event before re-emitting it. `booking-form.component.html`'s `<form>` binds `(ngSubmit)="onSubmit()"` but has **no** `[formGroup]` (nor is `FormsModule`'s implicit `NgForm` available, since the component imports only `ReactiveFormsModule`). With no directive providing the `ngSubmit` output, Angular's template compiler treats `(ngSubmit)` as a listener for a literal native DOM event named `"ngSubmit"` (which never fires), so the real native `"submit"` event is never intercepted or prevented — the browser performs its default full-page form submission instead.
- **Impact:** Every downstream E2E scenario that depends on an actual booking submission succeeding (full happy-path journeys, the booking-API-failure scenario) fails, because the real user-facing behavior is broken, not because the tests are wrong.
- **Recommendation:** Add a top-level `FormGroup` (e.g., wrap `passengersForm` or introduce a parent group) and bind it via `[formGroup]="<thatGroup>"` on the `<form>` element in `booking-form.component.html`, mirroring `search-form.component.html`'s pattern — this is the minimal, idiomatic fix; no other file needs to change for the click-through path itself.
- **Status:** Open — deferred to Phase 19 (Findings Fixes), per this role's mandate not to modify `src/`/`frontend/src` implementation code. **Recommended to the orchestrator as blocking** for Definition of Done on US-004/US-005/US-006 given the severity (no booking can be completed at all through the real UI) — human/orchestrator decision required on whether this warrants an out-of-sequence fix before the remaining review phases proceed, since several other review phases (accessibility, code review) will otherwise be reviewing a booking flow that cannot actually be exercised end-to-end by a real user.

No other defect was found in the 8 passing tests' code paths.

## 5. Servers Stopped

Both the backend (`dotnet run`, PID confirmed via `netstat` on port 5094) and the frontend dev server (`npm start`/`ng serve`, PID confirmed via `netstat` on port 4200) were terminated (`taskkill /F`) immediately after the final test run completed. Re-verified via `netstat -ano | grep -E ":4200|:5094"` returning no listening sockets. No long-running process was left behind.

## 6. Notes on Test Run Stability

An initial run (before servers were cleanly restarted) showed the same 3 failures plus additional, spurious failures in `search-form.spec.ts` and `results-persistence.spec.ts` caused by test-authoring issues (Playwright locator/matcher usage, not app defects): an `option[value]` selector incorrectly counting the disabled empty-value placeholder option, a `toHaveText` comparison mismatched against `innerText()`-captured text, and a same-airport test that asserted an unreachable error paragraph (see QA-004 in the handoff). These were fixed in the spec files themselves; the corrected suite was then run against freshly restarted servers with no further file edits in between, producing the 8 passed / 3 failed result reported in Section 3, which is the authoritative result for this summary.

## 7. Final QA Recommendation

**Do not sign off Phase 13/14 completion for the booking-submission path.** QA-003 is a Critical, MVP-blocking defect discovered only because this suite exercises a real browser against the real app — it would not have been caught by the existing TestBed-based component/service tests (which call the component method or dispatch synthetic events directly, bypassing real native-form-submission semantics). Recommend routing QA-003 to `lead-full-stack-engineer`/`senior-full-stack-engineer` for an out-of-sequence fix and a full E2E re-run before Phase 15 (Code Review) proceeds, since code review of a booking flow that cannot currently be completed by a real user has limited value until this is resolved. The other 8 passing tests provide real, reproducible, positive evidence for their respective acceptance criteria and require no further action beyond normal Phase 15+ review.

---

## 8. Fix Record — QA-003 Out-of-Sequence Fix (2026-07-06, lead-full-stack-engineer)

**Human PO explicitly approved fixing QA-003 out-of-sequence** (ahead of the normal Phase 19 Findings Fixes sequencing) given its Critical/MVP-blocking severity, per `docs/handoffs/13d-lead-full-stack-engineer-to-sdlc-orchestrator-qa003-fix.md`.

### What changed

- `frontend/src/app/features/booking/booking-form/booking-form.component.html` line 19: added `[formGroup]="bookingForm"` to the `<form>` element (was `<form (ngSubmit)="onSubmit()" novalidate>`, now `<form [formGroup]="bookingForm" (ngSubmit)="onSubmit()" novalidate>`).
- `frontend/src/app/features/booking/booking-form/booking-form.component.ts`: added one new field, `protected readonly bookingForm: FormGroup = this.fb.group({ passengers: this.passengersForm });`, declared immediately after the existing `passengersForm` FormArray. `Angular`'s `FormGroupDirective` (the directive backing `[formGroup]`) is typed to accept a `FormGroup`, not a `FormArray` — `passengersForm` itself could not be bound directly to `[formGroup]`, so `bookingForm` wraps it purely to give the outer `<form>` a valid reactive-forms root. No passenger-array structure, validators, or the nested `passenger-form-section` component's own `[formGroup]="group()"` binding were changed.

### Why

Root cause exactly as diagnosed in Section 4 above: the `<form>` had no `[formGroup]`/`NgForm` directive present (the component imports only `ReactiveFormsModule`, not `FormsModule`), so nothing provided the `ngSubmit` output — Angular never intercepted the native `submit` event, so clicking "Confirm Booking" performed a native full-page form submission/reload instead of invoking `onSubmit()`.

The nested `passenger-form-section.component.html`/`.ts` was checked per the fix brief and found **not** to need any change: it binds `[formGroup]="group()"` directly to its own `@Input() group: FormGroup` (not `formGroupName`/`formArrayName`), which is self-contained and does not depend on the outer form's `ControlContainer` — it was already wired correctly and unaffected by the outer form's missing binding.

### Verification

- `npm run build` (in `frontend/`): **0 errors, 0 warnings** (build succeeded, `Application bundle generation complete`).
- `dotnet build SkyRoute.slnx` (repo root): **0 Warning(s), 0 Error(s)**.
- Full E2E re-run, same procedure as Sections 1–2 above (backend on port 5094 via `dotnet run --no-build --launch-profile http`, frontend on port 4200 via `npm start`, then `npx playwright test` from `frontend/`):

```
Running 11 tests using 1 worker
  ✓  1 booking-validation.spec.ts — submit stays disabled until all passenger fields valid
  ✓  2 error-states.spec.ts — US-002 AC6: zero-result empty state
  ✓  3 error-states.spec.ts — US-002 AC7: search API failure, generic message
  ✓  4 error-states.spec.ts — US-006 AC6: booking API failure, generic message
  ✓  5 full-journey-domestic.spec.ts — full domestic single-passenger journey
  ✓  6 full-journey-international.spec.ts — full international 3-passenger journey
  ✓  7 results-persistence.spec.ts — US-002 AC8: results persist across navigation
  ✓  8 search-form.spec.ts — US-008 AC1/AC2: airport dropdown code/city/country
  ✓  9 search-form.spec.ts — US-001 AC8/US-008 AC4: same-airport guard
  ✓ 10 search-form.spec.ts — US-001 AC5: submit disabled while invalid
  ✓ 11 search-form.spec.ts — US-001 AC6: loading indicator shown

  11 passed (16.5s)
```

**Result: 11/11 passed, 0 failed, 0 skipped** — up from 8/11 passed before the fix. All 3 previously-failing specs (`error-states.spec.ts`'s booking-failure test, `full-journey-domestic.spec.ts`, `full-journey-international.spec.ts`) now pass with no test-file changes; only the application defect was fixed.

Both dev servers were stopped (`taskkill` on the PIDs bound to ports 4200/5094) immediately after this re-run; re-verified via `netstat` showing no listening sockets on either port.

### Status update

QA-003 is updated from **Open** to **Resolved** in `docs/handoffs/13c-functional-tester-to-sdlc-orchestrator-e2e-test-writing.md` (see the note added there). QA-001, QA-002, QA-004, and QA-005 are unaffected and remain **Open**, deferred to Phase 19.
