# Handoff HO-025 — A11Y-003/004/005/006 Fix (Page Title, Required-Field Indicators, Loading Live Region, Heading Hierarchy)

| Field | Value |
|---|---|
| Handoff ID | HO-025 |
| Date | 2026-07-07 |
| Branch | `sdlc/17-accessibility-review-skyroute-mvp` |
| Phase | Phase 17 — Accessibility Review (Iterative Review-Fix Loop) |
| From agent | junior-developer |
| To agent | sdlc-orchestrator |
| Status | Complete — ready for re-verification |

## Work completed

Fixed all four Low-severity findings assigned from `docs/reviews/accessibility-review-phase-17.md`:

**A11Y-003 (static, non-descriptive `<title>`, WCAG 2.4.2):** Added a distinct `title` string to each of the four routes in `app.routes.ts` ("Search Flights — SkyRoute", "Flight Results — SkyRoute", "Booking Details — SkyRoute", "Booking Confirmed — SkyRoute"). Angular Router's built-in `DefaultTitleStrategy` calls the `Title` service (`@angular/platform-browser`, `providedIn: 'root'`) automatically on every successful navigation when a route declares `title` — no extra provider was needed beyond the existing `provideRouter(routes)` in `app.config.ts`, and no new dependency was introduced. Also replaced the scaffold-default `<title>Frontend</title>` in `index.html` with a descriptive base title, `SkyRoute — Flight Search and Booking`.

**A11Y-004 (no required-field indicator, WCAG 3.3.2):** Added the native `required` attribute and `aria-required="true"` to all 8 mandatory fields — the 5 search-form fields (Origin, Destination, Departure Date, Passengers, Cabin Class — all 5 are effectively mandatory since the search form has no optional field, per the finding's own evidence) and the 3 passenger-form-section fields (Full Name, Email Address, the document-number field). Also added a visible `(required)` cue as a `<span class="required-indicator">` inside each field's `<label>`, styled in a de-emphasised grey (`#555555` on white, ≈7.46:1 contrast — reusing a ratio already verified safe in the Phase 17 report's contrast table for `.per-person-price`/`.lead-badge`) so it reads as a hint rather than a warning.

**A11Y-005 (loading-state text not in a live region):** Added a `role="status" aria-live="polite"` paragraph next to each of the two submit buttons (search-form's "Search"/"Searching…" and booking-form's "Confirm Booking"/"Confirming…"), shown only while `loading()` is true, with wording ("Searching for flights…" / "Submitting your booking…") independent of the button's own visible text. The new paragraph is visually hidden (a new `.visually-hidden` CSS class, clip-based off-screen technique) since the button's own text already shows the state visibly — the live region exists purely so assistive tech announces the state change regardless of where focus currently is, per the finding's recommendation. Button text/behaviour is otherwise unchanged.

**A11Y-006 (inconsistent heading hierarchy):** `search-form` (`<h1>Search Flights</h1>`) and `results-list` (`<h1>Flight Results</h1>`) already had a single, page-purpose `<h1>` each and needed no change — the finding's own recommendation explicitly accepts the app-shell's separate `<h1>SkyRoute</h1>` (in `<header>`, outside `<main>`) as a non-violating landmark/branding pattern. Fixed the two screens that were actually broken:
- `booking-form.component.html`: added a new page-purpose `<h1>Booking Details</h1>` (rendered unconditionally, even in the "no flight selected" fallback branch) and demoted the former `<h1>{{ f.origin }} → {{ f.destination }}</h1>` route-code heading to `<h2>` (matching CSS selector `.flight-summary h2` updated accordingly).
- `confirmation.component.html`: added a new page-purpose `<h1>Booking Confirmed</h1>` (rendered unconditionally, even in the "no booking" fallback branch); the existing `<h2>`/`<h3>` sub-headings (flight summary, passengers) were left unchanged since they were already one level below where a page `<h1>` belongs.

Confirmed compatibility with `RouteFocusService` (`frontend/src/app/core/services/route-focus.service.ts`, added by another engineer for A11Y-001): its selector `main h1, main h2, main [role="heading"]` will now find a real `<h1>` inside `<main>` on all four screens, including in the two fallback branches that previously had no heading in the booking/confirmation cases when `flight()`/`booking()` were null — an incidental robustness improvement for that service.

Two pre-existing unit tests broke as a direct, expected consequence of the A11Y-004/A11Y-006 markup changes and were updated (not the app behaviour they test, only the assertion to match the corrected accessible markup):
- `booking-form.component.spec.ts`: `'renders the flight summary...'` queried `.flight-summary h1`, updated to `.flight-summary h2` (the flight-route heading is now `<h2>`, not `<h1>`).
- `passenger-form-section.component.spec.ts`: two tests asserted the document-number `<label>`'s `textContent` was exactly `'Passport Number'`/`'National ID'` (`toBe`); changed to `toContain` since the label now also contains the new visible `(required)` text — the document-label functionality under test (dynamic label text) is unchanged and still verified.

## Artifacts created or updated

- `frontend/src/app/app.routes.ts` — added `title` to all 4 routes (A11Y-003).
- `frontend/src/index.html` — replaced scaffold `<title>Frontend</title>` with a descriptive default (A11Y-003).
- `frontend/src/app/features/search/search-form/search-form.component.html` — added `required`/`aria-required`/visible `(required)` span to 5 fields (A11Y-004); added a `role="status"` live-region paragraph for the loading state (A11Y-005).
- `frontend/src/app/features/search/search-form/search-form.component.css` — added `.required-indicator` and `.visually-hidden` classes.
- `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.html` — added `required`/`aria-required`/visible `(required)` span to 3 fields (A11Y-004).
- `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.css` — added `.required-indicator` class.
- `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.spec.ts` — updated 2 assertions from `toBe` to `toContain` (see above).
- `frontend/src/app/features/booking/booking-form/booking-form.component.html` — added page `<h1>Booking Details</h1>`, demoted route-code heading to `<h2>` (A11Y-006); added a `role="status"` live-region paragraph for the loading state (A11Y-005).
- `frontend/src/app/features/booking/booking-form/booking-form.component.css` — updated `.flight-summary h1` selector to `.flight-summary h2`; added `.visually-hidden` class.
- `frontend/src/app/features/booking/booking-form/booking-form.component.spec.ts` — updated 1 selector from `.flight-summary h1` to `.flight-summary h2` (see above).
- `frontend/src/app/features/confirmation/confirmation/confirmation.component.html` — added page `<h1>Booking Confirmed</h1>` (A11Y-006).

Not edited: `docs/reviews/accessibility-review-phase-17.md` (per instructions — reviewer-only file).

## Decisions made

- Used Angular Router's native `title` route-config property (`DefaultTitleStrategy`) rather than a custom `TitleStrategy`/per-component `Title` service call, since it needed zero new providers/dependencies and is the simplest, most idiomatic Angular pattern for this exact case — consistent with the finding's own "either approach is acceptable" framing.
- Applied the required-field indicator to all 5 search-form fields (not just the 3 backed by an explicit `Validators.required`), because the finding's own evidence text states "every field in both forms is, in fact, required (the search form has no optional field)" and explicitly recommends applying the fix "consistently across both forms since every field in both forms is mandatory" — matching the task's stated total of 8 fields (5 + 3).
- Used a visible `(required)` text span (not an asterisk) for consistency with the finding's own suggested wording and to avoid introducing an asterisk-to-meaning convention that would need its own explanatory legend.
- Used a visually-hidden live-region paragraph (clip-based CSS technique) for the loading-state announcement rather than a second visible copy of the loading text, to avoid duplicating the same visible text twice next to the button; this still satisfies the finding's own suggested alternative of "a visually-hidden or already-visible sibling paragraph."
- Made the two new page `<h1>`s (`booking-form`, `confirmation`) render unconditionally (outside the `@if (flight()/booking())` block) rather than only in the success branch, so the fallback/empty-state branches also expose a findable heading in `<main>` — this was not explicitly required by the finding but keeps both screens compatible with `RouteFocusService`'s heading-search fallback logic in every state, not only the happy path.
- Left `search-form`/`results-list` headings untouched, since the finding's own recommendation treats the current "one page `<h1>` + the separate app-shell `<h1>` outside `<main>`" pattern as already compliant on those two screens.

## Open questions

None.

## Risks and impediments

None identified. No new dependencies were introduced (Angular's `Title` service and Router `title` route property are already part of `@angular/platform-browser`/`@angular/router`, already installed). No destructive commands were run. No files were deleted.

## Required next agent action

Re-invoke **accessibility-tester**, scoped to the files listed below, to verify findings A11Y-003, A11Y-004, A11Y-005, and A11Y-006 and set their status from `Open` to `Resolved` in `docs/reviews/accessibility-review-phase-17.md` (this agent did not edit that report, per instructions).

## Completion criteria for next step

- accessibility-tester confirms each of the four routes now produces a distinct, descriptive `document.title` after navigation (A11Y-003).
- accessibility-tester confirms all 8 required fields (5 search-form + 3 passenger-form-section) now carry `required`/`aria-required="true"` and a visible `(required)` cue (A11Y-004).
- accessibility-tester confirms both submit buttons' loading states are now mirrored in a `role="status"`/`aria-live="polite"` element independent of the button's own text (A11Y-005).
- accessibility-tester confirms all four routed screens now have exactly one clear, page-purpose `<h1>` inside `<main>`, with no duplicate/misused `<h1>` remaining (A11Y-006).
- accessibility-tester updates A11Y-003/004/005/006's status from `Open` to `Resolved` in the Phase 17 review report.

## Relevant files

- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\app.routes.ts`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\index.html`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\search\search-form\search-form.component.html`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\search\search-form\search-form.component.css`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\passenger-form-section\passenger-form-section.component.html`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\passenger-form-section\passenger-form-section.component.css`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\passenger-form-section\passenger-form-section.component.spec.ts`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\booking-form\booking-form.component.html`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\booking-form\booking-form.component.css`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\booking\booking-form\booking-form.component.spec.ts`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\features\confirmation\confirmation\confirmation.component.html`
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\frontend\src\app\core\services\route-focus.service.ts` (read-only reference — confirmed compatible, not edited by this agent)
- `C:\Users\RaghavendraKodimala\source\repos\SkyRouteTravelPlatform\docs\reviews\accessibility-review-phase-17.md` (read-only reference — not edited by this agent)

## Commands run and results

```text
cd frontend && npm run build
  → Application bundle generation complete. [2.214 seconds]
  → Output location: frontend/dist/frontend
  → No errors.

cd frontend && npm test   (runs `ng test`, which runs vitest)
  → First run (before updating the 2 pre-existing spec assertions broken by the A11Y-004/A11Y-006
    markup change): Test Files 1 failed | 16 passed (17); Tests 2 failed | 147 passed (149).
  → After updating passenger-form-section.component.spec.ts (toBe → toContain, 2 assertions) and
    booking-form.component.spec.ts (.flight-summary h1 → .flight-summary h2, 1 selector):
    Test Files  17 passed (17)
    Tests       149 passed (149)
    Duration    3.64s
  → No failures, no regressions against the 149-test baseline.
```

No `git commit`/`git merge` performed. No dependencies installed. No files deleted. No branch switch performed (remained on `sdlc/17-accessibility-review-skyroute-mvp` throughout).
