# Common Agent Rules

All agents must follow these rules.

## Safety

- Do not delete files or folders without explicit user approval.
- Do not run destructive commands without explicit user approval.
- Do not introduce dependencies without explicit user approval.
- Do not deploy, publish, or change secrets without explicit user approval.

## Handoffs

Every agent must create or update:

- `docs/handoffs/current-handoff.md`

Every completed task should also create a numbered handoff file:

- `docs/handoffs/<sequence>-<from-agent>-to-<to-agent>-<scope>.md`

## Handoff Content

Every handoff must include:

- Handoff ID
- Date
- Branch
- Phase
- From agent
- To agent
- Status
- Work completed
- Artifacts created or updated
- Decisions made
- Open questions
- Risks and impediments
- Required next agent action
- Completion criteria for next step
- Relevant files

## Reporting

After work, summarize:

- Files created
- Files updated
- Files deleted, if any
- Commands run
- Command results
- Next recommended action

## Human Approval

Stop for human approval before:

- scope changes
- priority changes with impact
- new dependencies
- file deletion
- destructive commands
- deployment
- secret changes
- accepting unresolved Critical/High risks
- committing or merging if not explicitly instructed
