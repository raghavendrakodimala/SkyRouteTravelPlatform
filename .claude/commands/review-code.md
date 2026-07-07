---
description: Create code review report.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite
---

Use the code-reviewer agent.

Review current branch changes for:

$ARGUMENTS

Create/update:

- `docs/reviews/code-review-<scope>.md` — `CR-*` findings per `.claude/rules/review-and-test-reporting.md`
- `docs/handoffs/<phase>-loop-log.md` and `docs/handoffs/current-handoff.md`

Evidence rule: every finding must quote the offending code with file and line.

This is not a findings-only pass. Drive the Iterative Review-Fix Loop (`.claude/rules/phased-execution.md`):

1. File findings, all `Open`.
2. Orchestrator routes each `Open` finding to a developer agent per the routing table in `.claude/rules/delegation-rules.md`.
3. Developer fixes source and tests; the developer never edits the review report.
4. The same code-reviewer re-verifies, scoped to the changed files/finding IDs, and updates statuses.
5. Repeat until the report shows zero `Open` findings.

Allowed Bash:

- `git status`
- `git diff`
- `git diff --stat`
- `git log --oneline -n 5`
- safe build/test/lint/type-check commands to verify fixes

The reviewer must not modify source code, tests, config, or CI/CD files — fixes go through the loop.
