# Handoff: Booking UI Redesign — Implemented, Reviewed, Documented; Awaiting Human Verification and Merge Approval

| Field | Value |
|---|---|
| Handoff ID | HO-032 |
| Date | 2026-07-07 |
| Branch | `fix/requirements-compliance-gaps-skyroute-mvp` (all work **uncommitted** in the working tree) |
| Phase | Booking UI redesign (post-Phase-17, PO-directed; outside the numbered SDLC phase sequence) |
| From agent | sdlc-orchestrator |
| To agent | product-owner (human) |
| Status | Implemented + reviewed + documented — awaiting human verification and merge approval |

---

## Work Completed

1. **Backend route filtering** (carried on this branch from HO-028): `ProviderScheduleMapper.BuildResults()` filters each provider's fixed schedule to case-insensitive origin/destination matches; unmatched routes return an empty list (valid empty state); the departure date still does not filter. Provider/integration tests rewritten accordingly. `docs/requirements.md` ASM-006 revised to v1.5.
2. **Visual design spec applied** (DESIGN-VISUAL-001): design-token block in `styles.css`, system font stack, form-control/button/card recipes, focus-visible outline, reduced-motion support — CSS-only, across the global and feature stylesheets.
3. **Booking passenger flow redesigned** per the PO directive (no all-at-once passenger forms): passengers entered one at a time with saved-passenger summary cards (Edit/Remove, positional renumbering), live price breakdown, 9-passenger cap, full focus choreography (aria-disabled + click guards, never native `disabled`), persistent polite live region, `passengers[i].*` server-error mapping that reopens the offending passenger, parked-draft protection, and `bookingLeaveGuard` (canDeactivate) + `beforeunload` leave guards. `BookingRequest` wire shape unchanged (`passengerCount = passengers.length`).
4. **Search form**: DESIGN-FLOW-001 Part A first restored the native `<select id="passengerCount">` (1–9), superseding HO-031's yes/no counter; a subsequent PO UX correction (2026-07-07) then **removed the search passenger field entirely** — the form now captures origin/destination/date/cabin class only and always sends `passengerCount: 1`. The A11Y-007/A11Y-008 submit-button fix (aria-disabled + submit-attempt validation, never natively disabled) is in place. CR-006 (stale `ProviderScheduleMapper` doc-comment clause) fixed.
5. **Review loop closed** (report: `docs/reviews/booking-ui-redesign-review-2026-07-07.md`): CR-001 (unregistered leave guard, High), CR-002 (parked-draft silent discard, Medium), CR-003 (confirmed back-nav breakdown used searched count, Medium), and the two search-form accessibility findings (Medium) all fixed and independently verified **Resolved**; six Low findings filed **Open, report-only** (advisory; no merge blocker). Zero unresolved Critical/High.
6. **Documentation updated** (technical-writer, this handoff's authoring pass): `README.md` (passenger flow + route-filtering trade-off, closing old CR-002); `docs/features/feature-provider-aggregation.md` v1.1 (Sections 1–3, 5, 6.1, 8 now match route filtering, closing old CR-003); `docs/features/feature-flight-search.md` v1.1 and `docs/features/feature-booking-flow.md` v1.1 (passenger-entry reality, statuses Implemented); both design specs' statuses set to Implemented (2026-07-07); delegation log and workflow state updated.

## Artifacts Created or Updated

- `README.md`
- `docs/requirements.md` (v1.5, ASM-006 revision — pre-existing on this branch)
- `docs/features/feature-provider-aggregation.md` (v1.1), `feature-flight-search.md` (v1.1), `feature-booking-flow.md` (v1.1)
- `docs/design/visual-design-spec.md`, `docs/design/booking-passenger-flow-spec.md` (statuses → Implemented 2026-07-07)
- `docs/reviews/booking-ui-redesign-review-2026-07-07.md` (new)
- `docs/handoffs/32-…` (this file), `current-handoff.md`, `handoff-index.md`, `workflow-state.md`
- `docs/delivery/delegation-log.md` (DEL-024–DEL-030)
- Source: `frontend/src/app/features/booking/**`, `frontend/src/app/features/search/search-form/**`, `frontend/src/app/app.routes.ts`, stylesheets, `src/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs`, backend tests, `frontend/e2e/**`

## Decisions Made

1. **Search passenger select restored, then superseded.** DESIGN-FLOW-001 Part A restored the plain native passenger select per challenge PDF §3.1, superseding the "Add another passenger?" yes/no counter from HO-031. A later PO UX correction the same day removed the search passenger field altogether: **passenger count is now determined on the booking screen**, and `SearchRequest.passengerCount` is always `1` (the API contract still validates 1–9). Result totals on `/results` therefore effectively show the per-passenger fare.
2. **Booking wizard per PO directive, as amended.** The implemented shape is the PO UX correction's **single in-place passenger form** (persistent "Add another passenger" / "Confirm Booking" actions; "Save changes" / "Cancel edit" while editing) rather than DESIGN-FLOW-001 Part B's save → prompt → review wizard. Part B's cross-cutting mandates (focus management, live region, Edit/Remove correction, server-error repair loop, leave guards, 9-cap) are preserved in the amended flow.
3. **Visual design spec applied** CSS-only; no template/behavior changes from that workstream.
4. **Backend contract unchanged** throughout; no backend behavior changes beyond the route filtering already on this branch.
5. Six Low review findings held as **report-only Open** items for a future polish pass rather than routed through a fix loop now.

## Open Questions

1. **PO confirmation requested — search passenger count.** The challenge PDF (authoritative client requirement) specifies a passenger-count field (1–9) on the *search* form and passenger-count-aware totals on the results screen. The implemented tree omits that field per the PO UX correction and prices results for one passenger. Please explicitly confirm this deviation is the intended final scope for submission, or direct restoration of the Part A select.
2. Should the six report-only Low findings (and the `booking-form.component.css` style-budget warning) be scheduled into a polish pass before submission?

## Risks and Impediments

1. **E2E suite is stale against the final flow (blocking a clean e2e run).** `frontend/e2e/support/helpers.ts` and the specs still drive the superseded prompt/review wizard and the removed `#passengerCount` search select ("Save passenger", "Add another passenger?" prompt, `#review-heading` — none exist in the current app). The earlier e2e run also hit a stale `ng serve` with a dead file watcher. Required: restart the frontend dev server, update the e2e suite to the in-place flow, and re-run `npx playwright test` (functional-tester task, needs the operator-owned servers).
2. **Test-count claims predate the final PO UX correction.** The 175/175 frontend unit figure was reported against the prompt/review wizard tree; the booking spec suite has since been rewritten for the in-place flow. A fresh `npm test -- --watch=false`, `npm run build`, and `dotnet test SkyRoute.slnx` run should be executed before merge.
3. All work is uncommitted on a shared fix branch — no phase-transaction isolation; nothing may be committed/merged without explicit human instruction.

## Required Next Agent Action

**Human (Product Owner):** verify the redesign live (restart `ng serve` from `frontend/`, exercise search → results → booking → confirmation), answer Open Question 1, then either approve commit + merge of `fix/requirements-compliance-gaps-skyroute-mvp` to `main` or direct changes. If approved, the orchestrator should first route the e2e refresh + full test re-run (Risk 1/2) to functional-tester.

## Completion Criteria for the Next Step

- Human verdict recorded on Open Question 1 (search passenger-count deviation).
- Fresh green build/unit/backend test evidence against the final tree; e2e suite updated and green (or an explicit human waiver).
- Explicit human approval to commit and merge; working tree clean at merge time.

## Relevant Files

- `frontend/src/app/features/booking/booking-form/booking-form.component.{ts,html,css}`
- `frontend/src/app/features/booking/booking-leave.guard.ts`
- `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.{ts,html,css}`
- `frontend/src/app/features/search/search-form/search-form.component.{ts,html,css,spec.ts}`
- `frontend/src/app/app.routes.ts`, `frontend/src/styles.css`
- `src/SkyRoute.Infrastructure/Providers/ProviderScheduleMapper.cs`
- `docs/reviews/booking-ui-redesign-review-2026-07-07.md`
- `docs/design/booking-passenger-flow-spec.md`, `docs/design/visual-design-spec.md`
- `docs/features/feature-booking-flow.md`, `feature-flight-search.md`, `feature-provider-aggregation.md`
- `README.md`, `frontend/e2e/support/helpers.ts` (stale — see Risk 1)
