---
name: direct-merge-git-flow
description: Use when preparing branch commits and direct merges.
---

# Direct Merge Git Flow Skill

Branch-based development with direct merge. Full flow, pre-merge checklist, and conflict handling: `.claude/rules/git-workflow.md`.

Standard flow:

```bash
git switch main
git switch -c branch-name

git add .
git commit -m "message"

git switch main
git merge --no-ff branch-name -m "merge: description"
git branch -d branch-name
```

Rules:

- Do not commit, merge, push, or delete branches unless explicitly instructed by the user. In phased autopilot, `--auto-commit-merge` is that instruction for commit and merge only.
- Standing no-push rule: never push unless the user explicitly approves push (`--push-approved`).
- Nested-duplicate hazard: the `SkyRouteTravelPlatform/` folder at repo root is gitignored (`/SkyRouteTravelPlatform/` in `.gitignore`). Never edit or force-add anything under it; before committing, confirm `git status` lists no paths under it.
