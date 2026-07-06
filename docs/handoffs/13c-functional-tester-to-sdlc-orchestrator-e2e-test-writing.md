# Handoff HO-013C — Phase 13 Extension: Automated E2E Test Writing (Playwright)

| Field | Value |
|---|---|
| Handoff ID | HO-013C |
| Date | 2026-07-06 |
| Branch | `sdlc/13-test-writing-skyroute-mvp` |
| Phase | Phase 13 (extension) — Test Writing: automated E2E via Playwright |
| From agent | functional-tester |
| To agent | sdlc-orchestrator |
| Status | Complete (spec authoring, environment setup, and one full execution pass), but surfaces a **Critical, MVP-blocking application defect (QA-003)** discovered only through real-browser E2E execution. 8/11 automated E2E tests passed; 3/11 failed due to QA-003, not due to test defects. |

This is a numbered extension handoff under Phase 13, following `HO-013` (backend/frontend unit-and-component test writing, already Complete). It does not replace or supersede HO-013 — it adds automated E2E coverage that HO-013's original scope explicitly deferred to manual/exploratory only (test-strategy.md v1.0 Section 1.4), per new, explicit Human PO approval received 2026-07-06 to introduce Playwright.

---

## Work Completed

### 1. Spec update (required first step, completed before any test code)

`docs/testing/test-strategy.md` bumped to **v1.1**:
- Section 1.4 rewritten: automated E2E via Playwright is now the primary E2E mechanism for MVP, per explicit Human PO approval (2026-07-06) to introduce the tool and to run the associated `npm install`/`npx playwright install`/local-server commands. Manual/exploratory E2E is retained as a documented fallback/supplement, not removed.
- `QA-STRAT-OQ-002` (Section 10) marked **Resolved** with the approval date and a pointer to this handoff.
- New Section 12 (Document Changelog) added; Version bumped 1.0 → 1.1; Date updated; Status updated to reflect the v1.1 change.
- No other section, requirement, business rule, or NFR decision was reopened or altered.

### 2. Playwright installation

Run from `frontend/`:
```
npm install --save-dev @playwright/test    # installed @playwright/test@1.61.1
npx playwright install chromium            # Chromium 149.0.7827.55 + headless shell + ffmpeg + winldd
```
Chromium-only was installed (not the full Firefox/WebKit set `playwright install` would otherwise fetch) to keep the download bounded for this MVP scope — `playwright.config.ts` only defines a `chromium` project, so no other browser binary is needed. This is a scoped, documented deviation from the literal `npx playwright install` command in the task brief; flagging it here for visibility rather than silently narrowing scope.

`frontend/package.json` gained one devDependency (`@playwright/test`) and one new script (`"e2e": "playwright test"`). No other dependency was touched. `frontend/.gitignore` gained standard Playwright output-folder ignores (`/playwright-report`, `/test-results`, `/blob-report`, `/playwright/.cache`).

### 3. Configuration and spec files created

- `frontend/playwright.config.ts` — Chromium-only project, `testDir: './e2e'`, `baseURL: 'http://localhost:4200'`, `workers: 1`, `retries: 0`, HTML + list reporters. Deliberately does **not** use Playwright's `webServer` auto-start (both the real ASP.NET Core backend and the real Angular dev server are started/stopped manually around the run — see Section 5 of the execution summary).
- `frontend/e2e/tsconfig.json` — editor/type support only; does not affect the Angular app's own `tsconfig.app.json`/`tsconfig.spec.json` project graph.
- `frontend/e2e/support/helpers.ts` — shared helpers (`fillSearchForm`, `searchAndGoToResults`, `selectFlightByNumber`, `fillPassenger`, `expectBookingReferenceFormat`, `tomorrowDateString`) used across all spec files, built directly from the real component templates/selectors (`frontend/src/app/features/**`), not guessed.
- `frontend/e2e/full-journey-domestic.spec.ts` — US-001 through US-006, domestic single-passenger (GA412, MAN→LHR, both UK): search happy path, results display (both providers present), default price-ascending sort with an active-indicator check, a genuine re-order to duration-ascending sort with zero new `/api/search` calls, flight selection with no new API call, price breakdown, document label ("National ID"), passenger fill, confirm, booking-reference-format assertion, confirmation screen re-submission-guard checks (no resubmit control; navigating back to `/booking` shows the disabled-submit "already confirmed" state). Also serves as a light natural smoke check for US-007 (a single merged response containing both providers' flights).
- `frontend/e2e/full-journey-international.spec.ts` — US-004/US-005/US-006, international 3-passenger (GA101, LHR→JFK): document label ("Passport Number") identical across all 3 sections, progressive submit-gating as each passenger is filled, price breakdown (287.50 × 3 = 862.50), confirmation reference format and all 3 names.
- `frontend/e2e/search-form.spec.ts` — US-008 AC1/AC2 (airport dropdown code/city/country, 2+ countries), US-001 AC8/US-008 AC4 (same-airport guard), US-001 AC5 (submit disabled while invalid), US-001 AC6 (loading indicator, using a network-delay-only route interception that still lets the real backend answer the real request).
- `frontend/e2e/results-persistence.spec.ts` — US-002 AC8 (results persist across in-app navigation to `/booking` and back, with zero new `/api/search` calls).
- `frontend/e2e/error-states.spec.ts` — US-002 AC6 (zero-result empty state), US-002 AC7 (search API failure → generic message, no leak), US-006 AC6 (booking API failure → generic message, no leak). Uses documented, narrowly-scoped `page.route(...).fulfill(...)` interception for these three scenarios only (see the file's header comment and test-strategy.md Section 1.4 for the full rationale: the fixed mock-provider schedule makes a genuine empty/failed response structurally unreachable through any valid UI-driven request).
- `frontend/e2e/booking-validation.spec.ts` — US-005 AC10, full-name/email/document-number boundary cases with inline error-message assertions, progressive enable/disable of "Confirm Booking" as fields are corrected/broken again.

All 8 user stories' "Manual E2E"-tagged acceptance criteria from test-strategy.md v1.0 Section 2's traceability table are covered by at least one of the above, with the two documented exceptions/clarifications: (a) the empty-result/API-failure scenarios use response interception for the reasons above, and (b) US-007's "Manual E2E" cell is satisfied only as a light natural smoke check inside the full-journey specs, consistent with that row's own note that full fault-isolation coverage lives at the unit/integration level.

### 4. Execution

Full details, exact commands, and raw output are in `docs/testing/execution/e2e-playwright-test-execution-summary.md` (new). Summary:

- Backend built clean (`dotnet build SkyRoute.slnx --no-incremental` → 0/0), then run via `dotnet run --no-build --launch-profile http` on port 5094.
- Frontend run via `npm start` (`ng serve`) on port 4200.
- Both confirmed responsive (`curl` 200) before running `npx playwright test`.
- **First run** (before servers were cleanly restarted, while I was still iterating on spec-file bugs) showed the same 3 failures below plus additional spurious failures caused by my own test-authoring issues (a `option[value]` CSS selector counting the disabled placeholder option, a `toHaveText`/`innerText()` matcher mismatch, and a same-airport test asserting an unreachable error paragraph — see QA-004). I fixed all of these in the spec files, then stopped and cleanly restarted both servers, then ran the full suite once more with **no further file edits in between** to eliminate any possible dev-server HMR interference as a variable.
- **Final, authoritative run: 8 passed, 3 failed, 0 skipped** (11 total). The 3 failures are not test defects — see QA-003 below; the tests correctly assert the specified behavior and correctly fail because the application currently cannot perform it.
- Both servers were terminated (`taskkill /F` on the PIDs bound to ports 4200/5094) immediately after the run and re-verified stopped via `netstat`. No process was left running.

---

## Artifacts Created or Updated

- `frontend/playwright.config.ts` (new)
- `frontend/e2e/tsconfig.json` (new)
- `frontend/e2e/support/helpers.ts` (new)
- `frontend/e2e/full-journey-domestic.spec.ts` (new)
- `frontend/e2e/full-journey-international.spec.ts` (new)
- `frontend/e2e/search-form.spec.ts` (new)
- `frontend/e2e/results-persistence.spec.ts` (new)
- `frontend/e2e/error-states.spec.ts` (new)
- `frontend/e2e/booking-validation.spec.ts` (new)
- `frontend/package.json` (modified — added `@playwright/test` devDependency and `"e2e"` script only)
- `frontend/.gitignore` (modified — added Playwright output-folder ignores)
- `docs/testing/test-strategy.md` (modified — v1.0 → v1.1, Section 1.4 rewritten, `QA-STRAT-OQ-002` resolved, new Section 12 changelog)
- `docs/testing/execution/e2e-playwright-test-execution-summary.md` (new)
- `docs/delivery/task-board.md` (modified — see Board Update Log entry, PH-13/PH-14 rows updated)
- `docs/handoffs/13c-functional-tester-to-sdlc-orchestrator-e2e-test-writing.md` (this file, new)
- `docs/handoffs/current-handoff.md` (updated — points to HO-013C)
- `docs/handoffs/handoff-index.md` (updated — HO-013C row added)

No implementation source file under `src/` or `frontend/src/` was modified. `frontend/angular.json`, `frontend/src/**`, and all backend files are untouched by this handoff.

---

## Decisions Made

1. **Chromium-only browser install**, not the full Playwright browser matrix — see "Playwright installation" above. Zero functional impact on this MVP's single-target-browser scope; flagged for visibility.
2. **`page.route(...).fulfill(...)` used in exactly one file (`error-states.spec.ts`), for exactly three scenarios**, with an inline rationale comment and a corresponding note in test-strategy.md Section 1.4. Every other spec file performs an uninterrupted real round trip against the real backend. This is a documented, narrow, judgment-call exception to "no mocking," made necessary by the fixed mock-provider schedule (ASM-006) making a genuine empty/failed response structurally unreachable via any valid UI-driven request — the feature spec itself (`feature-flight-search.md` Section 5.2) already acknowledges the empty-array case as "never in practice" under the current design.
3. **`page.route(...)` with an artificial delay + `route.continue()`** (not `.fulfill()`) used once, in the loading-indicator test (`search-form.spec.ts`), to make the otherwise near-instant local in-memory response reliably observable — the real backend still receives and answers the real request; only the browser-perceived timing is stretched.
4. **One combined full-journey spec per route-type/passenger-count combination**, rather than one test per tiny AC, per the task brief's own guidance — with a handful of additional targeted specs for states a single happy-path journey can't exercise (errors, empty state, validation gating, persistence). Traceability from each spec back to its user story/AC is documented inline in each file's header comment.
5. **Did not adjust the 3 failing full-journey/booking-failure tests to "pass" by working around QA-003.** They assert the actual specified behavior (a real booking confirmation completing). Changing them to avoid clicking "Confirm Booking," or to accept a redirect to `/search` as success, would misreport a Critical defect as a pass — explicitly against this role's "never claim tests passed unless command output confirms it" rule and the general instruction not to fix or paper over implementation defects.

---

## QA Findings (New — Continuing From QA-002)

### QA-003 — `BookingFormComponent`'s `<form>` has no `[formGroup]`, so `(ngSubmit)` never fires; "Confirm Booking" triggers a full native page reload instead of an API call

**Severity: Critical.** No booking can be completed through the real running application by any real user. Full technical evidence, root-cause diagnosis, and a recommended fix are in `docs/testing/execution/e2e-playwright-test-execution-summary.md` Section 4 (not duplicated here in full to keep this handoff focused, but the short version): `frontend/src/app/features/booking/booking-form/booking-form.component.html` line 19 binds `(ngSubmit)="onSubmit()"` on a plain `<form>` with **no** `[formGroup]` (unlike `search-form.component.html`, which correctly binds `[formGroup]="form"`). Without a `FormGroupDirective`/`NgForm` present, Angular has no directive providing the `ngSubmit` output, so the binding does not intercept the native `submit` event; clicking the (enabled, valid-state) "Confirm Booking" button performs a native full-page form GET submission, reloading the whole SPA and wiping all in-memory Signal state, after which the post-reload `hasSelectedFlightGuard` redirects to `/search`. `BookingFormComponent.onSubmit()` — and therefore the entire `POST /api/bookings` call — is never invoked. **Status: Open — deferred to Phase 19** per this role's mandate not to modify `src/`/`frontend/src`, but flagged to the orchestrator as a candidate for an out-of-sequence fix given the severity (see "Required Next Agent Action" below).

> **Status update (2026-07-06, lead-full-stack-engineer, out-of-sequence fix, Human PO-approved):** QA-003 is now **Resolved**. Fix: added `[formGroup]="bookingForm"` (a thin `FormGroup` wrapper around the existing `passengersForm` `FormArray`, added for this purpose only) to the `<form>` element in `booking-form.component.html`; the nested `passenger-form-section` component needed no change (its own `[formGroup]="group()"` binding is self-contained and was already correct). Full E2E re-run: **11/11 passed** (up from 8/11), including all 3 previously-failing specs, with zero test-file changes. `npm run build`: 0 errors/0 warnings. Full detail in `docs/testing/execution/e2e-playwright-test-execution-summary.md` Section 8 and `docs/handoffs/13d-lead-full-stack-engineer-to-sdlc-orchestrator-qa003-fix.md`.

### QA-004 — `search-form.component.html`'s same-airport inline error paragraph is unreachable dead code

**Severity: Low.** The paragraph `"Origin and destination airports must be different."` (line ~33) is gated on `submitted() && sameAirportSelected()`. The group-level `sameAirport` validator also makes `form.invalid` true whenever the same airport is selected in both dropdowns, which disables the Search button (`[disabled]="loading() || form.invalid || sameAirportSelected()"`) at the exact same moment. Because a disabled native submit button cannot trigger `onSubmit()` (which is what sets `submitted` to `true`), `submitted()` can never become `true` while `sameAirportSelected()` is also `true` through normal user interaction — so this specific paragraph can never actually render. The underlying requirement (US-001 AC8/US-008 AC4 — "cannot select the same airport as both") is still fully satisfied by the button-disabled behavior itself, which is real and correctly reachable; only this one redundant inline-message branch is dead. **Status: Open — deferred to Phase 19** (cosmetic/code-hygiene, not user-facing since the button-disabled behavior already communicates the same constraint via native `disabled` semantics; low priority relative to QA-003).

### QA-005 — `passengerCount` is submitted as a JSON string (e.g. `"1"`), not a number, when set via the native `<select>`

**Severity: Low.** `search-form.component.ts` declares `passengerCount: this.fb.nonNullable.control<number>(1)`, but the template's plain `<select id="passengerCount" formControlName="passengerCount">` (no `[ngValue]`) means Angular's `SelectControlValueAccessor` writes back whatever the browser's native `<option value="...">` string is once the user interacts with the control — observed directly via network capture: `POST /api/search` body contains `"passengerCount":"1"` (quoted) rather than the documented bare-integer shape in `feature-flight-search.md` Section 2 (`"passengerCount": 2,`). The backend currently tolerates this (`System.Text.Json` successfully parses a numeric-looking JSON string into an `int` property), so search results are unaffected and no test in this suite fails because of it — but a non-numeric string in the same position (confirmed via a direct `curl` reproduction: `"passengerCount":"abc"`) produces an **unhandled 500** rather than a clean 400, the same class of robustness gap already tracked as QA-001. This specific field is not reachable with a non-numeric value through the real UI (the `<select>` only ever offers `"1"`–`"9"`), so it is not user-facing today, but the type mismatch itself (string vs. number on the wire) is a genuine contract-precision deviation. **Status: Open — deferred to Phase 19.** Recommended fix: bind the `<option>` values via `[value]` with `[compareWith]`/a numeric parse in the value accessor, or switch to `[ngValue]="count"` (which preserves the original type through `SelectControlValueAccessor`), consistent with how Angular typically avoids this exact pitfall for typed reactive form controls bound to native selects.

QA-001 and QA-002 (from HO-013) remain Open, unaffected by this handoff — not re-investigated here.

---

## Open Questions

None blocking this handoff's own scope. One process question for the orchestrator: **should QA-003 be fixed out-of-sequence, before Phase 15 (Code Review)/17 (Accessibility)/18 (Performance) proceed?** Those phases would otherwise be reviewing a booking flow that cannot currently be completed end-to-end by a real user in a real browser. This handoff does not decide that — it is a human/orchestrator sequencing call, flagged per this role's boundary (functional-tester finds and reports; does not decide remediation sequencing or fix code).

---

## Risks and Impediments

- **QA-003 (Critical)** is the primary risk carried forward — see above. Recommend it be prioritized ahead of QA-001/QA-002 (both Medium/Low) in whatever order Phase 19 processes findings, given it is the only one that is user-facing and completely blocks the core booking transaction.
- No new impediment to Phase 13/14 execution itself was encountered — Playwright installed and ran successfully end-to-end (browser binaries downloaded without incident, both dev servers started/stopped cleanly).
- IMP-001/IMP-002 (from HO-013, backend/frontend unit-test execution approvals) are unaffected by this handoff and remain the orchestrator's to track separately — this handoff's approvals (Playwright npm install, `npx playwright install`, local `dotnet run`/`ng serve` for E2E purposes only) were scoped explicitly and narrowly to E2E test execution per the task brief, and do not themselves resolve IMP-001/IMP-002 for the Vitest/`dotnet test` coverage-collection commands still pending separate approval.

---

## Required Next Agent Action

1. SDLC Orchestrator to review this handoff and `docs/testing/execution/e2e-playwright-test-execution-summary.md` in full.
2. Decide sequencing for QA-003: route to `lead-full-stack-engineer` or `senior-full-stack-engineer` for a fix now (recommended, given severity) or formally accept the risk and defer strictly to Phase 19 — either way, record the decision.
3. If QA-003 is fixed out-of-sequence, request a re-run of the 3 currently-failing specs (`full-journey-domestic.spec.ts`, `full-journey-international.spec.ts`, `error-states.spec.ts`'s booking-failure test) — this is a fast, low-risk re-verification (`npx playwright test` against the same two local servers) once the one-line-ish template fix lands.
4. Continue tracking QA-001/QA-002 (HO-013) and QA-003/QA-004/QA-005 (this handoff) together as the full findings backlog for Phase 19.
5. Proceed to whatever the orchestrator determines is the next phase (Phase 14 backend/frontend unit-test execution summary is still pending separate human approval per IMP-001/IMP-002 from HO-013 — unaffected by this handoff).

## Completion Criteria for Next Step

- Human/orchestrator decision recorded on QA-003 sequencing.
- If deferred to Phase 19: QA-003/QA-004/QA-005 formally added to whatever findings tracker Phase 15/19 uses, alongside QA-001/QA-002.
- If fixed now: a follow-up E2E re-run recorded (new or appended execution summary) showing all 11 tests passing before Phase 15 proceeds.

## Relevant Files

- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\playwright.config.ts`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\e2e\` (all files listed above)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\package.json`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\.gitignore`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\booking-form\booking-form.component.html` (QA-003 — not modified by this handoff, evidence only)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\search\search-form\search-form.component.html` (QA-004/QA-005 — not modified by this handoff, evidence only)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\testing\test-strategy.md`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\testing\execution\e2e-playwright-test-execution-summary.md`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\delivery\task-board.md`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\handoffs\current-handoff.md`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\handoffs\handoff-index.md`
