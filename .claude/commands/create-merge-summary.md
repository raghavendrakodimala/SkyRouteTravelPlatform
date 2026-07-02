---
description: Create local merge summary.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash
---

Create/update:

- `docs/delivery/merge-summary-<scope>.md`

Allowed Bash:

- `git status`
- `git diff --stat`
- `git log --oneline -n 10`

Include summary, branch, changed areas, tests performed, review reports, open risks, known limitations, and merge readiness checklist.

Do not modify source code.
