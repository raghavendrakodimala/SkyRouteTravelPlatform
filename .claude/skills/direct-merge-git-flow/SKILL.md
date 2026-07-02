---
name: direct-merge-git-flow
description: Use when preparing branch commits and direct merges.
---

# Direct Merge Git Flow Skill

Use branch-based development with direct merge.

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

Do not commit, merge, push, or delete branches unless explicitly instructed by the user.
