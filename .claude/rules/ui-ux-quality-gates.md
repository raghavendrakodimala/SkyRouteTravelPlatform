# UI/UX Quality Gates

Owner concept: this file owns the UI/UX quality gates — design-before-build, shift-left visual QA, rendered-UI review evidence, the PO visual demo gate, and the Production Layout Checklist.

Visual design quality and production-layout UX are first-class quality gates, with the same standing as code correctness, security, and accessibility.

These gates exist because code-reading reviews alone cannot catch layout, spacing, hierarchy, and polish problems. Someone must look at the rendered application in a browser before the Product Owner does.

---

## 1. Design Before Build

Any backlog item with UI impact requires an approved visual/interaction design spec BEFORE implementation starts.

- Design specs live under `docs/design/`.
- A visual design spec has the same standing as an API contract: missing spec means the item is not Ready (see `definition-of-ready.md`).
- The UX/UI Designer owns the visual design spec as a pre-implementation deliverable.
- The spec must cover: layout structure, navigation placement, spacing/typography rhythm, component states (loading/empty/error), responsive behavior, and the Production Layout Checklist below.
- Spec statuses follow `spec-driven-development.md` (Draft, In Review, Approved, ...). Implementation may start only against an Approved design spec, or with an explicitly approved exception.

---

## 2. Shift-Left Visual QA

The implementing engineer must verify the rendered UI in a running browser (dev server) against the design spec BEFORE handing off — unit tests alone are not sufficient UI verification.

The handoff note must include rendered-UI verification evidence:

- what screens/flows were checked in the browser,
- at which widths (desktop and mobile at minimum — see checklist widths below),
- which design-spec sections were verified,
- any deliberate deviations from the design spec, with reason.

A UI implementation handoff without this evidence is incomplete and must be returned to the engineer.

---

## 3. Rendered-UI Review Evidence

Accessibility Review and any UX/UI review of UI work must include a live rendered-app walkthrough (or screenshots of the rendered app) as evidence — code reading alone is not sufficient.

- The review report must state what was rendered, at which widths, and what was observed.
- A review that never rendered the app cannot mark UI findings `Resolved`.
- This applies equally to re-verification passes in the Iterative Review-Fix Loop (`phased-execution.md`).

---

## 4. PO Visual Demo Gate

Before a UI feature's review phases close, the Product Owner gets a visual demo checkpoint — screenshots or a live walkthrough of the rendered feature — mirroring a real sprint review/demo.

- The demo checkpoint is recorded in the handoff notes (what was shown, PO feedback, resulting actions).
- PO visual feedback is triaged like review findings: fixed in the current loop, or explicitly accepted/deferred by the PO.
- Review phases for a UI feature are not closed and merged until this checkpoint has happened.

---

## 5. Production Layout Checklist

Used by the UX/UI Designer (when authoring/verifying the design spec) and by reviewers (when verifying the rendered app). Every UI feature must satisfy:

- [ ] Branded top navigation with primary menu and account affordance (placeholders acceptable for out-of-scope items).
- [ ] Clear page hierarchy with one dominant call-to-action per screen.
- [ ] Consistent container widths and spacing rhythm across screens.
- [ ] Real footer: link columns plus a legal line (placeholders acceptable).
- [ ] Responsive and unbroken at 360px, 768px, and 1280px widths.
- [ ] Loading, empty, and error states are styled — not browser defaults or bare text.
- [ ] Visible focus indication (`:focus-visible`) everywhere interactive.

Checklist deviations must be explicitly approved by the Product Owner and recorded.

---

## 6. Sprint Cadence

In-sprint design review happens BEFORE the implementation task starts. This reflects the real agile model:

- The SDLC Orchestrator schedules the UX/UI Designer's design-spec task BEFORE the corresponding implementation task during sprint planning (Phase 09, numbering per `CLAUDE.md` §7), so the approved spec exists when implementation starts (shift-left).
- Design and spec work is part of Definition of Ready — a story without an approved design spec does not enter implementation.
- Demos happen every sprint, not once at project end.
- Feedback loops are per-story, not per-project: the PO sees each UI story rendered before its review phases close, so visual quality issues surface within the sprint that built them.
