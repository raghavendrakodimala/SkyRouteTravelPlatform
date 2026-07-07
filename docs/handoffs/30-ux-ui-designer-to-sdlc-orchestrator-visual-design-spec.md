# Handoff: Visual Design Spec for UI Polish (Requirements Compliance Fix)

| Field | Value |
|---|---|
| Handoff ID | HO-030 |
| Date | 2026-07-07 |
| Branch | `fix/requirements-compliance-gaps-skyroute-mvp` |
| Phase | Requirements Compliance Fix (post-Phase-17) |
| From agent | ux-ui-designer |
| To agent | sdlc-orchestrator |
| Status | Complete — spec ready for implementation. No application code touched. |

---

## Work Completed

The human Product Owner live-tested the app in Chrome and rated the UI "poorly designed," narrowing the complaint to visual polish specifically (not booking-form UX, not search/results layout logic) — layout, spacing, colors, typography.

Before writing anything, I read every CSS file under `frontend/src/**/*.css` and every template under `frontend/src/app/features/**/*.html` (plus `app.css`/`app.html`, `styles.css`, `index.html`) directly, rather than trusting the orchestrator's summary at face value. All five observations in the brief were confirmed against the real source:

1. **Font mismatch** — confirmed via `grep -r "font-family" frontend/src`, which returns zero matches anywhere. No stylesheet sets `font-family`, so headings fall back to the browser's default serif and everything else to default sans-serif.
2. **Unstyled native form controls** — confirmed; `select, input` rules in `search-form.component.css`/`passenger-form-section.component.css` set only `padding`/`font-size`, nothing else. Zero `outline`/`:focus` rules exist anywhere (grep-confirmed), matching the Phase 17 accessibility review's own finding.
3. **No consistent card/container system** — confirmed, with a nuance: `.result-card` and `.price-breakdown` already have *some* border/radius treatment (inconsistent with each other — 4px vs. 6px radius, no shadows); `.flight-summary` (booking-form and confirmation) has **zero** container styling at all, a bare text block. This is the single most visible gap.
4. **Header as the one deliberate color spot** — confirmed (`app.css`, `.app-header { background: #1a5fb4; color: #fff; }`).
5. **Default browser buttons** — confirmed across all button rules in all five component stylesheets; the one partial exception already in the codebase is `sort-control.component.css`, which already has a real border/radius/active-fill pattern — used in the spec as a pattern to extend, not replace.

I also independently found and factored in a CSP constraint not mentioned in the brief: `index.html`'s `Content-Security-Policy` sets `font-src 'self'` with no remote font host allowed — this independently confirms (doesn't just assume) that the type system must use a local/system font stack only, consistent with the "no new dependencies" instruction.

I then cross-checked every color decision against `docs/reviews/accessibility-review-phase-17.md`'s contrast table (§3.1 of the spec) and computed contrast ratios by hand (same sRGB relative-luminance method the accessibility review used) for the handful of genuinely new pairs the spec introduces (§3.2), before finalizing any hex value.

## Artifacts Created

- `docs/design/visual-design-spec.md` — the full visual design spec (`docs/design/` directory created). Covers:
  1. A complete `:root` CSS custom-property token block (colors, one system-font stack, type scale, 4px-rhythm spacing scale, radius scale, shadow tokens, transition token) — ready to paste into `frontend/src/styles.css`.
  2. Type system: single font stack for headings + body (removes the accidental serif/sans mix), a 7-step size scale, and a table mapping every existing text selector to its new token value.
  3. Color palette anchored on `#1a5fb4`, with an explicit table (§3.1) showing every existing Phase 17 contrast-table row is reused unchanged, and a second table (§3.2) computing contrast for the three genuinely new pairs this spec introduces.
  4. Spacing scale (4/8/12/16/24/32/48px) and a card/container recipe applied identically to `.result-card`, `.price-breakdown`, `.flight-summary` (booking-form + confirmation), and `.passenger-section`, with a selector-by-selector delta from current state.
  5. Form-control styling (border/radius/padding/focus glow/invalid-state via Angular's auto-applied `.ng-invalid.ng-touched` classes) and a two-tier button system (primary filled / secondary outline, with a stated rationale for which screens get which), plus an extension of the sort-control's already-decent existing pattern.
  6. A file-by-file implementation map (§6) naming every touched selector per file.
  7. Explicit constraints (§7) and open questions (§8) for the implementer/orchestrator.
- `docs/handoffs/30-ux-ui-designer-to-sdlc-orchestrator-visual-design-spec.md` (this file).

No files were modified outside `docs/design/` and this handoff. No application/CSS source code was touched, per the task instruction that this is a design-spec task, not an implementation task.

## Decisions Made

- **One font family for the whole app**, not a heading/body pairing — simplest option that fully removes the accidental mismatch and stays inside the CSP's `font-src 'self'` constraint with zero new assets.
- **Two-tier button system**: primary (filled) for "Search," "Confirm Booking," "Start a New Search" (each the sole next-step CTA on its screen); secondary (outline) for "Select" only, because it repeats once per result card and a wall of solid-blue buttons down the results list would visually overwhelm the page and undercut the "one clear primary action per screen" hierarchy.
- **Card treatment reuses each container's own existing selector name** (`.result-card`, `.price-breakdown`, `.flight-summary`, `.passenger-section`) rather than introducing a shared `.card` class, specifically to keep the change CSS-only with zero template edits (adding a shared class would require touching five `.html` files).
- **New form-control border color (`#767676`)** was deliberately chosen to clear the WCAG 1.4.11 non-text-contrast guideline (~4.5:1 against white) even though that SC sits outside the six the Phase 17 review scoped — flagged as a considered choice, not a hard requirement, in case the team prefers a lighter, more "flat" border instead (§8, open question 3).
- **Did not touch `html, body`'s existing `background-color: #ffffff`/`color-scheme: light` rule** in `styles.css`, since the task brief stated a separate, concurrent fix on this same branch owns the dark-mode-inversion bug in that exact block — avoided touching it to prevent a merge collision, and noted this explicitly as constraint §7 and open question §8.1.
- **`.total-price` font-size harmonized to one value (24px/`--font-size-2xl`)** across results-list/booking-form/confirmation, which today independently use three different sizes (20.8px/24px/22.4px) for the same semantic element — a deliberate, flagged fix, not an oversight.
- **`.passengers` (confirmation screen) intentionally excluded** from the card treatment — the task's target list named `.result-card`/`.price-breakdown`/`.flight-summary`/"the passenger fieldset"/"form field groups" but not this selector; left as plain text and flagged as open question §8.2 rather than assumed in-scope.

## Open Questions

See spec §8 in full. Summarized:

1. Should a subtle page-backdrop color (distinct from white cards) be added once the concurrent dark-mode fix's `html, body` background rule has landed? Deferred to avoid a merge collision on that line.
2. Should `.passengers` on the confirmation screen get the same card treatment as `.passenger-section`, even though it wasn't named in the task's target list?
3. Is the new, deliberately-more-visible form-control border color (`#767676`, chosen to clear WCAG 1.4.11 ~4.5:1) preferred over a lighter, more "flat modern" but sub-3:1 alternative matching the app's two existing (unreviewed-for-1.4.11) card/toggle borders?

None of these block implementation — the spec states a default answer for each and flags the alternative.

## Risks and Impediments

- None blocking. The spec was deliberately scoped to avoid any file/line overlap with the concurrent dark-mode-inversion fix mentioned in the task brief (same branch); if that fix has since changed `styles.css`'s structure by the time this spec is implemented, the implementer should re-read the current file before applying §1/§2's `html, body` additions, since this spec's snippets assume the file's current shape as read during this session.
- The three newly-computed contrast pairs (§3.2) were hand-computed using the same formula the Phase 17 review documented; recommend the implementing developer agent (or a follow-up accessibility-tester pass) spot-check at least the new form-control border ratio once real CSS lands, as a sanity check rather than a re-derivation.

## Required Next Agent Action

Per CLAUDE.md's agent-routing table, UI/UX flow work routes to a developer agent for implementation. Route this spec to **lead-full-stack-engineer** or **senior-full-stack-engineer** to implement `docs/design/visual-design-spec.md` across `frontend/src/app/**/*.css` and `frontend/src/styles.css`, per the file-by-file map in the spec's §6. No `.html` template changes are required (the spec is achievable through existing class names and Angular's auto-applied `.ng-invalid`/`.ng-touched` classes). The developer should run `npm run build` and the frontend test suite after implementing, since several selectors (`.total-price`, `.field` margin, `.passenger-section` radius) have visual-regression-style Vitest assertions worth re-checking (e.g., `booking-form.component.spec.ts` if any spec asserts computed styles — check before assuming none do).

## Completion Criteria for Next Step

- All CSS changes in §6's file map applied, using only the token values defined in the spec (no ad hoc new hex/px values introduced beyond the three already computed and justified in §3.2).
- `npm run build` clean, existing Vitest suite still green (no application behavior changed, so no test should need updating — if one does, investigate why before changing the test).
- No `<a>`/component/template files touched.
- No modification to `html, body`'s existing `background-color`/`color-scheme` lines in `styles.css` (coordinate first if that block has moved).
- A visual sanity check (screenshot or live browser) confirming: single sans-serif font app-wide, styled form-control borders/focus states, card treatment visible on `.flight-summary`/`.price-breakdown`/`.passenger-section`/`.result-card`, two distinguishable button styles (filled vs. outline).
- Documentation/delivery tracking updated per standard Definition of Done once implementation lands.

## Relevant Files

- `docs/design/visual-design-spec.md` (new — the spec itself)
- `docs/reviews/accessibility-review-phase-17.md` (contrast baseline cross-checked in spec §3.1)
- `frontend/src/styles.css`, `frontend/src/app/app.css`, `frontend/src/index.html` (read for verification; token block and global rules land here)
- `frontend/src/app/features/search/search-form/search-form.component.css`/`.html`
- `frontend/src/app/features/results/results-list/results-list.component.css`/`.html`
- `frontend/src/app/features/results/sort-control/sort-control.component.css`/`.html`
- `frontend/src/app/features/booking/booking-form/booking-form.component.css`/`.html`
- `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.css`/`.html`
- `frontend/src/app/features/confirmation/confirmation/confirmation.component.css`/`.html`

*End of Handoff HO-030.*
