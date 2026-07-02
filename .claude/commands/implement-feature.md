---
description: Implement approved feature or fix.
argument-hint: "[feature/fix scope]"
allowed-tools: Read, Write, Edit, MultiEdit, Grep, Glob, LS, Bash, WebFetch, WebSearch, TodoWrite, Task
---

Use the lead-full-stack-engineer agent.

Implement:

$ARGUMENTS

Before coding:

1. Confirm spec readiness.
2. Summarize implementation plan.
3. Identify files likely to change.
4. Identify tests to add/update.
5. Ask approval if dependencies or deletion are needed.

After implementation:

- update tests if needed
- run safe validation commands if appropriate
- summarize changed files
- summarize commands and results
- create/update handoff notes

Do not delete files without approval.
Do not introduce dependencies without approval.
Do not commit unless explicitly instructed.
