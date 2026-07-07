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
- `.claude/rules/ui-ux-quality-gates.md` if the phase touches UI
- `docs/handoffs/workflow-state.md`
- `docs/handoffs/current-handoff.md`

Use the canonical phase numbering, agents, artifacts, and commit/merge messages from the phase list in `.claude/commands/run-full-sdlc.md` (01 Scrum operating model through 24 Final SDLC summary; repository/tooling setup is Phase 00, once, outside the loop).

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

Review phases 15–18 run the Iterative Review-Fix Loop internally and merge only at zero `Open` findings (`.claude/rules/phased-execution.md`).

UI checkpoints (`.claude/rules/ui-ux-quality-gates.md`): approved design spec before UI implementation; rendered-UI evidence before implementation handoff; PO visual demo before UI review phases close.

Efficiency:

- Parallelize independent specialist work inside the phase when output files are disjoint.
- Do not re-read unchanged artifacts — workflow-state.md plus current-handoff.md are the trusted resume point.
- Safe commands (build, test, lint, type-check, `git status`/`git diff`/`git log`) never require a stop; stop only for `CLAUDE.md` §21 conditions.
- Numbered handoff files at the phase boundary only; inside a review-fix loop use `docs/handoffs/<phase>-loop-log.md`, with `current-handoff.md` mirroring the latest state.

If `$ARGUMENTS` includes `--auto-commit-merge`, Git commit and merge are approved for this phase only.

Do not push unless `$ARGUMENTS` includes `--push-approved`.

Do not perform future phase work.
