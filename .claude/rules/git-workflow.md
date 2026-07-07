# Git Workflow Rules

Owner concept: this file owns the general branch/commit/merge flow and conflict handling. Phase-branch naming and phased-autopilot git permissions live in `phased-execution.md`.

This project uses branch-based development with direct merge.

No PR is required.

## Standard Flow

```bash
git switch main
git switch -c branch-name

# work
git add .
git commit -m "message"

git switch main
git merge --no-ff branch-name -m "merge: description"
git branch -d branch-name
```

## Merge Rules

Before merging:

- Working tree must be clean.
- Test execution summary should exist for implementation branches.
- Critical/High review findings should be resolved or explicitly accepted.
- Delivery tracking should be updated.
- Documentation should be updated.
- Handoff notes should be complete.

## Conflict Handling

If conflicts occur:

```bash
git status
# resolve files
git add .
git commit
```

Abort merge if needed:

```bash
git merge --abort
```
