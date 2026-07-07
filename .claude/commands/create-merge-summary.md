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

Include summary, branch, changed areas, tests performed, review reports, open risks, known limitations, and a merge readiness checklist per `.claude/rules/git-workflow.md` (clean tree, test summary, Critical/High findings resolved or accepted, tracking/docs/handoffs updated).

Also note in the summary:

- Standing no-push rule: local merge only; never push unless the user explicitly approves push.
- Nested-duplicate hazard: the `SkyRouteTravelPlatform/` folder at repo root is gitignored (`/SkyRouteTravelPlatform/` in `.gitignore`). Confirm `git status` lists no paths under it and nothing was force-added.

Do not modify source code.
