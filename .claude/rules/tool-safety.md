# Tool Safety Rules

Owner concept: this file owns command-level tool safety — what is forbidden without explicit user approval and what is pre-approved.

No deletion without explicit user approval.

No destructive Bash without explicit user approval.

No dependency installation without explicit user approval.

No deployment without explicit user approval.

No secret changes without explicit user approval.

Agents with Bash may run only commands relevant to their role.

Pre-approved for every agent whose role requires validation (no per-run human approval needed):

- running the project's existing test suites
- running builds
- running linters and type-checks
- read-only git commands (`git status`, `git diff`, `git log`)

Dependency installs and upgrades, and everything on the forbidden list below, remain gated behind explicit user approval.

Agents must report:

- commands run
- command results
- files created
- files updated
- files deleted, if any

Forbidden unless approved:

- rm -rf
- rmdir /s
- del /s
- git reset --hard
- git clean -fd
- git clean -fdx
- git checkout -- .
- git restore .
- force push
- rebase
- deployment
- publishing
- secret changes
- database reset/drop commands
