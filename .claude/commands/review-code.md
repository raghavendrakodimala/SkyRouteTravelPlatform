---
description: Create code review report.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite
---

Use the code-reviewer agent.

Review current branch changes for:

$ARGUMENTS

Create/update:

- `docs/reviews/code-review-<scope>.md`
- `docs/handoffs/`

Allowed Bash:

- `git status`
- `git diff`
- `git diff --stat`
- `git log --oneline -n 5`

Do not modify source code, tests, config, or CI/CD files.
