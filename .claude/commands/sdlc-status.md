---
description: Show current SDLC workflow status.
argument-hint: ""
allowed-tools: Read, Grep, Glob, LS, Bash
---

Show SDLC workflow status.

Read:

- `docs/handoffs/workflow-state.md`
- `docs/handoffs/current-handoff.md`
- `docs/delivery/project-backlog.md`
- `docs/delivery/sprint-plan.md`
- `docs/reviews/`
- `docs/testing/execution/`

Allowed Bash:

- `git status`
- `git diff --stat`
- `git log --oneline -n 10`

Report:

- current branch
- current SDLC phase
- last completed agent
- next agent
- open blockers
- open review findings
- open QA findings
- recommended next command
