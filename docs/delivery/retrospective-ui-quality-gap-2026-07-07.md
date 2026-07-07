# Retrospective — UI Quality Gap (Process Improvement)

- Date: 2026-07-07
- Facilitator: sdlc-orchestrator (per human Product Owner directive)
- Scope: process retrospective on why visual/UX quality issues surfaced only at the end of the SDLC
- Type: focused process retrospective (out-of-cadence, PO-triggered)

---

## 1. What Happened

The human Product Owner reviewed the rendered application after implementation (Phase 12), test writing/execution (Phases 13–14), and four completed review phases (Code, Security, Accessibility — Phases 15–17), and found visual/UX quality problems no gate had caught:

- unpolished overall layout,
- misplaced navigation,
- poor spacing rhythm,
- no production-shape footer.

Every automated and review gate was green at that point: 270+ tests passing, zero `Open` code/security/accessibility findings. The visual quality gap was invisible to all of them.

## 2. Root Cause

The SDLC operating model gated code correctness, security, and accessibility early — but had **no early gate for visual design quality or production-layout UX**:

- No visual/interaction design spec was required before implementation (API contracts were mandatory; visual design was not).
- Reviews were code-reading exercises. Nobody was required to look at the rendered app in a browser until the PO did.
- Definition of Ready and Definition of Done had no rendered-UI criteria.
- There was no per-story demo checkpoint — visual feedback arrived per-project, at the end, instead of per-story within the sprint, as real agile delivery would require.

## 3. What We Changed

| # | File | Change |
|---|---|---|
| 1 | `.claude/rules/ui-ux-quality-gates.md` (new) | Defines the five gates: Design Before Build (approved `docs/design/` spec before UI implementation, same standing as API contracts), Shift-Left Visual QA (engineer verifies rendered UI in a browser at desktop + mobile widths before handoff, evidence in handoff note), Rendered-UI Review Evidence (a review that never rendered the app cannot resolve UI findings), PO Visual Demo Gate (per-story demo checkpoint before UI review phases close), and a Production Layout Checklist (branded nav, one dominant CTA, spacing rhythm, real footer, 360/768/1280px responsiveness, styled states, focus-visible). Includes the sprint-cadence rule: design review happens before implementation starts. |
| 2 | `.claude/rules/definition-of-ready.md` | UI work is not Ready without an approved visual/interaction design spec. |
| 3 | `.claude/rules/definition-of-done.md` | UI work is not Done without rendered-UI verification evidence and a completed PO visual demo checkpoint. |
| 4 | `.claude/rules/phased-execution.md` | Phase Completion Criteria: UI-delivering phases require rendered-UI verification evidence. Iterative Review-Fix Loop: reviewers of UI phases must include rendered-app evidence when filing and verifying findings. |
| 5 | `.claude/agents/ux-ui-designer.md` | Designer now explicitly owns the visual design spec as a pre-implementation deliverable, performs post-implementation visual QA against the rendered app, and applies the Production Layout Checklist. |
| 6 | `CLAUDE.md` §8 | Spec-Driven Development verification list now requires an approved visual design spec for UI work. |

## 4. How Future Sprints Prevent Recurrence

- **Shift-left**: visual design is now part of Definition of Ready — a UI story cannot enter implementation without an approved design spec, exactly like an API contract.
- **Rendered evidence, not code reading**: engineers and reviewers must both look at the running app (desktop + mobile) and record what they saw; unverified UI work bounces back at the handoff gate.
- **Per-story demos**: the PO sees each UI story rendered before its review phases close (real sprint review cadence), so visual feedback arrives within the sprint that built the story — not at project end.
- **Checklist-driven polish**: the Production Layout Checklist gives designer and reviewers one shared, objective definition of "production-shape" so nav/footer/spacing gaps are findings, not opinions.

## 5. Follow-Ups

- None blocking. The existing `docs/design/visual-design-spec.md` and `docs/design/booking-passenger-flow-spec.md` already conform to the new `docs/design/` location and serve as the baseline specs for current UI work.
