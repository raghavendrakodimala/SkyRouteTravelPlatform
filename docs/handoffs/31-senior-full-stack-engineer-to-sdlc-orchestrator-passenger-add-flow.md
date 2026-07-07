# Handoff 31 — Senior Full Stack Engineer → SDLC Orchestrator — Passenger "Add Another" Flow

| Field | Value |
|---|---|
| Handoff ID | HO-031 |
| Date | 2026-07-07 |
| Branch | `fix/requirements-compliance-gaps-skyroute-mvp` |
| Phase | Requirements Compliance Fix (post-Phase-17) |
| From agent | senior-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | **Complete** |

---

## Work Completed

Replaced the search form's "Passengers" `<select>` dropdown (options 1–9) with an interactive,
accessible "add another passenger?" prompt flow, per the human Product Owner's decision relayed
in this task. The underlying `passengerCount` reactive `FormControl` is unchanged in shape/default
(still starts at `1`, still consumed unchanged by `onSubmit()` → `SearchRequest.passengerCount`) —
this is a UI-interaction-only change layered on top of the same control.

Nothing in `SearchRequest`, the backend, `booking-form.component.ts/html`, or
`passenger-form-section.component.*` was touched, per the explicit instruction — the booking flow
already builds one passenger-detail section per unit of `passengerCount` via its own
`passengersForm` FormArray, independent of how the count is collected on the search screen.

### Interaction implemented

- Form always starts at 1 passenger (`MIN_PASSENGERS = 1`, unchanged default).
- A live status line always shows the current count: "1 passenger added" / "3 passengers added" (proper singular/plural, not a literal "(s)").
- Immediately below it, while more can be added, an explicit prompt is shown: "Add another passenger?" with two native `<button type="button">` actions:
  - **"Yes, add another passenger"** — increments the count by 1, re-shows the same prompt (unless the cap is now reached).
  - **"No, continue with N passenger(s)"** (actual rendered text pluralizes properly, e.g. "No, continue with 3 passengers") — finalizes the count; the prompt and both buttons disappear permanently for the rest of the session; nothing else happens for this field, matching "proceeds with the rest of the form as before."
- Hard cap at 9 (`MAX_PASSENGERS = 9`, matching the pre-existing 1–9 requirement exactly). Once the count reaches 9, the "add another" prompt/buttons stop being offered automatically (no decline needed) and a brief note is shown instead: "Maximum of 9 passengers reached."
- If the user declines before reaching 9, no max-reached note is ever shown (it would be misleading/irrelevant at that point) — verified by an explicit test.

Also fixed a downstream break this UI change caused in the Playwright E2E helper (`frontend/e2e/support/helpers.ts`) so the E2E suite keeps compiling against the new markup — see "Risks and Impediments" below for why this fix could not be executed/verified in this session.

## Artifacts Created or Updated

- `frontend/src/app/features/search/search-form/search-form.component.ts` — removed `PASSENGER_COUNTS`/`passengerCounts`; added `MIN_PASSENGERS`/`MAX_PASSENGERS` constants, `describePassengerCount()` helper, `passengerCount`/`awaitingPassengerResponse` signals, `canAddPassenger`/`maxPassengersReached`/`passengerCountAnnouncement`/`continuePassengerCountLabel` computed signals, and `addPassenger()`/`declinePassengerPrompt()` methods. `form.controls.passengerCount` retained unchanged (still `nonNullable.control<number>(1)`), kept in lockstep via `setValue()` inside `addPassenger()`.
- `frontend/src/app/features/search/search-form/search-form.component.html` — removed the `<select id="passengerCount">`; added a `role="group" aria-labelledby="passengerCountLabel"` wrapper containing the live-region count display, the conditional prompt/buttons, and the conditional max-reached note. Kept the existing `<label>` + `<span class="required-indicator">(required)</span>` convention and the existing `@if (fieldError('passengerCount'))` server-error block untouched below it.
- `frontend/src/app/features/search/search-form/search-form.component.css` — added `.passenger-count-display`, `.passenger-prompt`, `.passenger-prompt-actions`, `.passenger-prompt-actions button`, `.passenger-max-note`. Reused the already-WCAG-AA-verified `#555555`-on-white pairing (≈7.46:1, per `docs/reviews/accessibility-review-phase-17.md`'s contrast table) for the max-reached note, so no new contrast finding is introduced.
- `frontend/src/app/features/search/search-form/search-form.component.spec.ts` — removed the old `renders the fixed passenger count options (1 through 9)` dropdown test; added `findButtonByText()`/`passengerCountDisplay()` helpers and 3 new tests (initial state, increment-to-cap, decline-finalizes). Extended the existing `form()` test-helper type to expose `passengerCount.value` (previously `setValue`-only). No other existing test was modified.
- `frontend/e2e/support/helpers.ts` — updated `fillSearchForm()` to click "Yes, add another passenger" `(passengerCount - 1)` times instead of `page.locator('#passengerCount').selectOption(...)`, which no longer exists. `SearchFormInput`'s shape is unchanged, so none of the 6 E2E spec files needed editing. **Not independently executed** — see Risks below.

## Decisions Made

1. **Dedicated signals instead of `computed()` over `form.controls.passengerCount.value`.** The existing `sameAirportSelected` computed in this same file reads plain (non-signal) `FormControl.value` inside `computed()`. I verified directly against this repo's installed `@angular/core` (`22.0.5`) that a `computed()` which reads zero actual `Signal`s during its first execution never re-evaluates afterwards, even when the underlying plain value later changes — confirmed with a 5-line sandbox script (`computed(() => plain.value * 10)`; mutating `plain.value` afterward and re-reading the computed still returned the stale first-run value). That is a latent bug risk for any *new* code following the same pattern; I deliberately avoided it for this feature by making `passengerCount`/`awaitingPassengerResponse` genuine `signal()`s that the derived `computed()`s actually read, so the template reliably re-renders after every button click. I did **not** touch the pre-existing `sameAirportSelected` — out of scope for this task, and it happens not to manifest as a visible bug today only because its first-ever read in both the template and the existing tests occurs after the relevant `setValue()` calls (order-of-operations luck, not correctness) — flagging this for a separate finding/ticket if the orchestrator wants it tracked, but not fixing it here since it wasn't part of this task's scope and changing it would touch code/behavior outside what was authorized.
2. **Wording chosen:** "Yes, add another passenger" / "No, continue with N passenger(s)" (properly pluralized, e.g. "No, continue with 1 passenger" vs "No, continue with 3 passengers") — both self-describing out of context, satisfying the same non-ambiguous-accessible-name standard A11Y-002 established for the "Select" button. Live-region announcement text: "N passenger(s) added" (e.g. "1 passenger added", "9 passengers added"), matching the human's suggested wording exactly. Max-reached note: "Maximum of 9 passengers reached." — the `9` is interpolated from the same `MAX_PASSENGERS` constant the cap logic uses, so the copy can never drift from the actual cap.
3. **No `<select>`/`<input>` remains for this field**, so the field's `<label>` no longer has a single native control to bind `for`/`id` to. Kept the label's exact visible text/markup convention (`<label>Passengers <span class="required-indicator">(required)</span></label>`) per instruction, gave it `id="passengerCountLabel"`, and associated it with the whole interactive area via `role="group" aria-labelledby="passengerCountLabel"` on a wrapping `<div>` — mirroring the existing `role="group" aria-label="Sort flight results"` pattern already used by `sort-control.component.html` for a comparable "group of native buttons representing one logical field" case, rather than inventing a new grouping convention.
4. **Live region choice:** `role="status" aria-live="polite"` on the persistent, always-rendered count-display `<p>` (per the human's explicit instruction to follow the pattern used for loading-state text in this same file/`booking-form.component.html`). The max-reached note uses `role="status"` alone (no explicit `aria-live`), matching the precedent set by the other *visible*, non-hidden status messages in the codebase (`confirmation.component.html`'s booking-reference paragraph, `results-list.component.html`'s loading/empty-state paragraphs) — `role="status"` already implies `aria-live="polite"` per WAI-ARIA; the explicit `aria-live="polite"` pairing in this codebase is reserved for the visually-hidden loading announcements, which I matched exactly where the human pointed to it (the count display) while following the plain-`role="status"` precedent where a visible dynamic note otherwise matches that pattern (the max-reached note).
5. **Added `aria-describedby="addPassengerPrompt"` on both prompt buttons**, pointing at the "Add another passenger?" `<p>` — additive, not required (both buttons are already fully self-describing on their own), but ties the pair together for assistive technology at no cost.
6. Left the existing `@if (fieldError('passengerCount'))` server-side field-error block exactly where it was (rendered after the new group, same as before relative to the field's other content), since server-side validation errors for this field are orthogonal to the new client-side prompt UI and nothing in the task asked to change that behavior.

## Open Questions

None blocking. Two non-blocking observations:

1. Flagged above (Decision 1): the pre-existing `sameAirportSelected` computed()-over-plain-FormControl pattern elsewhere in this same file — not part of this task's scope, not fixed here, surfaced only as an FYI in case the orchestrator wants a separate finding logged.
2. **Documentation drift found (not fixed here, out of role):** `docs/features/feature-flight-search.md` line 34 and `GAP-FS-02` (line 184) still describe the Passengers field as "Dropdown (`<select>`) with exactly 9 options, values `1`–`9`." This is now stale relative to the implemented UI (still 1–9 inclusive, still enforced, just via the add-another-passenger flow instead of a `<select>`). Per spec-driven-development role boundaries, updating a feature spec is solution-architect's/technical-writer's territory, not mine — recommend the orchestrator route a small doc update to one of them so the spec matches shipped behavior. `docs/requirements.md`/`docs/specs/non-functional-requirements.md` were not checked line-by-line for the same drift but may warrant the same pass.

## Risks and Impediments

- None blocking implementation itself. Change to application code is isolated to the 4 files listed under "search-form" above; no other application file references the removed `passengerCounts`/`PASSENGER_COUNTS` symbols (confirmed via repo-wide grep before removal). `booking-form.component.ts/html` and `passenger-form-section.component.*` were not opened for edit, per instruction.
- **Found and fixed a real regression risk in the Playwright E2E suite**, outside the originally-scoped file list: `frontend/e2e/support/helpers.ts`'s `fillSearchForm()` drove the old dropdown directly (`page.locator('#passengerCount').selectOption(...)`), which no longer exists. Left unfixed, this would have broken every E2E spec that calls it with a `passengerCount` (5 of 6 E2E spec files, e.g. `full-journey-international.spec.ts` uses `passengerCount: 3` to exercise the booking form's multi-passenger `FormArray`). Updated the helper to click "Yes, add another passenger" `(passengerCount - 1)` times instead, preserving `SearchFormInput`'s external shape exactly — no E2E spec file itself needed to change. **This fix was not executed/verified**, because this repo's Playwright config (`frontend/playwright.config.ts`) deliberately requires manually starting a real `dotnet run` backend and `ng serve` frontend first (no `webServer` auto-start), which is a long-running-process action outside this task's explicit `npm run build`/`npm test -- --watch=false` verification scope and outside what I'm authorized to start without approval. Recommend functional-tester (or the orchestrator, with approval) run the full `npm run e2e` suite against this branch before Definition of Done is declared for this fix, to independently confirm the helper change actually restores the 5 affected E2E specs to green (they were last independently confirmed 11/11 passing in Phase 13/14, per `docs/testing/execution/e2e-playwright-test-execution-summary.md` and `docs/testing/execution/phase-14-test-execution-summary.md` — that count has not been re-verified since this branch's edits).
- A quick `npx tsc --noEmit -p e2e/tsconfig.json` sanity check on the e2e project surfaced a pre-existing, unrelated environment gap (`@types/node` not resolvable) that predates this change and is not something I introduced or fixed (fixing it would mean adding a dependency, which requires approval this task didn't grant).

## Required Next Agent Action

1. Recommend the orchestrator fold this into the existing `fix/requirements-compliance-gaps-skyroute-mvp` branch's eventual review/merge pass (code review / any re-run of accessibility review the orchestrator deems appropriate for this new interactive control, given it introduces new buttons/live-region text not covered by the closed Phase 17 report).
2. Route the E2E re-run described above (functional-tester, with human approval to start the backend/frontend dev servers) before treating this fix as fully Definition-of-Done-complete.
3. Optionally route the `docs/features/feature-flight-search.md` drift (Open Questions #2) to solution-architect/technical-writer.

## Completion Criteria for Next Step

- `npm run build` in `frontend/` is clean (confirmed below).
- `npm test -- --watch=false` in `frontend/` is 0 failures (confirmed below).
- If the orchestrator wants an accessibility re-check specifically for this new control (new live region, new buttons, new group), route it to accessibility-tester scoped to `frontend/src/app/features/search/search-form/*`.

## Relevant Files

- `frontend/src/app/features/search/search-form/search-form.component.ts`
- `frontend/src/app/features/search/search-form/search-form.component.html`
- `frontend/src/app/features/search/search-form/search-form.component.css`
- `frontend/src/app/features/search/search-form/search-form.component.spec.ts`
- `frontend/src/app/shared/models/search-request.model.ts` (read only, confirmed unchanged/no edit needed)
- `frontend/src/app/features/booking/booking-form/booking-form.component.html` (read only, referenced for the `role="status" aria-live="polite"` precedent)
- `frontend/src/app/features/results/sort-control/sort-control.component.html` (read only, referenced for the native-button/`role="group"` precedent)
- `docs/reviews/accessibility-review-phase-17.md` (read only, referenced for the reused contrast ratio and the A11Y-002 accessible-name standard)

## Commands Run and Results

```text
cd frontend && npm test -- --watch=false     # BEFORE any edit (baseline)
  Test Files  17 passed (17)
  Tests       149 passed (149)
  Build: Application bundle generation complete. [3.263 seconds]

cd frontend && npm run build                  # AFTER edits
  Application bundle generation complete. [4.016 seconds] — 0 errors

cd frontend && npm test -- --watch=false      # AFTER edits
  Test Files  17 passed (17)
  Tests       151 passed (151)
  Duration    11.40s
```

**Test count: 149 → 151** (net +2: removed 1 obsolete dropdown-option test, added 3 new button-flow tests — initial-state, increment-to-cap, decline-finalizes). 0 failures before or after.

```text
node -e "const { computed } = require('@angular/core'); ..."   # sandbox check supporting Decision 1
  first read 10
  second read after plain mutation 10   (confirms computed() over a non-signal read never re-evaluates)
```

No dependencies were added, no files were deleted, no destructive commands were run.

---

*End of Handoff 31.*
