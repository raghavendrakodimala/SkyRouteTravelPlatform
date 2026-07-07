# Accessibility Review Report — Phase 17

| Field | Value |
|---|---|
| Document ID | A11Y-PHASE-17 |
| Date | 2026-07-07 |
| Branch | `sdlc/17-accessibility-review-skyroute-mvp` |
| Phase | Phase 17 — Accessibility Review |
| Reviewer | accessibility-tester |
| Scope | `frontend/src/app/features/search/`, `frontend/src/app/features/results/`, `frontend/src/app/features/booking/`, `frontend/src/app/features/confirmation/`, `frontend/src/app/app.html`/`app.ts`/`app.css`, `frontend/src/app/app.config.ts`, `frontend/src/app/app.routes.ts`, `frontend/src/app/core/guards/booking-flow.guards.ts`, `frontend/src/index.html`, `frontend/src/styles.css` |
| Reference baselines | `docs/specs/non-functional-requirements.md` Section 8 (NFR-A11Y-001–006), `docs/requirements.md` Section 5.5 (WCAG 2.1 Level AA baseline), `docs/reviews/code-review-phase-15.md`, `docs/reviews/security-review-phase-16.md` |
| Standards referenced | WCAG 2.1 (W3C) success criteria 1.3.1, 1.4.3, 2.1.1, 2.1.2, 2.4.2, 2.4.3, 2.4.6, 3.3.2, 4.1.2 — cited by number/name from general knowledge of the WCAG 2.1 recommendation, consistent with its citation in `docs/requirements.md` Section 5.5 and `docs/specs/non-functional-requirements.md` Section 8; WAI-ARIA (`role="alert"`/`role="status"` live-region semantics) |
| Method | Static/manual code inspection only — no live browser, screen reader, or automated axe-core/Lighthouse run was available in this environment. Keyboard operability, label association, live-region usage, and colour contrast were assessed by reading component templates/TypeScript/CSS directly and, for contrast, by computing WCAG relative-luminance/contrast-ratio values from the actual hex colour pairs found in the CSS (methodology: sRGB→linear gamma correction per the WCAG 2.1 contrast formula, then `(L1+0.05)/(L2+0.05)`) |

---

## Summary

This review examined the full Angular 22 standalone-component frontend delivered through Phase 12–16 (search form, search results, booking/passenger form, and booking confirmation screens) against the six accessibility NFRs the Solution Architect scoped for this MVP in `docs/specs/non-functional-requirements.md` Section 8 (NFR-A11Y-001–006), with WCAG 2.1 Level AA as the governing baseline per `docs/requirements.md` Section 5.5.

**The core form and error-handling patterns are solid and consistent file-to-file.** Every form field across all four screens uses a native HTML control (`<select>`, `<input type="date"/"text"/"email">`) with a `<label for="...">` bound to a unique `id`, satisfying **NFR-A11Y-002** with zero placeholder-only fields found. Every interactive control is a native `<button>`, `<select>`, or `<input>` — no `<div>`/`<span>` click-only handlers were found anywhere in the four feature areas — so **NFR-A11Y-001** (keyboard operability, no keyboard trap) is satisfied structurally: native elements receive Tab focus and respond to Enter/Space by default, and no custom widget, modal, or focus-trapping construct exists in the codebase. Every validation error, API error banner, and empty-state message found is wrapped in `role="alert"` (assertive) or `role="status"` (polite), satisfying **NFR-A11Y-003** with zero unannounced dynamic messages found. The booking reference (`b.bookingReference`) is rendered as a plain text node inside a `role="status"` paragraph — not an image, not `aria-hidden`, present in document order — satisfying **NFR-A11Y-005** exactly as specified. Colour contrast was computed for every distinct text/background colour pair found in the four component stylesheets plus the shared header; every pair computed at or above the required 4.5:1 (normal text) / 3:1 (large text) ratio, with the tightest margin being the confirmation screen's booking-reference text (`#1a5fb4` on `#eef4fc`, ≈5.69:1, large text, threshold 3:1), so **NFR-A11Y-004** is satisfied with no finding raised.

**NFR-A11Y-006 (Should Have — logical focus order) is not met.** No focus-management mechanism exists anywhere in the routing layer: `app.config.ts` registers a plain `provideRouter(routes)` with no focus/scroll-restoration extra, and a repository-wide search found zero occurrences of `.focus(`, `cdkFocusInitial`, `autofocus`, or `tabindex` in `frontend/src`. Because each of the four screens is a full component swap behind a single `<router-outlet>` with no intervening element retained across navigations, the element that had focus at the moment of navigation (the "Select" button, the "Confirm Booking" button, etc.) is removed from the DOM as part of the swap, and per default browser behaviour this drops focus to `<body>` on every one of the four screen transitions (`/search`→`/results`, `/results`→`/booking`, `/booking`→`/confirmation`, `/confirmation`→`/search`) — the exact failure mode the NFR's target explicitly calls out as non-compliant. This is filed as **A11Y-001 (Medium)**.

Five further findings were raised, none of which map to an explicit Open item in the six core NFRs but which are genuine WCAG 2.1 AA-baseline gaps found during the walkthrough: ambiguous, non-unique "Select" button names across every result card (**A11Y-002**, Medium); a static, non-descriptive page `<title>` never updated per route (**A11Y-003**, Low); no visible or programmatic indication of which fields are required (**A11Y-004**, Low); loading-state text changes that are not wrapped in a live region (**A11Y-005**, Low); and inconsistent heading hierarchy across the four screens (**A11Y-006**, Low). All six findings are `Open`.

**Totals: 0 Critical, 0 High, 2 Medium, 4 Low. Zero findings require an immediate CLAUDE.md §21 human-approval gate — none is Critical/High.**

**2026-07-07 re-verification update (accessibility-tester):** All six findings were routed through the Iterative Review-Fix Loop to lead-full-stack-engineer (A11Y-001, HO-023), senior-full-stack-engineer (A11Y-002, HO-024), and junior-developer (A11Y-003/004/005/006, HO-025). Each fix was independently re-verified against the current source (not against the developers' own claims) by re-reading every touched file listed below. All six are confirmed correct and are now marked **Resolved**. No fix introduced a new keyboard trap, no fix silently broke another finding's compliance, and the four routed screens now each expose exactly one `<h1>` inside `<main>`, compatible with `RouteFocusService`'s `main h1, main h2, main [role="heading"]` selector. **The review report now shows zero `Open` findings.**

---

## NFR-by-NFR Assessment

| NFR | Requirement | Result | Notes |
|---|---|---|---|
| NFR-A11Y-001 | Full keyboard operability, no keyboard trap | **Pass** (see A11Y-001 for a related-but-distinct focus-order gap, not a keyboard-operability failure) | All controls are native `<select>`/`<input>`/`<button>` elements; zero custom click-only widgets found |
| NFR-A11Y-002 | Every form field has a programmatically associated visible label | **Pass** | 0 fields rely on placeholder text alone; all 8 form fields (5 search-form + 3 per passenger-form-section) use `<label for>` |
| NFR-A11Y-003 | Dynamic error/empty-state messages announced via live region/`role="alert"` | **Pass** | All validation errors, API error banners, and empty-state messages use `role="alert"` or `role="status"`; see A11Y-005 for a related but distinct loading-state gap that is not, strictly, an "error/empty-state message" |
| NFR-A11Y-004 | Colour contrast ≥4.5:1 normal text / ≥3:1 large text | **Pass** | All computed text/background pairs meet or exceed threshold; see full computation table below |
| NFR-A11Y-005 | Booking reference present in accessible DOM, not styling/image-only | **Pass** | Plain text node inside `role="status"` paragraph, document order preserved |
| NFR-A11Y-006 | Logical focus order across screens; focus must not land on `<body>` | **Fail → Pass (fix verified 2026-07-07)** | `RouteFocusService` now moves focus to the new screen's heading on every transition — see A11Y-001, now `Resolved` |

---

## Colour Contrast Computation Table (supporting NFR-A11Y-004)

| Text/UI element | Foreground | Background | Font size/weight | Text class | Computed ratio | Threshold | Result |
|---|---|---|---|---|---|---|---|
| App header `<h1>` | `#ffffff` | `#1a5fb4` | 1.5rem (24px) | Large | ≈6.29:1 | 3:1 | Pass |
| Field/banner error text (`search-form`, `passenger-form-section`) | `#b00020` | `#ffffff` (page) | 0.85rem (13.6px) | Normal | ≈7.32:1 | 4.5:1 | Pass |
| `.banner-error` text-on-tint (search-form, results-list, booking-form) | `#b00020` | `#fdecea` | 0.85–1rem | Normal | ≈6.9:1 (computed against near-white tint, marginally below the plain-white figure) | 4.5:1 | Pass |
| `.banner-info` text (booking-form "already confirmed" notice) | `#665c00` | `#fff8e1` | 1rem | Normal | ≈6.38:1 | 4.5:1 | Pass |
| `.timing` secondary text (results-list) | `#444444` | `#ffffff` | 0.9rem (14.4px) | Normal | ≈9.73:1 | 4.5:1 | Pass |
| `.per-person-price` (results-list, booking-form) | `#555555` | `#ffffff`/`#f7f9fc` | 0.9–0.95rem | Normal | ≈7.46:1 | 4.5:1 | Pass |
| `.lead-badge` (passenger-form-section) | `#555555` | `#ffffff` | 0.85rem | Normal | ≈7.46:1 | 4.5:1 | Pass |
| `.sort-option.active` (sort-control) | `#ffffff` | `#1a5fb4` | 0.9rem (14.4px), weight 600 | Normal (14.4px bold does not meet the 18.66px-bold "large text" threshold) | ≈6.29:1 | 4.5:1 | Pass |
| Booking reference (confirmation) | `#1a5fb4` | `#eef4fc` | 2rem (32px), weight 800 | Large | ≈5.69:1 | 3:1 | Pass |

No text/background pair found anywhere in the four feature stylesheets or the shared header falls below its applicable threshold. **No contrast finding is raised.**

---

## Findings

### A11Y-001 — No focus management on any of the four route transitions; focus lands on `<body>` (NFR-A11Y-006)

| Field | Value |
|---|---|
| Severity | Medium |
| File/area | `frontend/src/app/app.config.ts` (lines 7–15, `provideRouter(routes)` with no focus-management extra); `frontend/src/app/app.routes.ts` (all 5 route entries); `frontend/src/app/features/results/results-list/results-list.component.ts` (`selectFlight`, lines 38–43); `frontend/src/app/features/booking/booking-form/booking-form.component.ts` (`onSubmit`, lines 151–193); `frontend/src/app/features/confirmation/confirmation/confirmation.component.ts` (`startNewSearch`, lines 34–39) |
| Status | **Resolved** (fix verified 2026-07-07, HO-023) |

**Verification (accessibility-tester, 2026-07-07):** Independently read `frontend/src/app/core/services/route-focus.service.ts`, `frontend/src/app/app.ts`, `frontend/src/app/app.html`, and `frontend/src/app/core/services/route-focus.service.spec.ts` directly (not the handoff's claims). Confirmed: (1) `RouteFocusService` subscribes to `Router.events`, filters `NavigationEnd`, skips the first (bootstrap) navigation, and on every subsequent navigation defers via `setTimeout` then queries `main h1, main h2, main [role="heading"]` (falling back to `<main>`'s first non-`router-outlet` child), sets `tabindex="-1"` if absent, and calls `.focus()`; (2) `App` (`app.ts`) injects `RouteFocusService` and calls `.start()` once in its constructor — genuinely wired up, not merely created; (3) `app.html`'s shell is exactly `<main><router-outlet /></main>`, matching the selector's assumption; (4) no new keyboard trap is introduced — the target gets `tabindex="-1"` (programmatically focusable, not tab-reachable on its own) and a single `.focus()` call, with no trapping/cycling logic; (5) the 3 spec tests in `route-focus.service.spec.ts` genuinely exercise the no-op-on-first-nav case, the `<h1>`-focus-and-tabindex case, and the not-left-on-`<body>` case, and are not tautological. Confirmed with the junior-developer's independent A11Y-006 fix that all 4 routed screens now have a real `<h1>` inside `<main>` in every state (including empty/fallback branches), so the primary selector path is hit on all 4 screens, not the generic fallback. Verdict: genuinely closes the gap — **Resolved**.

**Evidence:** `app.config.ts` registers only `provideBrowserGlobalErrorListeners()`, `provideRouter(routes)`, and `provideHttpClient()` — no `withInMemoryScrolling`, no Angular CDK `FocusMonitor`/`LiveAnnouncer`, no router `NavigationEnd` subscriber anywhere in `frontend/src`. A repository-wide search for `\.focus\(|cdkFocusInitial|autofocus|tabindex` across `frontend/src` returned zero matches. Each of the four screens (`SearchFormComponent`, `ResultsListComponent`, `BookingFormComponent`, `ConfirmationComponent`) is rendered behind a single `<router-outlet />` in `app.html` with no shared, persistent focus anchor across routes.

The four user-triggered transitions and their focus outcome:
- `/search` → `/results`: `SearchFormComponent.onSubmit()` calls `router.navigate(['/results'])` after a successful search; the focused element at that moment is the "Search" submit button, which is destroyed when `SearchFormComponent` is torn down.
- `/results` → `/booking`: `ResultsListComponent.selectFlight()` calls `router.navigate(['/booking'])`; the focused element is the "Select" button on the chosen result card, destroyed on teardown.
- `/booking` → `/confirmation`: `BookingFormComponent.onSubmit()` calls `router.navigate(['/confirmation'])`; the focused element is the "Confirm Booking" submit button, destroyed on teardown.
- `/confirmation` → `/search`: `ConfirmationComponent.startNewSearch()` calls `router.navigate(['/search'])`; the focused element is the "Start a New Search" button, destroyed on teardown.

In every case, per standard browser behaviour, removing the currently-focused element from the DOM without an explicit follow-up `.focus()` call moves focus to `<body>` — exactly the outcome NFR-A11Y-006's target explicitly names as non-compliant ("0 instances of ... focus landing on `<body>` after a screen transition").

**Impact:** Keyboard-only and screen-reader users receive no orientation cue when a new screen loads — after every one of the 4 primary-path transitions, the assistive-technology cursor/focus is reset to the document body, requiring the user to re-discover their position (typically by tabbing from the very start of the page, past the app-shell header, before reaching the new screen's content) rather than landing on the new screen's heading or first interactive control. This is a WCAG 2.1 SC 2.4.3 (Focus Order) gap that recurs on every primary user journey in the application, not an isolated edge case.

**Recommendation:** Add a single, centralized focus-management mechanism rather than four one-off fixes — e.g., a `NavigationEnd`-subscribing service (or a shared directive on each page's root `<section>`) that sets `tabindex="-1"` on the page's top-level heading/section and calls `.focus()` on it after each successful navigation, mirroring the common Angular Router accessibility pattern. This keeps the fix in one place rather than requiring each of the four components to manage its own focus call.

**Required fix:** Implement programmatic focus movement to a sensible landing point (e.g., the new screen's `<h1>`/top-level heading, given `tabindex="-1"`) on every one of the 4 route transitions, verified by confirming `document.activeElement` is not `<body>` immediately after each transition.

---

### A11Y-002 — Every "Select" button on the results screen shares an identical, non-differentiating accessible name (WCAG 4.1.2 / 2.4.6)

| Field | Value |
|---|---|
| Severity | Medium |
| File/area | `frontend/src/app/features/results/results-list/results-list.component.html` (line 30, `<button type="button" (click)="selectFlight(result)">Select</button>`) |
| Status | **Resolved** (fix verified 2026-07-07, HO-024) |

**Verification (accessibility-tester, 2026-07-07):** Independently read `results-list.component.html` and `.ts` directly. Confirmed the Select button now carries `[attr.aria-label]="selectButtonLabel(result)"` while its visible text remains exactly `Select` (unchanged, so sighted users see no regression). `selectButtonLabel()` composes provider, flight number, origin/destination city labels, departure time, and total price from the same helpers (`cityLabel`, `formatFlightTime`, `totalPriceText`) already driving the row's visible content — so the accessible name is genuinely unique per row (two rows with different flight numbers/providers cannot produce the same label) and does not contradict the visible "Select" text (it extends it with the row's identifying detail rather than replacing or conflicting with it — a screen reader announces "Select GlobalAir flight GA100, ... , button", which still starts with the verb "Select" the sighted button shows). The added test in `results-list.component.spec.ts` (`gives each Select button a unique, descriptive accessible name (A11Y-002)`) renders two cards with different flight numbers/providers and genuinely asserts non-equal `aria-label` values plus per-row content and unchanged visible text — not a tautological test. Verdict: genuinely closes the gap — **Resolved**.

**Evidence:** Each `<li class="result-card">` in the `@for` loop renders a `<button type="button" (click)="selectFlight(result)">Select</button>` with no `aria-label`, no `aria-labelledby` pointing at the card's route/provider/time details, and no visually-hidden differentiating text. When a search returns multiple results (the normal case — this is the entire purpose of US-002/US-003's sort feature), the accessible button tree presented to a screen reader is N buttons, all announced identically as "Select, button," with no distinguishing information carried in the accessible name itself.

**Impact:** WCAG 4.1.2 (Name, Role, Value) requires a control's name to convey its purpose; here the *value* the button acts on (which specific flight) is not conveyed by the name, only by sighted proximity to the surrounding card text. Screen reader users navigating by control type (e.g., NVDA/JAWS "b" or "Tab through form controls" browsing, or a button/landmarks list in VoiceOver Rotor) cannot distinguish which "Select" corresponds to which flight without first reading every preceding sibling node in sequence — materially increasing the effort needed to complete US-004 (flight selection) non-visually, compared to a sighted user who can visually scan the card.

**Recommendation:** Add an `aria-label` (or `aria-labelledby` referencing the card's route/provider/departure-time text already rendered) to each Select button, e.g. `aria-label="Select {{ result.provider }} flight {{ result.flightNumber }}, {{ cityLabel(result.origin) }} to {{ cityLabel(result.destination) }}, departing {{ formatFlightTime(result.departureDateTime) }}"`, so each button's accessible name is unique and self-describing without requiring visual context.

**Required fix:** Add a differentiating `aria-label`/`aria-labelledby` to the Select button in `results-list.component.html`, with a test (e.g. a component test asserting the rendered button's accessible name contains the flight number/provider) proving uniqueness across rendered cards.

---

### A11Y-003 — Static, non-descriptive document title never reflects the current screen (WCAG 2.4.2)

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `frontend/src/index.html` (line 5, `<title>Frontend</title>`); no Angular `Title` service usage found anywhere in `frontend/src/app` (grep-confirmed for `Title`/`document.title`) |
| Status | **Resolved** (fix verified 2026-07-07, HO-025) |

**Verification (accessibility-tester, 2026-07-07):** Independently read `frontend/src/index.html` and `frontend/src/app/app.routes.ts` directly. Confirmed `index.html`'s `<title>` is now "SkyRoute — Flight Search and Booking" (descriptive, not the scaffold default), and all 4 routes (`search`, `results`, `booking`, `confirmation`) declare a distinct `title` string ("Search Flights — SkyRoute", "Flight Results — SkyRoute", "Booking Details — SkyRoute", "Booking Confirmed — SkyRoute"). Confirmed `app.config.ts` registers `provideRouter(routes)` with no custom `TitleStrategy` override — Angular Router's `DefaultTitleStrategy` (provided automatically as part of the router providers, no extra configuration required) applies a route's static `title` string to `document.title` via the `Title` service on every successful navigation; this is standard, documented Angular Router behaviour, not a claim requiring further code to activate. Verdict: genuinely closes the gap — **Resolved**.

**Evidence:** `index.html`'s `<title>` is the unmodified Angular CLI scaffold default, "Frontend." No route (`app.routes.ts`) sets a `title` property, and no component or service injects Angular's `Title` service to set `document.title` programmatically. All four screens (`/search`, `/results`, `/booking`, `/confirmation`) therefore share the exact same browser/tab title throughout the entire user journey.

**Impact:** WCAG 2.1 SC 2.4.2 (Page Titled) expects a title that identifies the page's topic or purpose; screen-reader users frequently rely on the announced page title immediately after a navigation (especially in an SPA, where many screen readers announce the document title on route change as their primary orientation cue) to confirm they have arrived somewhere new. Here, that cue never changes and never describes the app itself ("Frontend" is a placeholder, not a product name), providing no orientation value across any of the 4 screens or on initial load.

**Recommendation:** Set a descriptive base title (e.g., "SkyRoute — Flight Search and Booking") in `index.html`, and additionally set a per-route `title` in `app.routes.ts` (Angular Router supports a static `title` string or a `TitleStrategy`/resolver per route) so each screen announces a distinct, descriptive title (e.g., "Search Flights — SkyRoute," "Flight Results — SkyRoute," "Booking Details — SkyRoute," "Booking Confirmed — SkyRoute").

**Required fix:** Add a descriptive default title in `index.html` and per-route `title` values in `app.routes.ts` (or an equivalent `Title` service call in each component's constructor), with a test asserting `document.title` changes appropriately after navigating to each route.

---

### A11Y-004 — Required form fields have no visible or programmatic required-field indicator (WCAG 3.3.2)

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `frontend/src/app/features/search/search-form/search-form.component.html` (Origin, Destination, Departure Date fields, lines 6–48); `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.html` (Full Name, Email, Document Number fields, lines 10–43) |
| Status | **Resolved** (fix verified 2026-07-07, HO-025) |

**Verification (accessibility-tester, 2026-07-07):** Independently read both templates directly. Confirmed all 5 search-form fields (Origin, Destination, Departure Date, Passengers, Cabin Class) and all 3 passenger-form-section fields (Full Name, Email Address, the dynamic document-number field) now carry both the native `required` attribute and `aria-required="true"`, plus a visible `<span class="required-indicator">(required)</span>` inside each field's `<label>`. Confirmed the `.required-indicator` CSS (`#555555` on white) reuses a ratio (≈7.46:1) the review's own contrast table already verified safe for `.per-person-price`/`.lead-badge`, so no new contrast finding is introduced. Confirmed the two pre-existing specs whose assertions were updated (`passenger-form-section.component.spec.ts` two `toBe`→`toContain` changes, testing the dynamic document label) are a legitimate consequence of the added visible text, not a weakening of what is actually verified — the label's dynamic-text behavior (`documentLabel()` input) is still asserted, just with `toContain` instead of an exact-string match now that the label has an extra child span. Verdict: genuinely closes the gap — **Resolved**.

**Evidence:** All five search-form fields are backed by `Validators.required` (`search-form.component.ts` lines 49–51) or, for passenger fields, by `fullNameValidator()`/`emailFormatValidator()`/`documentNumberValidator(...)` (all of which reject an empty value, per `booking-form.component.ts` lines 112–118). None of the corresponding template `<select>`/`<input>` elements carries the native `required` HTML attribute, an `aria-required="true"` attribute, or any visible cue (asterisk, "(required)" suffix, or instructional text) in its `<label>`. Every field in both forms is, in fact, required (the search form has no optional field; every passenger field is required for every passenger), so this is not a case where an indicator is needed to distinguish required from optional fields — but it still leaves screen-reader users with no programmatic confirmation of the field's mandatory status, and sighted users with no visual cue either, until after a failed submit produces the `role="alert"` error message.

**Impact:** WCAG 3.3.2 (Labels or Instructions) expects instructions to be provided where user input is required, so that the requirement is knowable before an error occurs, not only after. Today, a screen-reader user tabbing through the search form or a passenger section hears only the field's label ("Origin," "Full Name," etc.) with no indication that leaving it blank will produce an error — they discover this only reactively, after attempting to submit and having the `role="alert"` paragraph read out. This is a usability/orientation gap rather than a blocking barrier (the error message itself is correctly announced per NFR-A11Y-003), but it means the mandatory-field information is available only after the fact, not proactively.

**Recommendation:** Add the native `required` attribute to each `<select>`/`<input>` (which also gets `aria-required` semantics for free in most browsers' accessibility trees) and/or a visible "(required)" text or asterisk convention in the associated `<label>`, applied consistently across both forms since every field in both forms is mandatory.

**Required fix:** Add `required`/`aria-required="true"` (or an equivalent visible label convention) to all 5 search-form fields and all 3 passenger-form-section fields, with a check confirming the attribute is present in the rendered DOM for each field.

---

### A11Y-005 — Transient loading-state text ("Searching…"/"Confirming…") is not wrapped in a live region

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `frontend/src/app/features/search/search-form/search-form.component.html` (lines 79–85, the submit button's `@if (loading())` text swap); `frontend/src/app/features/booking/booking-form/booking-form.component.html` (lines 40–46, same pattern) |
| Status | **Resolved** (fix verified 2026-07-07, HO-025) |

**Verification (accessibility-tester, 2026-07-07):** Independently read both templates and their CSS directly. Confirmed each submit button is now followed by a `<p class="visually-hidden" role="status" aria-live="polite">` shown only while `loading()` is true, with wording ("Searching for flights…" / "Submitting your booking…") independent of the button's own visible text. Confirmed `.visually-hidden` uses a genuine clip-based off-screen technique (`position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0)`) — this keeps the element in the accessible tree/announced by AT (unlike `display: none`/`visibility: hidden`, which would remove it from the accessibility tree and defeat the fix) while hiding it visually, which is correct and matches the finding's own recommendation. Verdict: genuinely closes the gap — **Resolved**.

**Evidence:** Both submit buttons swap their own text content between the idle label ("Search"/"Confirm Booking") and a loading label ("Searching…"/"Confirming…") based on the `loading()` signal, entirely inside the `<button>` element's own child content — there is no separate `role="status"`/`aria-live` element announcing the state change, unlike every other dynamic message in the app (validation errors, API error banners, and the results screen's own `role="status"` "Searching…" paragraph in `results-list.component.html` line 5, which is correctly live-region-wrapped but — per the search/results data flow in `search-state.service.ts`, where `search()` is awaited to completion before `router.navigate(['/results'])` ever runs — is not reachable in the normal navigation flow, since `loading()` is already `false` again by the time `/results` renders).

**Impact:** This is distinct from NFR-A11Y-003's already-compliant error/empty-state messaging (which this finding does not dispute) — it concerns the *in-progress* state specifically. A button's own text-content change is not guaranteed to be announced by assistive technology merely because the button was the most recently focused/activated element; this varies by screen reader/browser combination and is not a reliable substitute for an explicit live region. Given `NFR-PERF-001`/`NFR-PERF-002` bound search/booking latency to a p95 of 2s/1s respectively, the practical exposure window is short, but a screen-reader user who does not receive confirmation that their submission is in progress may attempt to re-activate the (already-disabled, so harmless, but silent) button, or simply be left uncertain whether their click registered at all.

**Recommendation:** Add a `role="status"` (polite) element alongside each submit button — e.g., a visually-hidden or already-visible sibling paragraph — that mirrors the loading state ("Searching for flights…"/"Submitting your booking…"), independent of the button's own label text, following the exact pattern already used correctly for the empty-state and error messages elsewhere in the same templates.

**Required fix:** Add a `role="status"` live-region element reflecting `loading()` state on both the search form and booking form, distinct from the button's own text content.

---

### A11Y-006 — Inconsistent heading hierarchy across the four screens (WCAG 1.3.1 / 2.4.6)

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `frontend/src/app/app.html` (line 2, global `<h1>SkyRoute</h1>`); `frontend/src/app/features/search/search-form/search-form.component.html` (line 2, `<h1>Search Flights</h1>`); `frontend/src/app/features/results/results-list/results-list.component.html` (line 2, `<h1>Flight Results</h1>`); `frontend/src/app/features/booking/booking-form/booking-form.component.html` (line 4, `<h1>{{ f.origin }} → {{ f.destination }}</h1>`); `frontend/src/app/features/confirmation/confirmation/confirmation.component.html` (no `<h1>` present at all — first heading is the `<h2>` at line 6) |
| Status | **Resolved** (fix verified 2026-07-07, HO-025) |

**Verification (accessibility-tester, 2026-07-07):** Independently read all 4 routed templates directly and confirmed each now has exactly one `<h1>` inside `<main>`, rendered unconditionally (outside the success-branch `@if`, so the fallback/empty state also has it): `search-form.component.html` — `<h1>Search Flights</h1>` (unchanged, already compliant); `results-list.component.html` — `<h1>Flight Results</h1>` (unchanged, already compliant); `booking-form.component.html` — new `<h1>Booking Details</h1>` at the section root, with the former route-code heading demoted to `<h2>{{ f.origin }} → {{ f.destination }}</h2>` inside the nested `.flight-summary` section (confirmed via `booking-form.component.css`'s `.flight-summary h2` selector, matching the demoted level); `confirmation.component.html` — new `<h1>Booking Confirmed</h1>` at the section root, with the existing `<h2>`/`<h3>` sub-headings (flight summary, passengers) unchanged one level below. Confirmed the app-shell's own `<h1>SkyRoute</h1>` (`app.html`, inside `<header>`, outside `<main>`) is the only other `<h1>` on any page and sits outside `<main>`, so `RouteFocusService`'s `main h1` selector (A11Y-001) cannot ambiguously match it — each screen's `main h1` query now resolves to exactly one element. Confirmed the two updated spec assertions (`booking-form.component.spec.ts`'s `.flight-summary h1`→`.flight-summary h2` selector change) track the actual, deliberate markup change rather than papering over a regression. Verdict: genuinely closes the gap — **Resolved**.

**Evidence:** The app shell (`app.html`) renders a permanent `<h1>SkyRoute</h1>` in the header on every screen. Each of the `/search` and `/results` routed components then renders its *own* `<h1>` for the page's purpose ("Search Flights," "Flight Results"), producing two simultaneous `<h1>` elements in the accessible tree on those two screens. `/booking` compounds this further: its `<h1>` is used for the flight route summary (`{{ f.origin }} → {{ f.destination }}`, e.g. "LHR → JFK") rather than a page-purpose heading like "Book Your Flight," so the page has no heading identifying it as the booking screen at all — a screen-reader user jumping by heading level 1 lands on a route code, not a page title. `/confirmation` has no `<h1>` of its own whatsoever; its first heading is the `<h2>` flight-summary heading at line 6, one level below where the other three routed screens place their page heading.

**Impact:** WCAG 2.1 SC 2.4.6 (Headings and Labels) expects headings to describe topic/purpose, and SC 1.3.1 (Info and Relationships) expects the heading structure to convey the document's organization consistently. A screen-reader user who learns, from `/search` and `/results`, that "jump to the first `<h1>` after the app-shell heading" reaches the page's purpose will find that pattern broken on `/booking` (lands on a route code, not "Booking") and broken differently on `/confirmation` (no page-level heading exists, landing directly on a sub-heading). This is a structural-consistency gap, not a blocking barrier — all content remains reachable — but it undermines the predictability heading-based navigation is meant to provide across a single, short user journey.

**Recommendation:** Give each of the four routed screens a single, page-purpose-appropriate top-level heading (e.g., "Book Your Flight" on `/booking`, "Booking Confirmed" on `/confirmation`), demoting the flight-route text to an `<h2>` (as `/booking`'s flight summary conceptually already is a sub-section) and adding an `<h2>`-level (not `<h1>`) heading to `/confirmation` if one is desired for the flight-summary block, so that heading level 1 consistently marks "the current screen's purpose" across all four routes, and the app-shell's own `<h1>` remains the only other `<h1>` on the page (a common, acceptable landmark/branding pattern, not itself a violation).

**Required fix:** Adjust heading levels in `booking-form.component.html` and `confirmation.component.html` so each routed screen has exactly one clear, page-purpose `<h1>` (or, if the app-shell `<h1>` is to remain the page's only `<h1>`, demote all four routed-screen headings to `<h2>` consistently) — either approach is acceptable as long as it is applied uniformly across all four screens.

---

## Findings Summary Table

| ID | Severity | Area | Status |
|---|---|---|---|
| A11Y-001 | Medium | Router/all 4 screens — no focus management on navigation (NFR-A11Y-006) | **Resolved** (2026-07-07) |
| A11Y-002 | Medium | `results-list.component.html` — non-unique "Select" button accessible names | **Resolved** (2026-07-07) |
| A11Y-003 | Low | `index.html`/routing — static, non-descriptive page title | **Resolved** (2026-07-07) |
| A11Y-004 | Low | `search-form`/`passenger-form-section` — no required-field indicator | **Resolved** (2026-07-07) |
| A11Y-005 | Low | `search-form`/`booking-form` — loading-state text not in a live region | **Resolved** (2026-07-07) |
| A11Y-006 | Low | All 4 screens — inconsistent heading hierarchy | **Resolved** (2026-07-07) |

**Totals: 0 Critical, 0 High, 2 Medium, 4 Low. All 6 findings are `Resolved` as of 2026-07-07. Zero `Open` findings remain.**

---

## Fix Verification Evidence (2026-07-07)

Each finding above was re-verified by the accessibility-tester by independently reading the current state of every touched file (not by trusting the developer handoffs' claims). See the "Verification" note embedded in each finding above for the specific evidence and reasoning.

**Build/test evidence note:** This accessibility-tester session did not have a shell/Bash-equivalent tool available, so `npm run build` and `npm test -- --watch=false` in `frontend/` could not be executed directly by this agent in this pass. The most recent developer-reported execution (HO-025, junior-developer, 2026-07-07) reports, after fixing the two specs its own markup changes broke: `Test Files 17 passed (17)`, `Tests 149 passed (149)`, and `npm run build` → "Application bundle generation complete. [2.214 seconds]", no errors. This is consistent with HO-023's and HO-024's own incrementally-reported counts (149 after HO-023's 3 new tests; 146 after HO-024's 1 new test, prior to HO-025's spec-assertion updates) and with the specific test files independently read above (`route-focus.service.spec.ts`'s 3 tests, `results-list.component.spec.ts`'s 1 new A11Y-002 test, and the 3 updated pre-existing assertions in `passenger-form-section.component.spec.ts`/`booking-form.component.spec.ts`), all of which were read in full and found to be genuine, non-tautological assertions of the fixes' correctness rather than assertions weakened to force a pass. **Recommendation:** the orchestrator or functional-tester should independently execute `npm run build` and `npm test -- --watch=false` in `frontend/` before this phase is treated as fully closed for the Definition of Done, since this review's own static-code verification — while thorough — is not a substitute for one independently-observed command run confirming the reported 149/149 count.

**Independent confirmation (sdlc-orchestrator, 2026-07-07):** The sdlc-orchestrator independently executed both commands directly against the current working tree in `frontend/` — `npm run build` → "Application bundle generation complete. [4.291 seconds]", 0 errors; `npm test -- --watch=false` → "Test Files 17 passed (17)", "Tests 149 passed (149)", 0 failed — matching the developers' self-reported counts exactly with zero discrepancy, closing this section's caveat and satisfying the recommendation above. See `docs/handoffs/26-accessibility-tester-to-sdlc-orchestrator-a11y-verification-closure.md` (HO-026).

---

## Areas Explicitly Reviewed — No Finding Raised

- **Keyboard operability (NFR-A11Y-001):** Every interactive control across all four screens (`<select>`, `<input type="date"/"text"/"email">`, `<button>`) is a native HTML element. Zero `<div>`/`<span>` elements with only a `(click)` handler and no keyboard equivalent were found in `frontend/src/app/features`. No modal, overlay, or custom widget exists anywhere in the codebase, so no keyboard-trap risk exists structurally. The sort control (`sort-control.component.html`) correctly uses native `<button>` elements with `aria-pressed` for its toggle state, and `role="group"`/`aria-label="Sort flight results"` on its container — a correct WAI-ARIA pattern for a button-group toggle.
- **Label association (NFR-A11Y-002):** All 5 search-form fields (`origin`, `destination`, `departureDate`, `passengerCount`, `cabinClass`) and all 3 per-passenger fields (`fullName`, `email`, `documentNumber`, the latter with a dynamic `documentLabel()` text) use `<label [for]="...">` bound to a matching `id`/`[id]`. The passenger section additionally uses a semantic `<fieldset>`/`<legend>` ("Passenger N") to group each passenger's three fields — a correct native pattern for repeated field groups, and one worth calling out as a positive practice.
- **Live-region/error announcement (NFR-A11Y-003):** Every validation error (`search-form`, `passenger-form-section`), API error banner (`search-form`, `results-list`, `booking-form`), and empty-state message (`results-list`) is wrapped in `role="alert"` or `role="status"`. The "already confirmed" re-submission-guard notice (`booking-form.component.html` line 35) correctly uses `role="status"` (informational, not an error). See A11Y-005 for the one related-but-distinct gap found (button-internal loading text).
- **Colour contrast (NFR-A11Y-004):** See the full computation table above — every text/background pair found across the four feature stylesheets and the shared app header meets or exceeds its applicable WCAG 2.1 AA threshold.
- **Booking reference accessibility (NFR-A11Y-005):** `confirmation.component.html` line 3 renders `{{ b.bookingReference }}` as a plain interpolated text node inside a `role="status"` paragraph — present in document order, not conveyed by an image, not hidden via `aria-hidden`, and not styled in a way that removes it from the accessible tree (the visual "prominence" is achieved purely via font-size/weight/colour/border on the same text node, none of which affects its presence in the accessibility tree).
- **Focus-visible/outline removal:** A repository-wide search for `outline`/`:focus` in `frontend/src` found zero matches — no CSS anywhere removes or overrides the browser's default focus indicator, so native focus-visibility (WCAG 2.4.7, not one of the 6 scoped NFRs but part of the WCAG 2.1 AA baseline) is preserved by default across all controls.
- **XSS-adjacent rendering safety (cross-referenced from `docs/reviews/security-review-phase-16.md`):** Zero `innerHTML`/`bypassSecurityTrust*` usage exists in the frontend (already confirmed independently by the Phase 16 security review); all dynamic text in the four templates reviewed here is rendered via standard Angular interpolation, which is relevant to accessibility only in that it guarantees no assistive-technology-breaking markup injection is possible via user-controlled text (e.g., a passenger's full name rendered on the confirmation screen).

---

## Overall Recommendation

No Critical or High finding was identified. The two Medium findings (A11Y-001 focus management, A11Y-002 non-unique button names) were genuine, moderate-impact gaps affecting every primary user journey and every multi-result search respectively. Both, along with the four Low findings (A11Y-003 page title, A11Y-004 required-field indication, A11Y-005 loading-state live region, A11Y-006 heading hierarchy), were routed through the Iterative Review-Fix Loop per CLAUDE.md §22/`.claude/rules/phased-execution.md` to lead-full-stack-engineer (A11Y-001), senior-full-stack-engineer (A11Y-002), and junior-developer (A11Y-003–006), and each fix has now been independently re-verified against the current source by this reviewer (2026-07-07) — see the Fix Verification Evidence section above and the per-finding verification notes.

**Final status: all 6 findings are `Resolved`. Zero `Open` findings remain in this report.** Per `.claude/rules/phased-execution.md`'s Phase Completion Criteria, this phase's review-report gate for merge to `main` is satisfied, subject to an independent `npm run build`/`npm test` execution (see the Build/test evidence note above — this reviewer's session lacked a shell tool to run those commands directly) being confirmed by the orchestrator or functional-tester before the phase is finally closed out.

This review did not itself modify any application code — all fixes were made by the routed developer agents (HO-023, HO-024, HO-025); this reviewer only updated finding statuses and added verification evidence to this report.

---

*End of Accessibility Review Report — Phase 17.*
