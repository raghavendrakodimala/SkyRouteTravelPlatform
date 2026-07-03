---
description: Continue the SDLC workflow from current handoff state by running the next phase only.
argument-hint: "[optional scope] [--auto-commit-merge] [--no-push]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite, Task
---

Continue the SDLC workflow from current handoff state.

Read:

- `CLAUDE.md`
- `.claude/rules/phased-execution.md`
- `docs/handoffs/workflow-state.md`
- `docs/handoffs/current-handoff.md`
- `docs/handoffs/handoff-index.md`

Determine:

- last completed phase
- current phase
- next phase
- next responsible agent
- blockers
- required artifacts

Then run exactly the next phase using phased execution.

For the next phase:

1. Switch to `main`.
2. Create the correct phase branch.
3. Run only that phase.
4. Invoke responsible agents.
5. Verify artifacts.
6. Verify handoff.
7. Update workflow state.
8. Commit phase work if `--auto-commit-merge` is included.
9. Merge phase branch into `main` if `--auto-commit-merge` is included.
10. Delete completed phase branch if merge succeeds.
11. Stop after one phase.

Do not run multiple phases from `/sdlc-next`.

Do not push unless explicitly approved.
