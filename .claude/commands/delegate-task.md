---
description: Delegate a task from one agent to another using the approved delegation model.
argument-hint: "[from agent] -> [to agent]: [task]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, TodoWrite, Task
---

Delegate this task:

$ARGUMENTS

Before delegating:

1. Read `.claude/rules/delegation-rules.md`.
2. Confirm the from-agent is allowed to delegate to the to-agent.
3. Confirm the task is within scope.
4. Confirm no human approval gate is triggered.

Create/update:

- `docs/delivery/delegation-log.md`
- `docs/handoffs/current-handoff.md`
- appropriate `docs/handoffs/<sequence>-<from>-to-<to>.md`

Delegation brief must include:

- Delegation ID
- From agent
- To agent
- Objective
- Context files
- Expected artifacts
- Acceptance criteria
- Constraints
- Required handoff file

If the delegation is not allowed, stop and explain why.
