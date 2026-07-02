---
description: Continue the SDLC workflow from current handoff state.
argument-hint: "[optional context]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite, Task
---

Continue the SDLC workflow.

Read:

- `docs/handoffs/workflow-state.md`
- `docs/handoffs/current-handoff.md`
- `docs/handoffs/handoff-index.md`

Determine:

- current phase
- completed phase
- next responsible agent
- required next action
- blockers

Then invoke the next responsible agent.

After the agent finishes:

- verify artifacts
- verify handoff
- update workflow state
- recommend commit command if appropriate

Do not commit or merge unless explicitly instructed.
