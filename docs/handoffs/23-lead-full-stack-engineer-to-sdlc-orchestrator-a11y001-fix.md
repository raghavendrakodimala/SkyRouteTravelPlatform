# Handoff HO-023 — A11Y-001 Fix (Focus Management on Route Transitions)

| Field | Value |
|---|---|
| Handoff ID | HO-023 |
| Date | 2026-07-07 |
| Branch | `sdlc/17-accessibility-review-skyroute-mvp` |
| Phase | Phase 17 — Accessibility Review (Iterative Review-Fix Loop) |
| From agent | lead-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Fix complete — ready for accessibility-tester re-verification |

---

## Work Completed

Fixed finding **A11Y-001** (Medium — `docs/reviews/accessibility-review-phase-17.md`, NFR-A11Y-006, WCAG 2.1 SC 2.4.3 Focus Order): no focus-management mechanism existed anywhere in the routing layer, so focus dropped to `<body>` after each of the 4 primary screen transitions (`/search`→`/results`→`/booking`→`/confirmation`→`/search`).

Implemented a single, centralized focus-management service, per the finding's own recommendation (one mechanism, not four one-off fixes):

1. **`frontend/src/app/core/services/route-focus.service.ts`** (new) — `RouteFocusService`, an injectable (`providedIn: 'root'`) that subscribes to `Router.events`, filters for `NavigationEnd`, and on every navigation after the first:
   - Defers via `setTimeout` one macrotask so the new route's component has finished rendering.
   - Resolves a focus target with priority order: first `main h1`, else `main h2`, else `main [role="heading"]` (document-order `querySelector`, so it finds whichever heading level a given screen currently uses — deliberately generic, since A11Y-006's heading-hierarchy inconsistency is a separate finding not touched here), else falls back to the routed view's own root element inside `<main>` (first child of `<main>` that isn't the `<router-outlet>` anchor itself) for robustness if a future screen has no heading at all.
   - Sets `tabindex="-1"` on the target if not already present, then calls `.focus()`.
   - The very first navigation (initial app bootstrap, `''` → `/search`) is intentionally skipped — the finding is scoped to the 4 transitions *between* already-loaded screens, not initial page load.
   - Subscription lifetime is managed with `takeUntilDestroyed(DestroyRef)`.
2. **`frontend/src/app/app.ts`** (edited) — root `App` component now injects `RouteFocusService` and calls `.start()` once in its constructor, so the subscription is established at app bootstrap and lives for the app's lifetime. No template/`app.html` change was needed.
3. **`frontend/src/app/core/services/route-focus.service.spec.ts`** (new) — 3 Vitest/TestBed tests using two stub routed components (`<h1>Screen A</h1>` / `<h1>Screen B</h1>`) behind a `<main><router-outlet /></main>` host mirroring the real `app.html` shell:
   - Does not move focus on the very first navigation.
   - Moves focus to the new screen's `<h1>` after a subsequent navigation, and sets `tabindex="-1"` on it.
   - Does not leave `document.activeElement` on `<body>` after a screen transition (the exact NFR-A11Y-006 target outcome named in the finding).

No changes were made to `SearchFormComponent`, `ResultsListComponent`, `BookingFormComponent`, or `ConfirmationComponent` themselves — the mechanism is entirely centralized in the new service plus the one-line wiring in `app.ts`, exactly matching the finding's recommendation to avoid four one-off fixes.

## Artifacts Created / Updated

- Created: `frontend/src/app/core/services/route-focus.service.ts`
- Created: `frontend/src/app/core/services/route-focus.service.spec.ts`
- Updated: `frontend/src/app/app.ts`
- Created: `docs/handoffs/23-lead-full-stack-engineer-to-sdlc-orchestrator-a11y001-fix.md` (this file)

Not touched: `docs/reviews/accessibility-review-phase-17.md` (reviewer-owned; status update is accessibility-tester's action, not mine).

## Decisions Made

- Chose a dedicated injectable service (`RouteFocusService`) over a directive-per-page or four inline `.focus()` calls, per the finding's explicit recommendation for a single centralized mechanism.
- Focus-target resolution is heading-level-agnostic (`h1` → `h2` → `[role="heading"]` → fallback root element) rather than hard-coded to `<h1>`, because `ConfirmationComponent` currently has no `<h1>` at all (its first heading is `<h2>`, per A11Y-006 — a separate, not-yet-fixed finding). This keeps A11Y-001's fix independent of A11Y-006's fix landing in either order, without needing to touch `confirmation.component.html`.
- The first (`NavigationEnd`) event since app bootstrap is deliberately not focus-managed, matching the finding's own scope (4 *transitions*, not initial load) and common Angular Router accessibility-pattern practice of not stealing focus away from the browser's own initial-page-load state.
- Used a real macrotask (`setTimeout`) rather than `requestAnimationFrame`/`afterNextRender` to defer the DOM query, matching the simplicity of the existing codebase (no other component uses `afterNextRender`) and confirmed reliable both in the Vitest/jsdom test environment and structurally against Angular's synchronous post-`NavigationEnd` rendering.

## Open Questions

- None. The fix is self-contained and does not depend on A11Y-002 through A11Y-006, which are being handled by other agents in parallel per the task boundary given.

## Risks / Impediments

- None identified. `git status` shows other agents' concurrent edits to `results-list.component.*` (A11Y-002 fix) and `docs/handoffs/current-handoff.md`/`handoff-index.md` in the same working tree; I did not touch those files to avoid clobbering that parallel work — only the 3 files listed above plus this handoff note were created/edited by me.

## Required Next Agent Action

Re-invoke **accessibility-tester**, scoped to `frontend/src/app/core/services/route-focus.service.ts`, `frontend/src/app/app.ts`, and finding ID `A11Y-001`, to verify the fix per the Iterative Review-Fix Loop (CLAUDE.md §22) and update the finding's status in `docs/reviews/accessibility-review-phase-17.md` from `Open` to `Resolved` (or file a new finding if a gap remains).

## Completion Criteria for Next Step

- accessibility-tester confirms `document.activeElement` is not `<body>` immediately after each of the 4 route transitions and that the target element is a sensible landing point (heading or screen root).
- `docs/reviews/accessibility-review-phase-17.md` finding A11Y-001 status updated to `Resolved` by accessibility-tester (not by me).

## Relevant Files

- `frontend/src/app/core/services/route-focus.service.ts`
- `frontend/src/app/core/services/route-focus.service.spec.ts`
- `frontend/src/app/app.ts`
- `frontend/src/app/app.html` (read only, unchanged — confirms `<main><router-outlet /></main>` shell the service relies on)
- `frontend/src/app/app.routes.ts` (read only, unchanged)
- `docs/reviews/accessibility-review-phase-17.md` (finding A11Y-001, read only)

---

## Build / Test Evidence

Commands run from `frontend/`:

```
npm run build
```
Result: `Application bundle generation complete. [2.365 seconds]` — no errors, no warnings.

```
npm test -- --watch=false
```
Result: `Test Files  17 passed (17)` / `Tests  149 passed (149)` (146 pre-existing + 3 new `RouteFocusService` tests). Zero failures.

No `dotnet build` was run — this fix touches only the Angular frontend (`frontend/src/app/`), no backend files.

*End of Handoff HO-023.*
