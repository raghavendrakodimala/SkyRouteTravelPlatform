---
description: Evaluate and apply backlog priority changes with impact analysis.
argument-hint: "[priority change request]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, TodoWrite, Task
---

Handle this priority change request:

$ARGUMENTS

Use Product Owner as accountable role.

Before changing priorities:

1. Ask Project Coordinator for delivery impact.
2. Ask Solution Architect for technical impact.
3. Ask Functional Tester for test impact.
4. Ask Scrum Master for sprint impact.
5. Identify risks/dependencies.

Update if approved:

- `docs/delivery/project-backlog.md`
- `docs/delivery/risk-register.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/decision-log.md`
- `docs/handoffs/`
- `docs/delivery/delegation-log.md` if tasks are reassigned

Do not change committed sprint scope without explicit human approval.
