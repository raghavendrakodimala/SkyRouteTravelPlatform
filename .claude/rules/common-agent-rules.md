# Common Agent Rules

Owner concept: this file owns the baseline obligations shared by every agent — safety stops, handoff mechanics and required handoff content, reporting, and human-approval stops.

All agents must follow these rules.

## Safety

- Do not delete files or folders without explicit user approval.
- Do not run destructive commands without explicit user approval.
- Do not introduce dependencies without explicit user approval.
- Do not deploy, publish, or change secrets without explicit user approval.
- Running the project's existing test suites, builds, linters, and type-checks is pre-approved for agents whose role requires validation (see `tool-safety.md` for the full command-level rules).

## Handoffs

Every agent must create or update:

- `docs/handoffs/current-handoff.md` — always mirrors the latest handoff state.

Numbered handoff files are created at phase boundaries only:

- `docs/handoffs/<sequence>-<from-agent>-to-<to-agent>-<scope>.md`

Inside an Iterative Review-Fix Loop (`CLAUDE.md` §22, Phases 15–18), do not mint a numbered handoff file per loop iteration — append your entry to the phase's single loop log instead:

- `docs/handoffs/<phase>-loop-log.md`

## Handoff Content

Every handoff — numbered file or loop-log entry — must include:

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

## Pre-Handoff Self-Check

Before any handoff, every agent asks: what would a demanding senior human engineer point out here? If the answer is known, fix it before handing off — the human PO must never be the one to find it.

Requirements are floors, not ceilings — see `production-readiness-baseline.md` for the standing baseline every delivery must satisfy or explicitly waive.

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
