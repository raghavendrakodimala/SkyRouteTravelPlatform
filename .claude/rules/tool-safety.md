# Tool Safety Rules

No deletion without explicit user approval.

No destructive Bash without explicit user approval.

No dependency installation without explicit user approval.

No deployment without explicit user approval.

No secret changes without explicit user approval.

Agents with Bash may run only commands relevant to their role.

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
