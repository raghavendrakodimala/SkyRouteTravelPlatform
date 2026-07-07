# Review Report — Booking UI Redesign (Post-Phase-17, PO-Directed)

| Field | Value |
|---|---|
| Document ID | REVIEW-BUIR-2026-07-07 |
| Date | 2026-07-07 |
| Branch | `fix/requirements-compliance-gaps-skyroute-mvp` (uncommitted working tree) |
| Phase | Booking UI redesign (post-Phase-17, outside the numbered SDLC phase sequence) |
| Reviewers | code-reviewer (REQ/CODE lenses), accessibility-tester (A11Y lens), ux-ui-designer (VIS lens) |
| Scope | The booking passenger-flow redesign (`frontend/src/app/features/booking/**`), the search-form changes (`frontend/src/app/features/search/search-form/**`), `frontend/src/app/app.routes.ts`, and the visual-polish stylesheets, against `docs/design/booking-passenger-flow-spec.md` (DESIGN-FLOW-001) and `docs/design/visual-design-spec.md` (DESIGN-VISUAL-001) |
| Method | Static source inspection plus targeted test execution where the reviewing role's command rules permitted (noted per finding); no live browser/assistive-technology run |

## Finding ID Namespace

Finding IDs in this report (`REQ-*`, `CODE-*`, `A11Y-*`, `VIS-*`) are scoped to **this report only** and continue each lens's running sequence from the redesign review; they are distinct from Phase 15's `CR-*`, Phase 16's `SEC-*`, and Phase 17's `A11Y-*` report IDs (e.g., A11Y-004 below is unrelated to Phase 17's A11Y-004). During the fix loop the three functional defects were also referred to by the working labels CR-001/CR-002/CR-003; both labels are given per finding. REQ-001/CODE-010, REQ-002/CODE-011, and REQ-003/CODE-012 are the same three underlying defects independently confirmed under two lenses and fixed once each.

## Summary

| ID | Working label | Severity | Area | Status |
|---|---|---|---|---|
| REQ-001 / CODE-010 | CR-001 | High | `app.routes.ts` — `bookingLeaveGuard` never registered | Resolved |
| REQ-002 / CODE-011 | CR-002 | Medium | `booking-form.component.ts` — parked draft silently discarded via server-error repair chain | Resolved |
| REQ-003 / CODE-012 | CR-003 | Medium | `booking-form.component.ts` — confirmed back-nav breakdown used searched count, not booked count | Resolved |
| A11Y-004 | A11Y-007 (working) | Medium | `search-form.component.html` — native `disabled` on submit drops focus to `<body>` | Resolved |
| A11Y-005 | A11Y-008 (working) | Medium | `search-form.component.html` — disabled-until-valid submit made validation alerts unreachable | Resolved |
| A11Y-006 | A11Y-009 (working) | Low | `booking-form.component.css` — `list-style: none` strips list semantics in Safari/VoiceOver | Open (report-only) |
| A11Y-007 | A11Y-010 (working) | Low | `booking-form.component.css` — `aria-disabled` opacity dims the focus ring | Open (report-only) |
| A11Y-008 | A11Y-011 (working) | Low | `passenger-form-section.component.html` — no `aria-describedby`/`aria-invalid` on error'd fields | Open (report-only) |
| A11Y-009 | A11Y-012 (working) | Low | `search-form.component.html` — conditionally rendered loading live region | Open (report-only) |
| CODE-013 | CR-004 | Low | `booking-form.component.ts` — confirmed-state stepper pills read "not started" | Open (report-only; subject structure since removed — see post-review note) |
| VIS-014 | — | Low | booking wizard stylesheets — design-token values duplicated as `var()` fallbacks | Open (report-only) |

**Totals: 0 Critical, 2 High (both Resolved), 6 Medium (all Resolved), 6 Low (all Open, report-only). Zero unresolved Critical/High — no CLAUDE.md §21 acceptance gate is triggered.** All Low findings are advisory: each finding's own text proposes an optional/hardening fix, and none blocks merge; they are filed `Open` for a future polish pass rather than routed through a fix loop now.

---

## Post-Review State Note (Important)

After the fixes below were verified, a **Product Owner UX correction (2026-07-07)** landed in the same working tree, replacing the reviewed save → prompt → review wizard with a single in-place passenger form (see `docs/features/feature-booking-flow.md` v1.1 Section 2 and the status note in `docs/design/booking-passenger-flow-spec.md`). Consequences for reading this report:

- Evidence text and line numbers referencing the prompt block, the review phase, `returnTo`, the searched-count mismatch note, or the `.wizard-steps` progress stepper describe the working tree **as reviewed**; those structures were subsequently removed or reshaped. The *defects and their fixes* remain historically accurate, and the behaviors the fixes protect (leave-guard registration, no silent draft discard, booked-count breakdown after confirmation) are preserved in the amended flow.
- **CODE-013**'s subject (the progress stepper) no longer exists in the tree at all (`booking-form.component.spec.ts:227` now asserts `.wizard-steps` is absent) — the finding is retained for the record and is effectively obsolete; the closing reviewer should confirm and close it.
- **A11Y-006**'s `.wizard-steps <ol>` half is likewise gone, but its `.passenger-list <ul>` half still applies (grep-confirmed today: `list-style: none` at `booking-form.component.css:71`, no `role="list"` in the template).
- **A11Y-007**, **A11Y-008**, **A11Y-009**, and **VIS-014** were re-checked against the current tree and still apply (`opacity: var(--opacity-disabled, 0.6)` on `button[aria-disabled='true']` at `booking-form.component.css:226`; zero `aria-describedby`/`aria-invalid` occurrences in `frontend/src`; the search loading live region still renders inside `@if (loading())` at `search-form.component.html:79–81`; 104 `var(--…, fallback)` occurrences across the two booking stylesheets).

---

## Resolved Findings

### REQ-001 / CODE-010 (working label CR-001) — `bookingLeaveGuard` defined but never registered

| Field | Value |
|---|---|
| ID | REQ-001 (REQ lens) / CODE-010 (CODE lens) |
| Severity | High |
| File or area | `frontend/src/app/app.routes.ts` (booking route); `frontend/src/app/features/booking/booking-leave.guard.ts` |
| Status | **Resolved** |

**Evidence (as filed):** `booking-leave.guard.ts:13` exports `bookingLeaveGuard: CanDeactivateFn<BookingFormComponent> = (component) => component.canLeave();` and its own doc comment prescribes the one-line registration, but the booking route in `app.routes.ts` had no `canDeactivate` — a repo-wide grep found zero references to `bookingLeaveGuard` outside the guard file. The unit test called `component.canLeave()` directly, so 175/175 passing tests did not cover registration. Spec DESIGN-FLOW-001 §B.11 and its Part C checklist require the `canDeactivate` confirm leg alongside `beforeunload`.

**Impact:** In-app navigation (e.g., browser Back from `/booking` to `/results`) silently destroyed all entered passenger data — up to 9 passengers — with no confirmation; only the tab-close/refresh (`beforeunload`) leg worked.

**Recommendation / required fix:** Import `bookingLeaveGuard` in `app.routes.ts`, add `canDeactivate: [bookingLeaveGuard]` to the booking route, and add a route-level test proving router navigation away from `/booking` with entered data triggers the confirm.

**Verification (code-reviewer):** Fixed and verified. `app.routes.ts` imports the guard (line 3) and registers `canDeactivate: [bookingLeaveGuard]` on the booking route (line 31 as verified; line 31 in the current tree). The coverage gap is closed: a route-registration describe block in `booking-form.component.spec.ts` imports the **real** `routes`, uses `provideRouter(routes)` + `RouterTestingHarness.create('/booking')`, and proves (a) `navigateByUrl('/results')` with entered data and confirm=false resolves `false` with the exact confirm wording and the URL staying `/booking`; (b) confirm=true lets navigation proceed; (c) no data → silent leave, no confirm. These tests fail if the registration is ever dropped. No regressions: the guard still delegates to `component.canLeave()`; `navigatedAfterSuccess` disarms the guard before the post-201 redirect; the original direct `canLeave()` unit test is retained. Caveat: the verifying reviewer's command rules were read-only, so verification was static (line-precise) rather than an executed test run.

### REQ-002 / CODE-011 (working label CR-002) — Parked new-passenger draft silently discarded by the server-error repair chain

| Field | Value |
|---|---|
| ID | REQ-002 (REQ lens) / CODE-011 (CODE lens) |
| Severity | Medium |
| File or area | `frontend/src/app/features/booking/booking-form/booking-form.component.ts` (`saveEditedPassenger`, as reviewed) |
| Status | **Resolved** |

**Evidence (as filed):** `beginEdit` parked a dirty collecting draft (`parkedDraft.set(...)`, `openEditForm(index, 'collecting')`), but `saveEditedPassenger`'s `wasFlagged` branches bypassed `leaveEditForm` — the only place `parkedDraft` was restored/cleared — forcing the wizard to review (or chaining via `openEditForm(next, 'review')`, overwriting the carried `returnTo: 'collecting'`). Reachable repro: 400 with `passengers[0].*` errors → cancel the auto-opened edit → "Add another passenger" → type partial data (dirty) → Edit the still-flagged card → fix → Save changes.

**Impact:** The typed draft vanished — violating spec §B.1's "never a silent discard of dirty data" — and the orphaned non-null `parkedDraft` left `guardArmed` permanently true even after all data was removed.

**Recommendation / required fix:** Honor the carried `returnTo`: chain with `openEditForm(next, p.returnTo)` and route the chain-complete case with `returnTo === 'collecting'` through `leaveEditForm` so the parked draft is restored (and `parkedDraft` always cleared). Add a spec test reproducing the exact flow.

**Verification (code-reviewer):** Fixed and verified. The repair chain carried the original `returnTo`, and the chain-complete `'collecting'` case resolved through `leaveEditForm`, which restores the parked draft into the active form, re-marks it dirty, clears `parkedDraft`, and announces "All corrections saved. Returning to passenger {N} details." per spec §B.1 — no orphan path remained in that method. The prescribed spec test was added ("restores a parked draft when the edit resolved through the server-error repair chain (CR-002)"), reproducing the finding's exact repro and asserting the chain carry, restored draft values, cleared error summary/badges, exact live-region wording, and `parkedDraft() === null`. Verified by an executed run: `npm test -- --watch=false --include="**/booking-form.component.spec.ts"` — 34/34 tests passed. The pre-existing review-path chain test was intact (no weakened tests). Out-of-scope observation recorded at verification time: `removePassenger` could still orphan a parked draft in one pre-existing edge case (only saved card removed while being edited over a parked draft) — independent of this finding, flagged for awareness. The parked-draft restore behavior survives in the current in-place flow (`leaveEditForm` in `booking-form.component.ts`).

### REQ-003 / CODE-012 (working label CR-003) — Confirmed back-nav price breakdown computed from the searched count, not the booked count

| Field | Value |
|---|---|
| ID | REQ-003 (REQ lens) / CODE-012 (CODE lens) |
| Severity | Medium |
| File or area | `frontend/src/app/features/booking/booking-form/booking-form.component.ts` (`liveCount`/`priceBreakdown`, as reviewed) |
| Status | **Resolved** |

**Evidence (as filed):** In the `alreadyConfirmed` branch, the count fell back to `searchedCount()` because component-local `savedPassengers` is always empty on a fresh instance after back-navigation from `/confirmation`. Example: search 2, add a 3rd passenger, confirm (real total = 3 × per-person per `BookingResponse.totalPrice`), press Back → the page showed a 2-passenger total under the "already been confirmed" banner, with the mismatch note suppressed. The correct data was available on `BookingResponse` (`totalPrice`, `passengers[]`).

**Impact:** A completed booking's read-only page displayed a wrong total and wrong passenger count whenever the wizard count diverged from the searched count — a regression introduced by the redesign (under the old all-at-once form the searched-count fallback was always correct).

**Recommendation / required fix:** Derive the confirmed-state breakdown from `bookingState.bookingResponse()`: `count = passengers.length`, `total = totalPrice` (also fixing the stepper's contradictory "not started" pills). Add a back-nav spec with a booked count different from the searched count.

**Verification (code-reviewer):** Fixed and verified. `priceBreakdown` returns `{ perPerson: response.flight.pricePerPassenger, count: response.passengers.length, total: response.totalPrice }` whenever a booking response exists and the local saved list is empty — the exact back-nav state — so the confirmed page shows the **booked** figures, never a recomputation; the count computed and the stepper were aligned accordingly (all pills "done", sized from the booked count, in the tree as reviewed). A new spec test simulated back-nav faithfully (fresh component created **after** setting a 3-passenger/750-total response while the searched count was 2) and asserted "USD 750.00 total" (old code rendered USD 500.00), "× 3 passengers", and the corrected stepper. The pre-existing FR-038 back-nav test was intact. Caveat: this verification was static (reviewer role had no test-execution allowance), traced end-to-end through the template, `BookingResponse` model, and `formatUsd`. The booked-figures behavior survives in the current in-place flow (`priceBreakdown` in `booking-form.component.ts` lines 166–185).

### A11Y-004 (working label A11Y-007) — Search submit button natively disabled, dropping focus to `<body>`

| Field | Value |
|---|---|
| ID | A11Y-004 (this report's A11Y lens sequence) |
| Severity | Medium |
| File or area | `frontend/src/app/features/search/search-form/search-form.component.html` (submit button) |
| Status | **Resolved** |

**Evidence (as filed):** `<button type="submit" [disabled]="loading() || form.invalid || sameAirportSelected()">` — when the focused Search button's `disabled` attribute became true on click (`loading()` flipping), browsers move focus to `<body>`; on a 400/network failure no navigation occurs, so focus stayed on `<body>`. This contradicted the booking wizard's own deliberate `aria-disabled` + click-guard pattern, and the design spec itself called the `[disabled]` binding out as the failure mode. The `onSubmit` guard already made the `disabled` binding redundant as a guard.

**Impact:** Keyboard/screen-reader users lost their place the instant a search started; on failure they had to re-orient from the top of the page.

**Recommendation / required fix:** Replace the `[disabled]` binding with `[attr.aria-disabled]="loading() ? true : null"` (the existing `onSubmit` guard no-ops re-entrant submits), add the `button[aria-disabled='true']` styling, and add a jsdom test asserting `document.activeElement` is not `<body>` after a mocked failed search.

**Verification (accessibility-tester):** Resolved. `search-form.component.html` uses `[attr.aria-disabled]="loading() ? true : null"` with no native `disabled` binding anywhere in the search feature (grep-verified); the component keeps the re-entrant `loading()` guard and adds `focusFirstInvalidControl` for rejected submits; the CSS adds the `button[aria-disabled='true']` cursor/opacity treatment with `:hover`/`:active` guards (`--opacity-disabled` token exists in `styles.css`). The unit spec pins "never natively disabled", the in-flight `aria-disabled` state, and the exact requested jsdom focus test after a mocked failed search. The initially-flagged deterministic e2e regression was closed in a follow-up: `frontend/e2e/search-form.spec.ts` was rewritten to the new contract (button `toBeEnabled()` while invalid, `role="alert"` messages, focus on first invalid control, zero `/api/search` requests via interception; in-flight asserts `aria-disabled="true"` and not the native attribute), and `docs/features/feature-flight-search.md`'s US-001 AC5 wording was amended to the submit-attempt-interception pattern (2026-07-07). Caveat: vitest/Playwright were not executed in the verification environment — verification is static but line-precise.

### A11Y-005 (working label A11Y-008) — Disabled-until-valid submit made every client-side validation message unreachable

| Field | Value |
|---|---|
| ID | A11Y-005 (this report's A11Y lens sequence) |
| Severity | Medium |
| File or area | `frontend/src/app/features/search/search-form/search-form.component.html` / `.ts` |
| Status | **Resolved** |

**Evidence (as filed):** All four client error branches were gated on `submitted()`, set only in `onSubmit()` — but the form could not be submitted while invalid (button disabled; implicit Enter-key submission does nothing when the default submit button is disabled), so the `role="alert"` branches were effectively dead code. Worst case: the same-airport rule sets its error on the form **group**, so neither select ever gained `.ng-invalid` — a user selecting the same airport twice faced a silently disabled Search button with zero textual or visual explanation (WCAG 3.3.1/3.3.2).

**Impact:** Client-side error identification was unreachable on the primary path for all users; screen-reader users additionally got no `aria-disabled` reason and no alert.

**Recommendation / required fix:** Remove `form.invalid`/`sameAirportSelected()` from the disabled binding so an invalid submit attempt reaches `onSubmit()`, sets `submitted(true)`, and surfaces the existing alerts; focus the first invalid control on rejected submit; add tests for the empty-origin and same-airport alerts.

**Verification (accessibility-tester):** Resolved; both blocking gaps from the interim "Partially Resolved" verdict were closed. Component fix verified intact: no `[disabled]` binding; `submitted.set(true)` precedes the invalid guard, surfacing all four alert branches (including the formerly dead same-airport message); `markAllAsTouched()` restores the border cue; `focusFirstInvalidControl()` handles the group-level sameAirport rule by focusing `#destination`; the loading guard blocks re-entrant submits. Gap A (e2e regression) closed: `frontend/e2e/search-form.spec.ts` rewritten to the submit-attempt contract (AC8: same airports → button `toBeEnabled()`, click → exact alert text, focus `#destination`, URL stays `/search`, zero API calls; AC5: empty-form submit → all three required-field alerts, focus `#origin`, zero API calls). Gap B (spec contradiction) closed: `docs/features/feature-flight-search.md` US-001 AC5 amended to the submit-attempt interception pattern (A11Y-007/A11Y-008, 2026-07-07). Unit tests cover every required behavior with real DOM submit events; nothing weakened. Caveat: tests not executed in the verification environment (static, line-precise verification). Carry-over observation recorded at verification time — an e2e helper mismatch around the then-in-flight passenger-flow work — was superseded by the subsequent e2e rewrite for the final flow.

---

## Open Findings (Low — report-only)

All six findings below are **advisory**: filed `Open` for the record and a future polish pass, with no fix routed now (report-only per the closing orchestrator's instruction). None blocks merge; none triggers an approval gate.

### A11Y-006 — `list-style: none` strips list semantics in Safari/VoiceOver

| Field | Value |
|---|---|
| Severity | Low |
| File or area | `frontend/src/app/features/booking/booking-form/booking-form.component.css` (`.passenger-list`; `.wizard-steps` as reviewed — since removed) |
| Evidence | `.wizard-steps { … list-style: none; … }` and `.passenger-list { list-style: none; … }`, applied to elements carrying `aria-label="Booking progress"` / `aria-label="Saved passengers"`. Current tree: the `.wizard-steps` `<ol>` no longer exists; `.passenger-list` (`booking-form.component.css:71`) still has `list-style: none` and the `<ul class="passenger-list" aria-label="Saved passengers">` has no `role="list"`. |
| Impact | A long-standing WebKit heuristic makes Safari/VoiceOver drop list semantics for `list-style: none` lists — VoiceOver users lose "list, N items" context, per-item position, and the `aria-label` accessible name. Content remains readable linearly, hence Low. |
| Recommendation | Add `role="list"` to the `.passenger-list` `<ul>` — redundant-but-harmless elsewhere, restores semantics in Safari/VoiceOver. |
| Required fix | None required (report-only); apply the recommendation in a future polish pass. |
| Status | **Open** (report-only) |

### A11Y-007 — `aria-disabled` opacity dims the focus-visible outline below 3:1

| Field | Value |
|---|---|
| Severity | Low |
| File or area | `frontend/src/app/features/booking/booking-form/booking-form.component.css` (`button[aria-disabled='true']`, currently line 226) |
| Evidence | `button[aria-disabled='true'] { opacity: var(--opacity-disabled, 0.6); cursor: not-allowed; }` — whole-element opacity multiplies the global `button:focus-visible` outline (`styles.css`: `2px solid var(--color-primary)`); 0.6 × `#1a5fb4` over white ≈ `#769fd2`, ≈2.7:1 against the page — below the 3:1 non-text threshold. These buttons deliberately stay in the tab order (the point of the `aria-disabled` design), contradicting the rule's own comment that the outline is kept. |
| Impact | During the submit lockdown or the dirty-edit lock, a keyboard user can focus a locked Edit/Remove/Confirm button whose focus ring is materially dimmed. WCAG 1.4.11 exempts inactive controls, so this is hardening, not a hard failure. |
| Recommendation | Replace element opacity with explicit muted colors that leave the outline at full strength, or minimally add `button[aria-disabled='true']:focus-visible { opacity: 1; }`. |
| Required fix | None required (report-only); apply in a future polish pass. |
| Status | **Open** (report-only) |

### A11Y-008 — Field error messages lack `aria-describedby`/`aria-invalid` association

| Field | Value |
|---|---|
| Severity | Low |
| File or area | `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.html` (all three fields; pattern repeats in the search form) |
| Evidence | Client and server error messages are adjacent `<p role="alert">` siblings with no `aria-describedby` on the inputs and no `aria-invalid`; a repo-wide grep of `frontend/src` finds zero occurrences of either attribute (re-confirmed in the current tree). |
| Impact | Errors announce once on appearance (the pattern Phase 17 passed under NFR-A11Y-003), but a screen-reader user who later tabs onto a flagged field — which the server-error repair loop specifically invites, since the reopened edit form renders with errors already present and focus starts on the `#error-summary` banner — hears only the field's label with no indication it is in error or why. Pre-existing pattern, made more load-bearing by the `passengers[i].*` error mapping; advisory. |
| Recommendation | Give each error paragraph a stable id and bind `[attr.aria-describedby]` with whichever error ids are currently rendered plus `[attr.aria-invalid]` on the input; apply the same pattern to the search form. Keep `role="alert"` on appearance. |
| Required fix | None required (report-only); apply in a future polish pass. |
| Status | **Open** (report-only) |

### A11Y-009 — Search loading announcement is a conditionally rendered live region

| Field | Value |
|---|---|
| Severity | Low |
| File or area | `frontend/src/app/features/search/search-form/search-form.component.html` (currently lines 79–81) |
| Evidence | `@if (loading()) { <p class="visually-hidden" role="status" aria-live="polite">Searching for flights…</p> }` — the region is created already containing its message. The booking form explicitly avoids this (its persistent region's comment: "a conditionally-rendered live region misses its first announcement"), so the two screens disagree on the pattern. |
| Impact | Live regions generally must exist in the DOM before their content changes to announce reliably; several screen reader/browser combinations skip an inserted pre-populated `role="status"` element. Pre-existing (this was the shape of the Phase 17 A11Y-005 fix); filed for reliability/consistency. |
| Recommendation | Render the region unconditionally and toggle only its text content, matching the booking screen. No visual change. |
| Required fix | None required (report-only); apply in a future polish pass. |
| Status | **Open** (report-only) |

### CODE-013 (working label CR-004) — Confirmed-state stepper pills read "not started" alongside "Review: completed"

| Field | Value |
|---|---|
| Severity | Low |
| File or area | `frontend/src/app/features/booking/booking-form/booking-form.component.ts` (`steps()`, as reviewed) |
| Evidence | As reviewed: after back-nav from `/confirmation`, the re-created component had `savedPassengers() = []`, so the stepper rendered "Passenger 1: not started", "Passenger 2: not started" alongside "Review: completed" and the "already been confirmed" banner — contradictory visible and screen-reader state for a completed booking. (The prescribed alignment was applied as part of the REQ-003/CODE-012 fix in the reviewed tree.) |
| Impact | Internally inconsistent status text announced to assistive technology on a done booking. |
| Recommendation | Mark all passenger pills "done" when confirmed, sized from `bookingResponse().passengers.length`. |
| Required fix | None required (report-only). **Post-review note:** the PO UX correction removed the progress stepper entirely (`booking-form.component.spec.ts:227` asserts `.wizard-steps` is absent), so this finding's subject no longer exists — effectively obsolete; the closing reviewer should confirm and close. |
| Status | **Open** (report-only; subject removed by the subsequent PO UX correction) |

### VIS-014 — Design-token values duplicated as `var()` fallbacks in the booking stylesheets

| Field | Value |
|---|---|
| Severity | Low |
| File or area | `frontend/src/app/features/booking/booking-form/booking-form.component.css`, `passenger-form-section.component.css` |
| Evidence | Token values are hard-coded a second time as `var()` fallbacks throughout the two booking stylesheets (e.g., `border: 1px solid var(--color-neutral-200, #dddddd);`) — 104 occurrences in the current tree (97 + 7), while the other component stylesheets reference tokens directly. `docs/design/visual-design-spec.md` Section 1: "do not hard-code the hex/px values a second time anywhere else." Every distinct fallback value was verified against the `:root` token block in `styles.css` and currently matches its token 1:1 — no visual defect today. The implementing engineer documented the fallbacks as a deliberate preservation of the existing wizard CSS style. |
| Impact | Silent drift risk if a token is later retuned (the fallback would mask the change wherever a token failed to resolve), plus stylistic inconsistency with the direct-token stylesheets. |
| Recommendation | Optional cleanup: strip the fallbacks so tokens have a single source of truth in `styles.css`; alternatively record the fallback style as an accepted deviation in the design spec. No visual change either way. |
| Required fix | None required (report-only); either cleanup path is acceptable in a future polish pass. |
| Status | **Open** (report-only) |

---

## Verification Evidence Caveat

Several verifications above were static (line-precise source reading) because the verifying reviewer roles' command allowances excluded test execution; where a run was executed it is stated explicitly (REQ-002/CODE-011: 34/34 component spec tests passed). The functional tester's independent full-tree validation on this branch reported: `npm run build` clean (one pre-existing style-budget warning on `booking-form.component.css`), frontend unit suite green, `dotnet build`/`dotnet test` green — see the delivery handoff `docs/handoffs/32-sdlc-orchestrator-to-product-owner-booking-ui-redesign.md` for the consolidated status, including the outstanding stale-dev-server condition blocking a clean end-to-end Playwright confirmation.

*End of Review Report — Booking UI Redesign, 2026-07-07.*
