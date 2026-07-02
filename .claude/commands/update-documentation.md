---
description: Update project documentation.
argument-hint: "[documentation task]"
allowed-tools: Read, Write, Edit, MultiEdit, Grep, Glob, LS, Bash, WebFetch
---

Use the technical-writer agent.

Update documentation for:

$ARGUMENTS

Allowed Bash:

- `pwd`
- `ls`
- `dir`
- `git status`
- `mkdir`

Do not delete files.
Do not run build/test/install/deploy commands unless explicitly instructed.
