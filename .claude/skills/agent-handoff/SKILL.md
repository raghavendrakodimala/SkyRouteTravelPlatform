---
name: agent-handoff
description: Use when passing work from one agent to another.
---

# Agent Handoff Skill

Store handoffs under `docs/handoffs/`.

Required content per handoff: the fields in `CLAUDE.md` §6 (Handoff ID, Date, Branch, Phase, From/To agent, Status, Work completed, Artifacts, Decisions, Open questions, Risks and impediments, Required next action, Completion criteria for the next step, Relevant files).

File model (handoff efficiency):

- Create numbered handoff files (`<sequence>-<from>-to-<to>-<scope>.md`) at phase boundaries only — not per task.
- Inside an Iterative Review-Fix Loop (review phases), append each iteration (finding IDs routed, developer fix evidence, re-review result) to a single per-phase loop log: `docs/handoffs/<phase>-loop-log.md`. Do not create numbered files per iteration.
- `docs/handoffs/current-handoff.md` always mirrors the latest handoff or loop state.
- Update `docs/handoffs/handoff-index.md` and `docs/handoffs/workflow-state.md` at each phase boundary.

For UI implementation handoffs, include rendered-UI verification evidence per `.claude/rules/ui-ux-quality-gates.md` §2 — without it the handoff is incomplete.
