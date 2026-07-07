---
description: Execute tests and create test execution summary.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite, Task
---

Use the functional-tester agent.

Execute and summarize testing for:

$ARGUMENTS

Create/update:

- `docs/testing/execution/<scope>-test-execution-summary.md`
- `docs/handoffs/`

Steps:

1. Identify relevant test commands.
2. Run safe local test commands in non-watch/single-run mode.
3. Capture exact commands executed.
4. Capture exact counts from output (passed/failed/skipped) and pass/fail/blocked/not-run status per area — no estimates.
5. Summarize failed tests with QA IDs.
6. Identify defects, risks, and missing coverage.
7. Provide final QA recommendation.

Never claim tests passed unless command output confirms it. If tests cannot run, state: tests not run, the reason, and the recommended command.
