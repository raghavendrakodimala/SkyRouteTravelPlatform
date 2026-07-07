---
description: Show current SDLC workflow status.
argument-hint: ""
allowed-tools: Read, Grep, Glob, LS, Bash
---

Show SDLC workflow status. This is a fast status check — no full-tree scan.

Read ONLY:

- `docs/handoffs/workflow-state.md`
- `docs/handoffs/current-handoff.md`
- `docs/handoffs/handoff-index.md`

Do not read `docs/delivery/`, `docs/reviews/`, `docs/testing/`, or source files. If a status item is not recorded in the three files above, report it as "not recorded in workflow state" instead of scanning for it.

Allowed Bash:

- `git status`
- `git log --oneline -n 10`

Report:

- current branch
- current SDLC phase
- last completed agent
- next agent
- open blockers
- open review findings (as recorded in workflow state / current handoff)
- open QA findings (as recorded in workflow state / current handoff)
- recommended next command
