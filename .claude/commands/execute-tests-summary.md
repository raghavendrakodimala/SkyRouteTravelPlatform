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
2. Run safe local test commands.
3. Capture exact commands executed.
4. Capture pass/fail/blocked/not-run status.
5. Summarize failed tests with QA IDs.
6. Identify defects, risks, and missing coverage.
7. Provide final QA recommendation.

Do not claim tests passed unless command output confirms it.
