# Handoff HO-013D — QA-003 Out-of-Sequence Fix (Critical)

| Field | Value |
|---|---|
| Handoff ID | HO-013D |
| Date | 2026-07-06 |
| Branch | `sdlc/13-test-writing-skyroute-mvp` |
| Phase | Phase 13 (extension) — out-of-sequence Critical finding fix, Human PO-approved ahead of normal Phase 19 (Findings Fixes) sequencing |
| From agent | lead-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete — QA-003 Resolved, verified by full Playwright E2E re-run (11/11 passed) |

---

## Work Completed

Fixed exactly one tracked finding, **QA-003** (Critical), per explicit Human PO approval to fix it out-of-sequence rather than waiting for Phase 19. QA-003 was originally logged in `docs/handoffs/13c-functional-tester-to-sdlc-orchestrator-e2e-test-writing.md` (QA Findings section) and `docs/testing/execution/e2e-playwright-test-execution-summary.md` (Section 4).

### Root cause (as diagnosed by functional-tester, confirmed during this fix)

`frontend/src/app/features/booking/booking-form/booking-form.component.html` line 19 bound `(ngSubmit)="onSubmit()"` on a plain `<form>` with no `[formGroup]` directive present. The component imports only `ReactiveFormsModule` (not `FormsModule`), so no directive (`FormGroupDirective` or `NgForm`) was attached to the `<form>` to provide the `ngSubmit` output or to call `preventDefault()` on the native `submit` event. Clicking "Confirm Booking" therefore performed a native full-page form submission/reload instead of invoking `BookingFormComponent.onSubmit()` — `POST /api/bookings` was never sent, and all in-memory Signal state (including the selected flight) was wiped by the reload, after which `hasSelectedFlightGuard` redirected the user to `/search`.

### Fix applied

1. **`frontend/src/app/features/booking/booking-form/booking-form.component.html`** — added `[formGroup]="bookingForm"` to the `<form>` element:
   - Before: `<form (ngSubmit)="onSubmit()" novalidate>`
   - After: `<form [formGroup]="bookingForm" (ngSubmit)="onSubmit()" novalidate>`

2. **`frontend/src/app/features/booking/booking-form/booking-form.component.ts`** — added one new field immediately after the existing `passengersForm` `FormArray` declaration:
   ```ts
   protected readonly bookingForm: FormGroup = this.fb.group({ passengers: this.passengersForm });
   ```
   Angular's `FormGroupDirective` (which backs the `[formGroup]` input) is typed to accept only a `FormGroup`, not a `FormArray` (confirmed against `@angular/forms`' `.d.ts`: `form: FormGroup`). The existing `passengersForm` property is a `FormArray<FormGroup>` (one group per passenger, sized dynamically from passenger count) and could not be bound to `[formGroup]` directly. `bookingForm` is a thin wrapper `FormGroup` that holds `passengersForm` as a single nested control, added solely so the outer `<form>` has a valid reactive-forms root. No passenger validators, no `FormArray` structure, and no other component logic were changed.

3. **Checked `passenger-form-section.component.html`/`.ts`** (the per-passenger nested component) per the fix brief's step 3, since it renders inside the array-of-passenger-forms loop. Found it did **not** need any change: it binds `[formGroup]="group()"` directly to its own `@Input() group: FormGroup` — a self-contained `FormGroupDirective` binding that does not rely on `formGroupName`/`formArrayName` or any parent `ControlContainer`. This was already correctly wired before this fix and is unaffected by the outer form's missing binding; it was not touched.

No other file was modified. Scope was held strictly to QA-003 — QA-001, QA-002, QA-004, and QA-005 were not touched and remain **Open**, deferred to Phase 19.

### Verification

1. **Frontend build:** `npm run build` in `frontend/` — succeeded, **0 errors, 0 warnings** (`Application bundle generation complete`, output to `frontend/dist/frontend`).
2. **Backend build:** `dotnet build SkyRoute.slnx` — succeeded, **0 Warning(s), 0 Error(s)**.
3. **E2E re-run:** started backend (`dotnet run --no-build --launch-profile http` from `src/SkyRoute.Api`, port 5094) and frontend (`npm start` from `frontend/`, port 4200), confirmed both responsive, then ran `npx playwright test` from `frontend/`:
   - **Result: 11 passed, 0 failed, 0 skipped** (up from 8 passed / 3 failed before the fix).
   - All 3 previously-failing specs now pass with **zero test-file changes**: `error-states.spec.ts`'s US-006 AC6 booking-API-failure test, `full-journey-domestic.spec.ts`, `full-journey-international.spec.ts`.
   - Full raw output recorded in `docs/testing/execution/e2e-playwright-test-execution-summary.md` Section 8.
4. **Servers stopped:** both dev servers were terminated (`taskkill //F //PID` on the PIDs bound to ports 4200 and 5094) immediately after the re-run. Re-verified via `netstat -ano | grep -E ":4200|:5094"` returning no listening sockets. No process left running.

---

## Artifacts Created or Updated

- `frontend/src/app/features/booking/booking-form/booking-form.component.html` (modified — added `[formGroup]="bookingForm"`)
- `frontend/src/app/features/booking/booking-form/booking-form.component.ts` (modified — added `bookingForm` field)
- `docs/testing/execution/e2e-playwright-test-execution-summary.md` (modified — new Section 8, fix record with before/after Playwright result)
- `docs/handoffs/13c-functional-tester-to-sdlc-orchestrator-e2e-test-writing.md` (modified — QA-003 status note added: Open → Resolved; finding text itself preserved, not rewritten)
- `docs/handoffs/13d-lead-full-stack-engineer-to-sdlc-orchestrator-qa003-fix.md` (this file, new)
- `docs/handoffs/current-handoff.md` (updated — points to HO-013D)
- `docs/handoffs/handoff-index.md` (updated — HO-013D row added)

`docs/handoffs/workflow-state.md` intentionally **not** updated by this handoff, per task instruction — reserved for the orchestrator.

No file under `passenger-form-section/`, `search-form/`, or any other component was modified. `frontend/src/app/features/booking/passenger-form-section/*` was read/reviewed but required no change.

---

## Decisions Made

1. **Wrapped `passengersForm` (a `FormArray`) in a new `bookingForm` (`FormGroup`)** rather than attempting to bind `[formGroup]` directly to `passengersForm`, because Angular's `FormGroupDirective` input is typed strictly as `FormGroup` (verified against `@angular/forms` type declarations) — a direct `FormArray` binding would not be idiomatic/type-correct. This is the minimal correct fix consistent with the fix brief's intent ("the correct FormGroup property from the component class") given the component's actual `FormArray`-based passenger structure.
2. **Did not modify `passenger-form-section.component.html`/`.ts`.** Its `[formGroup]="group()"` binding is self-contained (binds directly to the `@Input() group: FormGroup` it receives, not via `formGroupName`/`formArrayName`/`ControlContainer` inheritance from an ancestor), so it was already correct and functioned independently of the outer form's binding gap — confirmed by the E2E re-run itself (per-passenger field validation, touched/invalid error messages, and server-error display all worked correctly in the passing full-journey specs).
3. **Did not adjust any Playwright spec file.** All 3 previously-failing tests now pass unmodified, confirming the E2E test authoring itself was correct and the defect was purely in application code, consistent with the functional-tester's original diagnosis.

---

## Open Questions

None. QA-003 is fully resolved and verified.

---

## Risks and Impediments

None introduced by this fix. QA-001, QA-002, QA-004, and QA-005 remain **Open**, unaffected, and deferred to Phase 19 as originally planned — this fix did not touch any of the code areas those findings reference (`search-form.component.html`/`.ts`, backend validation).

---

## Required Next Agent Action

1. SDLC Orchestrator to review this handoff and the updated execution summary/QA-003 status note.
2. Update `docs/handoffs/workflow-state.md` to reflect QA-003 Resolved (reserved for orchestrator per task instruction, not done in this handoff).
3. Resume the normal phase sequence (Phase 15 Code Review, etc.) — the booking flow can now be reviewed as a fully working, real, end-to-end path.
4. Continue tracking QA-001, QA-002, QA-004, QA-005 for Phase 19 as before; QA-003 is closed out of that backlog.

## Completion Criteria for Next Step

- `docs/handoffs/workflow-state.md` updated by orchestrator to reflect QA-003 Resolved and this out-of-sequence fix.
- Phase 15+ proceeds against a booking flow verified end-to-end via the 11/11 passing E2E suite.

## Relevant Files

- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\booking-form\booking-form.component.html`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\booking-form\booking-form.component.ts`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\passenger-form-section\passenger-form-section.component.html` (reviewed, not modified)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\passenger-form-section\passenger-form-section.component.ts` (reviewed, not modified)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\search\search-form\search-form.component.html` (reference pattern, not modified)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\testing\execution\e2e-playwright-test-execution-summary.md`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\handoffs\13c-functional-tester-to-sdlc-orchestrator-e2e-test-writing.md`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\handoffs\current-handoff.md`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\handoffs\handoff-index.md`
