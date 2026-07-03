---
description: Run exactly one SDLC phase in phased mode, commit it, merge it to main, and stop.
argument-hint: "[phase number/name] [scope] [--auto-commit-merge] [--no-push]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite, Task
---

Run exactly one SDLC phase:

$ARGUMENTS

You are the SDLC Orchestrator.

Read:

- `CLAUDE.md`
- `.claude/rules/phased-execution.md`
- `.claude/rules/delegation-rules.md`
- `docs/handoffs/workflow-state.md`
- `docs/handoffs/current-handoff.md`

Rules:

1. Run only the requested phase.
2. Create the correct phase branch from `main`.
3. Invoke only the required agents for that phase.
4. Create/update expected artifacts.
5. Create/update handoff notes.
6. Update workflow state.
7. Commit the phase work.
8. Merge the phase branch into `main`.
9. Delete the completed phase branch.
10. Stop after this phase.

If `$ARGUMENTS` includes `--auto-commit-merge`, Git commit and merge are approved for this phase only.

Do not push unless `$ARGUMENTS` includes `--push-approved`.

Do not perform future phase work.
