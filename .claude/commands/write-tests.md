---
description: Write or update automated tests.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, MultiEdit, Grep, Glob, LS, Bash, TodoWrite, Task
---

Use the functional-tester agent.

Write or update tests for:

$ARGUMENTS

Cover:

- unit tests
- API tests
- validation failures
- edge cases
- empty states
- UI/component behavior if applicable
- e2e smoke flow if applicable

Run safe test commands if appropriate, in non-watch/single-run mode.

Do not claim tests passed unless command output confirms it — report exact counts (passed/failed/skipped) from the output.
Do not delete tests without approval.
