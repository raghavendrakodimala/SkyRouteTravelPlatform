# Agent Communication Rules

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

Agents must not assume another agent has completed work unless:

- the artifact exists, and
- the handoff confirms completion.

Every handoff must identify:

- next responsible agent
- required next action
- blockers
- files to read
- decisions made
- open questions
