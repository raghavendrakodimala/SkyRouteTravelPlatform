# Phased SDLC Execution Rules

The SDLC workflow must run phase by phase.

The orchestrator must not complete the whole SDLC in one branch or one uncommitted change set.

Each phase is treated as an independent delivery transaction.

---

## Phase Transaction Model

For every SDLC phase:

1. Start from `main`.
2. Create a dedicated phase branch.
3. Run only the current phase work.
4. Invoke only the agents required for the current phase.
5. Create or update current phase artifacts.
6. Create or update handoff notes.
7. Update `docs/handoffs/workflow-state.md`.
8. Update `docs/handoffs/handoff-index.md`.
9. Validate expected artifacts exist.
10. Run safe validation commands if relevant.
11. Commit phase work in the phase branch.
12. Switch to `main`.
13. Merge the phase branch into `main` using `--no-ff`.
14. Delete the completed phase branch.
15. Start the next phase from updated `main`.

---

## Important Restriction

Do not perform future phase work inside the current phase branch.

Examples:

- During Requirements phase, do not implement code.
- During Architecture phase, do not write production code.
- During Test Strategy phase, do not implement tests unless explicitly part of that phase.
- During Code Review, Security Review, Accessibility Review, and Performance Review phases, the reviewer agent does not fix code directly — findings are routed through the Iterative Review-Fix Loop below.
- During Fixes phase, fix only tracked findings.

---

## Iterative Review-Fix Loop (Phases 15–18)

Code Review, Security Review, Accessibility Review, and Performance Review each loop internally instead of deferring every fix to Phase 19/20:

1. The reviewer agent files findings (all `Open`) in the phase's review report under `docs/reviews/`.
2. The orchestrator routes each `Open` finding to a developer agent by severity/complexity, per the routing table in `delegation-rules.md` ("Review Finding → Developer Agent Routing").
3. The developer agent fixes the finding (source + tests) and records evidence in a handoff note. The developer agent never edits the review report itself.
4. The orchestrator re-invokes the same reviewer agent, scoped to the changed files/finding IDs, to verify the fix.
5. The reviewer sets the finding to `Resolved` (verified), leaves it `Open`/`Partially Resolved` (loop continues), or files a new incremented finding ID if the fix revealed something new.
6. Repeat steps 2–5 until the report shows zero `Open` findings.

`Resolved`, `Accepted Risk`, `Deferred`, and `Rejected` are all valid terminal statuses. Only `Open`, `In Progress`, and `Partially Resolved` keep the loop going.

A developer agent must never itself set a finding to `Accepted Risk`, `Deferred`, or `Rejected` — that requires human approval recorded by the reviewer, except when a Low/informational finding's own original text already said no fix was required (the reviewer may mark that `Accepted Risk` directly).

Fixing a Critical/High finding and marking it `Resolved` does not itself trigger the human-approval gate in Blocker Handling below — that gate is for *accepting* a Critical/High finding *unresolved*. Send a non-blocking FYI to the human before starting a Critical/High fix as good practice.

Reviewers of UI phases must include rendered-app evidence (live walkthrough or screenshots, not code reading alone) per `.claude/rules/ui-ux-quality-gates.md` — both when filing findings and when verifying fixes.

A review phase branch is not committed/merged until its report shows zero `Open` findings (or the human has explicitly accepted every remaining item).

---

## Branch Naming

Use this branch format:

```text
sdlc/<phase-number>-<phase-name>-<scope-slug>
```

Examples:

```text
sdlc/01-scrum-operating-model-skyroute-mvp
sdlc/02-delivery-model-skyroute-mvp
sdlc/12-implementation-trip-search-mvp
sdlc/15-code-review-trip-search-mvp
```

A review phase reopened after merge to close out its Iterative Review-Fix Loop uses an `a`-suffixed branch off updated `main`:

```text
sdlc/15a-code-review-fixes-trip-search-mvp
```

---

## Commit Message Format

Use this commit format:

```text
docs: complete phase 01 scrum operating model
docs: complete phase 03 requirements analysis
feat: complete phase 12 implementation
test: complete phase 13 test writing
docs: complete phase 15 code review
fix: complete phase 19 findings fixes
```

---

## Merge Message Format

Use this merge format:

```text
merge: complete phase 01 scrum operating model
merge: complete phase 03 requirements analysis
merge: complete phase 12 implementation
```

---

## Git Commands Allowed in Phased Autopilot

When the user explicitly starts phased autopilot with auto-commit/merge approval, the SDLC Orchestrator may run:

```bash
git status
git diff --stat
git log --oneline -n 10
git switch main
git switch -c <branch>
git add .
git commit -m "<message>"
git merge --no-ff <branch> -m "<message>"
git branch -d <branch>
```

Do not push unless the user explicitly approves push.

Do not run destructive commands.

Do not use:

```bash
git reset --hard
git clean -fd
git clean -fdx
git checkout -- .
git restore .
git rebase
git push --force
```

---

## Phase Completion Criteria

A phase is complete only when:

- expected artifacts exist,
- handoff note exists,
- workflow state is updated,
- delegation log is updated if delegation occurred,
- relevant validation has been performed or explicitly marked not applicable,
- no blocking issue remains,
- for Code/Security/Accessibility/Performance Review phases, the review report shows zero `Open` findings (per the Iterative Review-Fix Loop above),
- for phases delivering UI, rendered-UI verification evidence exists per `.claude/rules/ui-ux-quality-gates.md`,
- changes are committed,
- phase branch is merged to `main`.

---

## Blocker Handling

Stop immediately if:

- working tree is dirty before starting a new phase,
- merge conflict occurs,
- required source requirement is missing,
- dependency installation is needed,
- file deletion is needed,
- destructive command is needed,
- deployment is requested,
- a Critical/High finding is being accepted unresolved (not being fixed — fixing within the Iterative Review-Fix Loop does not require a blocking gate, though a non-blocking FYI to the human is good practice),
- tests/build fail in a way that should not be merged.

When stopped, write a blocker note to:

```text
docs/handoffs/current-handoff.md
docs/handoffs/workflow-state.md
```

---

## Skipped or Not Applicable Phases

If a phase is not applicable, still record the phase as completed with status `Not Applicable`.

Examples:

- Accessibility Review is Not Applicable when no UI is involved.
- Performance Review is Not Applicable when no performance-sensitive path changed.

For skipped/not applicable phases, create a handoff note and commit that documentation.
