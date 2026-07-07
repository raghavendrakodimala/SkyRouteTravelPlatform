---
name: spec-driven-sdlc
description: Use when planning, checking readiness, or implementing through the spec-driven SDLC model.
---

# Spec-Driven SDLC Skill

Before implementation, confirm (full lists: `.claude/rules/spec-driven-development.md`, `.claude/rules/definition-of-ready.md`):

- Requirement/user story and acceptance criteria exist.
- Architecture direction exists.
- API contract exists if API work is involved.
- UI flow exists if UI work is involved.
- Approved visual design spec exists under `docs/design/` for UI work (`.claude/rules/ui-ux-quality-gates.md`).
- Test plan exists.
- NFRs are considered.
- Accessibility/security expectations exist where needed.
- Dependencies and risks are tracked.

If anything is missing, block implementation and request the correct agent handoff.
