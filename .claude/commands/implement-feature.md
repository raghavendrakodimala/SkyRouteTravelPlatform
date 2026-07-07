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
2. For UI work, confirm an Approved visual design spec exists under `docs/design/` (`.claude/rules/ui-ux-quality-gates.md`) — if missing, stop and route to the ux-ui-designer.
3. Summarize implementation plan.
4. Identify files likely to change.
5. Identify tests to add/update.
6. Ask approval if dependencies or deletion are needed.

After implementation:

- update tests if needed
- run safe validation commands if appropriate
- for UI work: verify the rendered UI in a running browser at desktop and mobile widths against the design spec, and record rendered-UI verification evidence in the handoff (`.claude/rules/ui-ux-quality-gates.md` §2) — a UI handoff without it is incomplete
- summarize changed files
- summarize commands and results
- create/update handoff notes

Do not delete files without approval.
Do not introduce dependencies without approval.
Do not commit unless explicitly instructed.
