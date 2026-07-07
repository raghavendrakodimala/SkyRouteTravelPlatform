---
description: Create security review report.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, WebFetch, WebSearch, TodoWrite
---

Use the security-reviewer agent.

Review security for:

$ARGUMENTS

Create/update:

- `docs/reviews/security-review-<scope>.md` — `SEC-*` findings per `.claude/rules/review-and-test-reporting.md`
- `docs/handoffs/<phase>-loop-log.md` and `docs/handoffs/current-handoff.md`

Check input validation, unsafe logging, sensitive data exposure, error responses, secret handling, dependency risks, OWASP risks, and configuration risks.

Evidence rule: every finding must quote the offending code with file and line.

This is not a findings-only pass. Drive the Iterative Review-Fix Loop (`.claude/rules/phased-execution.md`):

1. File findings, all `Open`.
2. Orchestrator routes each `Open` finding to a developer agent per the routing table in `.claude/rules/delegation-rules.md`. Send the human a non-blocking FYI before a Critical/High fix starts.
3. Developer fixes source and tests; the developer never edits the review report.
4. The same security-reviewer re-verifies, scoped to the changed files/finding IDs, and updates statuses.
5. Repeat until the report shows zero `Open` findings.

The reviewer must not modify code, dependencies, secrets, or configuration — fixes go through the loop.
