# Handoff HO-024 — A11Y-002 Fix (Non-Unique "Select" Button Accessible Names)

| Field | Value |
|---|---|
| Handoff ID | HO-024 |
| Date | 2026-07-07 |
| Branch | `sdlc/17-accessibility-review-skyroute-mvp` |
| Phase | Phase 17 — Accessibility Review (Iterative Review-Fix Loop) |
| From agent | senior-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete — ready for re-verification |

## Work completed

Fixed finding **A11Y-002** (Medium, WCAG 4.1.2 Name/Role/Value + 2.4.6 Headings and Labels) from `docs/reviews/accessibility-review-phase-17.md`: every "Select" button on the results screen previously had an identical accessible name ("Select"), so a screen reader announced "Select, Select, Select..." with no way to distinguish which flight result each button acted on.

Added a per-row `aria-label` to each Select button, computed from the same itinerary data already rendered visually in the row (provider, flight number, origin/destination city labels, formatted departure time, and total price text) — so the accessible name and the visible card content can never drift apart. The visible button text remains unchanged ("Select").

Implementation detail: added a new `selectButtonLabel(result: FlightResult): string` method to `ResultsListComponent` that reuses the component's existing `cityLabel()`, `formatFlightTime()`, and `totalPriceText()` helpers rather than duplicating formatting logic, and bound it via `[attr.aria-label]` on the button in the template.

## Artifacts created or updated

- `frontend/src/app/features/results/results-list/results-list.component.html` — added `[attr.aria-label]="selectButtonLabel(result)"` to the per-row Select `<button>` (was at line 30; visible text "Select" unchanged).
- `frontend/src/app/features/results/results-list/results-list.component.ts` — added `selectButtonLabel(result: FlightResult): string` method, producing e.g. `"Select GlobalAir flight GA100, London (LHR) to New York (JFK), departing 09:00, $750.00 total"` (exact wording depends on `cityLabel`/`formatFlightTime`/`totalPriceText` output).
- `frontend/src/app/features/results/results-list/results-list.component.spec.ts` — added a new test, `gives each Select button a unique, descriptive accessible name (A11Y-002)`, that renders two result cards with different provider/flight-number data and asserts: both buttons have a non-empty `aria-label`, the two labels are not equal (uniqueness), each label contains its row's flight number and provider, and the visible button text is still exactly `Select`.

## Decisions made

- Used `aria-label` (interpolated string) rather than `aria-labelledby` referencing existing card text nodes, since the card's visual fields (route, provider/flight number, timing, price) are spread across several sibling `<span>` elements with no single referenceable container — a single computed `aria-label` string was simpler and matches the review's suggested example verbatim in form.
- Reused existing formatting helpers (`cityLabel`, `formatFlightTime`, `totalPriceText`) inside the new `selectButtonLabel` method instead of re-deriving strings, so the accessible name always matches whatever the visible row currently shows (no risk of divergence if formatting logic changes later).
- Did not change the visible "Select" button text, per the task instruction to keep the visual UI unchanged.
- Did not touch `<h1>Flight Results</h1>` (line 2) or any other heading — that is A11Y-006, owned by the junior-developer's upcoming fix on this same file.

## Open questions

None.

## Risks and impediments

- **Sequencing risk (flagged by orchestrator, now mitigated by prompt completion):** a junior-developer is expected to touch heading hierarchy (A11Y-006) on this same `results-list.component.html` file shortly after this fix. This change only touched the `<button>` element inside the `@for` loop (line ~30) and the component's `.ts`/`.spec.ts` files — it did not touch the `<h1>` at line 2, so there should be no line-level overlap, but the orchestrator should confirm the junior-developer rebases/re-reads the file before editing since line numbers shifted slightly (the button now spans 4 lines instead of 1 due to attribute formatting).
- No other risks identified. No new dependencies introduced. No destructive commands run.

## Required next agent action

Re-invoke **accessibility-tester**, scoped to `frontend/src/app/features/results/results-list/results-list.component.html` and `results-list.component.ts`, to verify finding A11Y-002 and set its status to `Resolved` in `docs/reviews/accessibility-review-phase-17.md` (this agent did not edit that report, per instructions).

## Completion criteria for next step

- accessibility-tester confirms each rendered Select button now has a unique, self-describing accessible name derived from that row's flight data (not just visually distinct proximity text).
- accessibility-tester confirms the visible button text is unchanged ("Select").
- accessibility-tester updates A11Y-002's status from `Open` to `Resolved` in the Phase 17 review report.

## Relevant files

- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\results\results-list\results-list.component.html`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\results\results-list\results-list.component.ts`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\results\results-list\results-list.component.spec.ts`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\reviews\accessibility-review-phase-17.md` (read-only reference — not edited by this agent)

## Commands run and results

```text
cd frontend && npm run build
  → Application bundle generation complete. [2.133 seconds]
  → Output location: frontend/dist/frontend
  → No errors.

cd frontend && npm test   (runs `ng test`, which runs vitest)
  → Test Files  16 passed (16)
  → Tests       146 passed (146)
  → Duration    3.78s
  → No failures. (145 prior tests + 1 new A11Y-002 test = 146.)
```

No `git commit`/`git merge` performed. No dependencies installed. No files deleted.
