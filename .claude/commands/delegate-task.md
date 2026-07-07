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
- at a phase boundary: `docs/handoffs/<sequence>-<from>-to-<to>-<scope>.md`; inside an iterative review-fix loop: append to `docs/handoffs/<phase>-loop-log.md` instead of a numbered file

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
