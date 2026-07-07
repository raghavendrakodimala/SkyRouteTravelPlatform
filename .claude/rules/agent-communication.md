# Agent Communication Rules

Owner concept: this file owns how agents communicate and where persistent handoff state lives, including the handoff file economy. The required handoff content fields are owned by `common-agent-rules.md` (Handoff Content).

Agents communicate through:

1. Main Claude orchestrator
2. In-session delegation
3. Persistent handoff files

Persistent handoff location:

- `docs/handoffs/`

Required files:

- `docs/handoffs/workflow-state.md`
- `docs/handoffs/handoff-index.md`
- `docs/handoffs/current-handoff.md`

Every agent must create a handoff after completing a task.

The main orchestrator must read the handoff before invoking the next agent.

## Handoff File Economy

- Numbered handoff files are created at phase boundaries only (naming per `common-agent-rules.md`).
- Inside an Iterative Review-Fix Loop (`CLAUDE.md` §22, Phases 15–18), participants do not mint numbered handoff files per iteration. All loop participants append entries to a single per-phase loop log: `docs/handoffs/<phase>-loop-log.md` (e.g. `docs/handoffs/15-code-review-loop-log.md`).
- `docs/handoffs/current-handoff.md` always mirrors the latest handoff state, whether it came from a numbered file or a loop-log entry.
- `docs/handoffs/handoff-index.md` lists each numbered handoff file and each phase's loop log once; individual loop iterations do not get index rows.

## Handoff Trust

Agents must not assume another agent has completed work unless:

- the artifact exists, and
- the handoff confirms completion.

## Handoff Content

Every handoff — numbered file or loop-log entry — must include the required content fields defined in `common-agent-rules.md` (Handoff Content), including in particular: next responsible agent, required next action, blockers, files to read, decisions made, and open questions.
