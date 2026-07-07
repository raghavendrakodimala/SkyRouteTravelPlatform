---
description: Create performance review report.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, WebFetch, WebSearch, TodoWrite
---

Use the performance-tester agent.

Review performance for:

$ARGUMENTS

If no performance-sensitive path changed, create a Not Applicable review note and handoff, and stop.

Create/update:

- `docs/reviews/performance-review-<scope>.md` — `PERF-*` findings per `.claude/rules/review-and-test-reporting.md`
- `docs/testing/performance/` if needed
- `docs/handoffs/<phase>-loop-log.md` and `docs/handoffs/current-handoff.md`

Evidence rules:

- Quote the offending code with file and line.
- When the app or tests can run safely, back findings and re-verifications with runtime measurements (response times, payload sizes, timings) — not code reading alone.
- Do not run heavy load tests without approval.

This is not a findings-only pass. Drive the Iterative Review-Fix Loop (`.claude/rules/phased-execution.md`):

1. File findings, all `Open`.
2. Orchestrator routes each `Open` finding to a developer agent per the routing table in `.claude/rules/delegation-rules.md`.
3. Developer fixes source and tests; the developer never edits the review report.
4. The same performance-tester re-verifies, re-measuring where possible, and updates statuses.
5. Repeat until the report shows zero `Open` findings.

The reviewer must not modify application code — fixes go through the loop.
