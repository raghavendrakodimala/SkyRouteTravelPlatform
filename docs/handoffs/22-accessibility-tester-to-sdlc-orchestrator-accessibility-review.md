# Handoff: Accessibility Review Findings Filed (Phase 17)

| Field | Value |
|---|---|
| Handoff ID | HO-022 |
| Date | 2026-07-07 |
| Branch | `sdlc/17-accessibility-review-skyroute-mvp` |
| Phase | Phase 17 — Accessibility Review |
| From agent | accessibility-tester |
| To agent | sdlc-orchestrator |
| Status | Findings filed — 6 `Open` findings (0 Critical, 0 High, 2 Medium, 4 Low). Review is not yet closed: per CLAUDE.md §22/`.claude/rules/phased-execution.md`, this phase is not merged to `main` until `docs/reviews/accessibility-review-phase-17.md` shows zero `Open` findings via the Iterative Review-Fix Loop. |

---

## Work Completed

Performed a static/manual-inspection accessibility review of the full Angular 22 standalone-component frontend (`frontend/src/app/features/search`, `.../results`, `.../booking`, `.../confirmation`, plus `app.html`/`app.ts`/`app.css`, `app.config.ts`, `app.routes.ts`, `core/guards/booking-flow.guards.ts`, `index.html`, `styles.css`) against the six accessibility NFRs in `docs/specs/non-functional-requirements.md` Section 8 (NFR-A11Y-001–006), with WCAG 2.1 Level AA as the governing baseline per `docs/requirements.md` Section 5.5. No live browser, screen reader, or automated axe-core/Lighthouse tool was available in this environment; all keyboard-operability, label-association, live-region, contrast, booking-reference, and focus-order checks were performed by reading component templates/TypeScript/CSS directly, and colour contrast was independently computed from the actual hex colour pairs found in the CSS using the WCAG relative-luminance/contrast-ratio formula.

**Result: 4 of the 6 core NFRs pass with no finding** (NFR-A11Y-001 keyboard operability, NFR-A11Y-002 label association, NFR-A11Y-003 live-region error announcement, NFR-A11Y-004 colour contrast, NFR-A11Y-005 booking-reference accessibility — 5 of 6, correcting the count: NFR-A11Y-001/002/003/004/005 all pass). **NFR-A11Y-006 (Should Have — focus order) fails**: no focus-management mechanism exists anywhere in the routing layer (`app.config.ts`'s `provideRouter(routes)` has no focus/scroll extra; zero `.focus(`/`cdkFocusInitial`/`autofocus`/`tabindex` usages found repository-wide), so focus lands on `<body>` after all four route transitions (`/search`→`/results`, `/results`→`/booking`, `/booking`→`/confirmation`, `/confirmation`→`/search`). This is filed as **A11Y-001 (Medium)**.

Five further findings were raised beyond the core 6 NFRs, all genuine WCAG 2.1 AA-baseline gaps found during the walkthrough:
- **A11Y-002 (Medium)** — every "Select" button on the results screen shares an identical, non-differentiating accessible name (`results-list.component.html` line 30), a WCAG 4.1.2/2.4.6 gap that recurs on every multi-result search.
- **A11Y-003 (Low)** — static, non-descriptive `<title>Frontend</title>` in `index.html`, never updated per route; no Angular `Title` service usage found anywhere (WCAG 2.4.2).
- **A11Y-004 (Low)** — no visible/programmatic required-field indicator on any of the 8 required form fields across the search form and passenger-form-section, despite all being enforced via Angular validators (WCAG 3.3.2).
- **A11Y-005 (Low)** — the "Searching…"/"Confirming…" loading-state text swap is inside the submit button's own text content only, not a separate live region, unlike every other dynamic message in the app.
- **A11Y-006 (Low)** — inconsistent heading hierarchy across the four screens: `/search` and `/results` each duplicate an `<h1>` alongside the permanent app-shell `<h1>SkyRoute</h1>`; `/booking`'s `<h1>` is a flight-route code rather than a page-purpose heading; `/confirmation` has no page-level heading at all.

No Critical or High finding was identified anywhere in the frontend. Colour contrast was computed for every distinct text/background pair found across the four feature stylesheets and the shared app header; all passed at or above the applicable 4.5:1/3:1 threshold (full computation table in the review report). The booking reference (US-006 AC5) is a plain interpolated text node inside a `role="status"` paragraph — confirmed present in document order, not image-conveyed, not `aria-hidden`.

Per the accessibility-tester role's rules, **no application code was modified** — this is a findings-only filing. Routing findings to developer agents and re-invoking this reviewer for verification is the orchestrator's responsibility per the Iterative Review-Fix Loop.

## Artifacts Created or Updated

- `docs/reviews/accessibility-review-phase-17.md` (new) — full review report: header metadata table, summary, NFR-by-NFR assessment table, colour-contrast computation table, 6 findings (A11Y-001–006, all `Open`) each with Severity/File-area/Evidence/Impact/Recommendation/Required fix/Status, findings summary table, "Areas Explicitly Reviewed — No Finding Raised" section, overall recommendation.
- `docs/handoffs/22-accessibility-tester-to-sdlc-orchestrator-accessibility-review.md` (this file, new).
- `docs/handoffs/current-handoff.md` (updated to point to this handoff as current).
- `docs/handoffs/handoff-index.md` (row added for HO-022).

No source code, test, or configuration file was created, updated, or deleted.

## Decisions Made

- All 6 findings are filed as `Open` (none as `Accepted Risk`) — every finding's own text states a concrete, low-to-moderate-effort fix is expected, so none qualifies for the narrow "reviewer may mark Accepted Risk directly" carve-out in CLAUDE.md §22 (which requires the finding's *own original text* to already say no fix is required, mirroring CR-005's precedent — none of these six do).
- NFR-A11Y-006's focus-management gap was filed as a single finding (A11Y-001) covering all four route transitions and recommending one centralized fix (e.g., a `NavigationEnd`-subscribing service or shared directive), rather than four separate per-transition findings, since the root cause (no focus-management mechanism at all in `app.config.ts`) is singular.
- Five additional findings beyond the six core NFRs were filed because they are genuine WCAG 2.1 AA-baseline gaps under the "WCAG 2.1 Level AA baseline" framing in `docs/requirements.md` Section 5.5, even though they don't map 1:1 to an NFR-A11Y-00X ID — consistent with the review task's instruction to "file every finding... and explicitly note anything that already passes," not only findings against the six listed NFRs.
- Severity was assigned by user-impact breadth/frequency (recurs on every primary journey or every multi-result search → Medium; narrower or lower-friction gaps → Low), not by the NFR's own MoSCoW priority (NFR-A11Y-006 is "Should Have" in the NFR spec but its violation was still rated Medium on user-impact grounds).

## Open Questions

None blocking. One judgment call worth flagging to the human/orchestrator: A11Y-002 (non-unique "Select" button names) and A11Y-006 (heading hierarchy) both touch `results-list.component.html`/`booking-form.component.html` templates that a developer agent may want to fix together in one pass — no objection from this review either way, since neither finding depends on the other's fix.

## Risks and Impediments

- No live browser/screen reader/automated tool was available for this review (stated explicitly in the review report's Method line); all findings rest on static code inspection and manually-computed contrast ratios, not on-device verification. This is a documented methodology limitation, not a blocker — the computed contrast ratios have wide margins (tightest pass is ≈5.69:1 against a 3:1 threshold), so the risk of a false pass due to computation error is low, but a live screen-reader/axe-core pass remains a reasonable follow-up if time allows before final MVP sign-off.
- None of the 6 findings is Critical/High, so no CLAUDE.md §21 human-approval gate is triggered by this filing.

## Required Next Agent Action

1. Route each of the 6 `Open` findings (A11Y-001–006) to a developer agent per the routing table in `.claude/rules/delegation-rules.md` ("Review Finding → Developer Agent Routing") — both Medium findings (A11Y-001 focus management, A11Y-002 button naming) are template/service-level changes of moderate scope (candidates for senior-full-stack-engineer or lead-full-stack-engineer depending on how centralized the focus-management fix is designed); the four Low findings (A11Y-003 page title, A11Y-004 required-field indicators, A11Y-005 loading live region, A11Y-006 heading levels) are smaller, template-localized changes (candidates for junior-developer, following the existing `role="alert"`/`role="status"` pattern already proven elsewhere in the same templates).
2. After each fix, re-invoke accessibility-tester scoped to the changed files/finding IDs to verify and flip status to `Resolved`/`Partially Resolved`/a new incremented finding ID, per the Iterative Review-Fix Loop.
3. Repeat until `docs/reviews/accessibility-review-phase-17.md` shows zero `Open` findings, then this phase branch can be committed/merged to `main`.

## Completion Criteria for Next Step

- Every finding ID A11Y-001 through A11Y-006 has a developer-agent handoff recording the fix (source + tests where applicable).
- accessibility-tester has independently re-verified each fix against the current code (not solely the developer's handoff claims) and updated the finding's Status in `docs/reviews/accessibility-review-phase-17.md`.
- `docs/reviews/accessibility-review-phase-17.md`'s Findings Summary Table shows zero `Open`/`In Progress`/`Partially Resolved` rows.
- `docs/delivery/delegation-log.md` has a row for each routed finding.

## Relevant Files

- `docs/reviews/accessibility-review-phase-17.md`
- `docs/specs/non-functional-requirements.md` (Section 8, NFR-A11Y-001–006)
- `docs/requirements.md` (Section 5.5)
- `frontend/src/app/features/search/search-form/search-form.component.html`, `.ts`, `.css`
- `frontend/src/app/features/results/results-list/results-list.component.html`, `.ts`, `.css`
- `frontend/src/app/features/results/sort-control/sort-control.component.html`, `.ts`, `.css`
- `frontend/src/app/features/booking/booking-form/booking-form.component.html`, `.ts`, `.css`
- `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.html`, `.ts`, `.css`
- `frontend/src/app/features/confirmation/confirmation/confirmation.component.html`, `.ts`, `.css`
- `frontend/src/app/app.html`, `frontend/src/app/app.ts`, `frontend/src/app/app.css`
- `frontend/src/app/app.config.ts`, `frontend/src/app/app.routes.ts`
- `frontend/src/app/core/guards/booking-flow.guards.ts`
- `frontend/src/index.html`, `frontend/src/styles.css`
