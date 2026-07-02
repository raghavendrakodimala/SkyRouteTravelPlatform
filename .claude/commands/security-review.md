---
description: Create security review report.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, WebFetch, WebSearch, TodoWrite
---

Use the security-reviewer agent.

Review security for:

$ARGUMENTS

Create/update:

- `docs/reviews/security-review-<scope>.md`
- `docs/handoffs/`

Check input validation, unsafe logging, sensitive data exposure, error responses, secret handling, dependency risks, OWASP risks, and configuration risks.

Do not modify code, dependencies, secrets, or configuration.
