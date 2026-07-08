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

Trust these as the resume point — do not re-read artifacts that are unchanged since the last completed phase.

Determine:

- last completed phase
- current phase
- next phase (canonical numbering per the phase list in `.claude/commands/run-full-sdlc.md`, 01–24; Phase 00 is one-time repo/tooling setup outside the loop)
- next responsible agent
- blockers
- required artifacts

Then run exactly the next phase using phased execution.

For the next phase:

1. Switch to `main`.
2. Create the correct phase branch.
3. Run only that phase.
4. Invoke responsible agents (parallelize independent specialists when output files are disjoint).
5. Verify artifacts.
6. Verify handoff.
7. Update workflow state.
8. Commit phase work if `--auto-commit-merge` is included.
9. Merge phase branch into `main` if `--auto-commit-merge` is included.
10. Delete completed phase branch if merge succeeds.
11. Stop after one phase.

Review phases 15–18 run the Iterative Review-Fix Loop internally and merge only at zero `Open` findings (`.claude/rules/phased-execution.md`).

UI checkpoints (`.claude/rules/ui-ux-quality-gates.md`): approved design spec before UI implementation; rendered-UI evidence before implementation handoff; PO visual demo before UI review phases close.

Production-readiness checkpoints (`.claude/rules/production-readiness-baseline.md`): every baseline item implemented or explicitly waived as a dated decision-log entry before the phase completes; a Red-Team Product Review (code-reviewer or ux-ui-designer by subject, against the running product with hostile inputs) precedes any PO demo checkpoint or phase closure of user-facing work.

Efficiency:

- Adjacent documentation-only phases MAY be batched onto one branch only when the PO explicitly pre-approved batching for this run; record it in the phase commit message.
- Safe commands (build, test, lint, type-check, `git status`/`git diff`/`git log`) never require a stop; stop only for `CLAUDE.md` §21 conditions.
- Numbered handoff files at the phase boundary only; inside a review-fix loop use `docs/handoffs/<phase>-loop-log.md`, with `current-handoff.md` mirroring the latest state.

Do not run multiple phases from `/sdlc-next` (a PO-pre-approved doc-only batch counts as one phase transaction).

Do not push unless explicitly approved.
