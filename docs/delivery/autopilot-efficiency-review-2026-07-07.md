# Autopilot Efficiency Review — Rules Pass — 2026-07-07

PO-ordered efficiency/quality audit of the SDLC autopilot rulebook (`CLAUDE.md` + `.claude/rules/`). This document records the defects found, the canonical decisions adopted, and the files changed by this rules pass.

---

## 1. Defects Found

### D-1 — Inconsistent phase numbering (Primary; observed causing real confusion in operation)

Three mutually incompatible phase-numbering schemes existed simultaneously:

| Source | Divergent citation (exact) |
|---|---|
| `CLAUDE.md` §7 (before) | 25-phase list starting at `1. Repository/tooling setup`, ending `25. Direct merge to main`; trailing paragraph said "**Phases 16–19** (Code, Security, Accessibility, Performance Review)" and "**Phase 20** ('Fix review/test findings')" |
| `CLAUDE.md` §22 (before) | "Iterative Review-Fix Loop (**Phases 15–18**)" — contradicting §7 of the same file |
| `.claude/rules/phased-execution.md` (before) | "Iterative Review-Fix Loop (**Phases 15–18**)"; branch examples `sdlc/12-implementation-...`, `sdlc/15-code-review-...` — consistent with §22 but not §7 |
| `.claude/rules/sdlc-rules.md` (before) | Its own third scheme: a 21-step list with "**15. Reviews**", "**16. Fixes**", "**21. Merge**" |
| Delivered history (`docs/handoffs/workflow-state.md` phase table) | 01 Scrum … 11 Spec readiness, 12 Implementation, 13 Test writing, 14 Test execution summary, 15 Code review, 16 Security review, 17 Accessibility review, 18 Performance review, 19 Findings fixes, 20 Re-test/re-review, 21 Delivery tracking, 22 Sprint review, 23 Retrospective, 24 Final SDLC summary |

### D-2 — Handoff file noise inside review-fix loops

Every loop iteration minted a numbered handoff file; the delivered project accumulated HO-027–HO-031 as unindexed noise (and HO-023–HO-025 had to be backfilled into the index).

### D-3 — Autopilot friction

No explicit permission to parallelize independent specialists, batch documentation-only phases, or resume from workflow state without re-reading unchanged artifacts; safe validation commands were ambiguously stop-worthy.

### D-4 — Duplicated/divergent normative text across rules files

`sdlc-rules.md` restated its own (divergent) Definition of Done and gates; `agent-communication.md` carried a divergent partial handoff-content list (6 fields vs. the canonical 15); no rules file declared which concept it canonically owns.

---

## 2. Canonical Decisions Adopted

1. **Phase numbering** — the delivered-history numbering is canonical: **Phase 00 — Repository/tooling setup** as a one-time pre-phase outside the loop, plus **Phases 01–24** (01 Scrum operating model … 15 Code review, 16 Security review, 17 Accessibility review, 18 Performance review, 19 Fix review/test findings, 20 Re-test and re-review, 21 Delivery tracking update, 22 Sprint review, 23 Retrospective, 24 Final SDLC summary). There is no separate merge phase — each phase merges its own branch. Defined once in `CLAUDE.md` §7; all other files cite it and must not restate the list. Review loop = **Phases 15–18**; findings-fixes consolidation = **Phase 19**.
2. **Handoff economy** — numbered handoff files at phase boundaries only; inside an Iterative Review-Fix Loop, all participants append to a single per-phase loop log `docs/handoffs/<phase>-loop-log.md`; `current-handoff.md` always mirrors the latest state; the index lists each loop log once. Required handoff content fields unchanged and apply to loop-log entries too.
3. **Autopilot efficiency** — (a) parallel invocation of independent specialists within a phase when file sets are disjoint; (b) PO-pre-approved batching of adjacent documentation-only phases onto one branch, recorded in the commit message; (c) `workflow-state.md` + `current-handoff.md` are the authoritative resume point — unchanged artifacts are not re-read between phases; (d) safe validation commands (build/test/lint/type-check, `git status`/`diff`/`log`) never require a human stop; the only stop conditions are `CLAUDE.md` §21's gates plus technical blockers.
4. **Validation pre-approval** — running existing test suites, builds, linters, and type-checks is pre-approved for every agent whose role requires validation; installs/upgrades and destructive operations remain gated. No safety rule was weakened; §21 human approval gates are intact in meaning.
5. **Cross-reference hygiene** — every rules file now states its owner concept once; duplicated normative text was replaced with pointers to the owning file (canonical DoR/DoD/spec rules/phase list each live in exactly one place).
6. **Shift-left design scheduling** — the orchestrator schedules the UX/UI design-spec task before the implementation task in sprint planning (Phase 09), per `ui-ux-quality-gates.md`.

---

## 3. Files Changed by This Rules Pass

| File | Change |
|---|---|
| `CLAUDE.md` | §7 rewritten to canonical Phase 00 + Phases 01–24; trailing paragraph now cites 15–18/19; §5 handoff file economy (loop log) added; §6 fields apply to loop-log entries; §15 validation pre-approval; §20 efficiency rules + stop conditions tied to §21; §22 efficiency rules + loop evidence goes to the loop log. §3 agent table verified against `.claude/agents/` (17 agents, exact match — no changes needed). |
| `.claude/rules/sdlc-rules.md` | Divergent 21-step list removed; now points to `CLAUDE.md` §7 with 15–18/19 structural notes; DoD and gates replaced with pointers to their owning files. |
| `.claude/rules/phased-execution.md` | Numbering/examples verified already canonical (no renumbering needed); new Autopilot Efficiency Rules section; loop step 3 evidence goes to the per-phase loop log; owner line added. |
| `.claude/rules/agent-communication.md` | Handoff File Economy section (phase-boundary numbered files, per-phase loop log, mirror/current, index once); divergent partial content list replaced with pointer to `common-agent-rules.md`; owner line added. |
| `.claude/rules/common-agent-rules.md` | Handoffs section: numbered files at phase boundaries only, loop-log rule; content fields apply to loop-log entries; validation pre-approval pointer in Safety; owner line added. Required handoff content list unchanged. |
| `.claude/rules/tool-safety.md` | Explicit pre-approved validation list (test suites, builds, lint, type-check, read-only git); gates unchanged; owner line added. |
| `.claude/rules/ui-ux-quality-gates.md` | Orchestrator schedules design spec before implementation in sprint planning (Phase 09); owner line added; verified coherent with the loop/handoff changes. |
| `.claude/rules/orchestration-rules.md` | Owner line added (control loop; numbering per `CLAUDE.md` §7). |
| `.claude/rules/review-and-test-reporting.md` | Owner line added (finding-ID scheme, report content). |
| `.claude/rules/delegation-rules.md` | Owner line added (delegation authority, routing table). |
| `.claude/rules/git-workflow.md` | Owner line added (general git flow; phased specifics in `phased-execution.md`). |
| `.claude/rules/nfr-governance.md` | Owner line added. |
| `.claude/rules/definition-of-done.md` | Owner line added (canonical DoD). |
| `.claude/rules/definition-of-ready.md` | Owner line added (canonical DoR). |
| `.claude/rules/spec-driven-development.md` | Owner line added (canonical spec requirements/statuses/traceability). |

Verification: a post-edit sweep of `CLAUDE.md` and all `.claude/rules/*.md` confirms every remaining phase-number citation matches the canonical scheme (15–18 review loop, 19 findings fixes, 09 sprint planning, 00 pre-phase, 01–24 list; branch/commit examples `01/02/03/12/13/15/15a/19` all canonical).

## 4. Companion passes

### Pass B — Agent definitions (17 files, `.claude/agents/`)

All 17 definitions rewritten to a uniform, lean role contract (32–44 lines each): mission, owned artifacts, role-specific "excellent output" quality bar, tools with rationale, delegation rights and handoff duties by pointer. Key upgrades: code-reviewer must verify fixes against CURRENT code and may run build/tests (previously forbidden); security-reviewer gained an OWASP checklist and loop re-verification duties; performance-tester now requires runtime measurements ("code reading alone is not a performance review"); **accessibility-tester and ux-ui-designer gained Bash** for dev-server/evidence runs per `ui-ux-quality-gates.md`; functional-tester explicitly owns keeping `src/UI/e2e/` aligned whenever UX changes; the three developer tiers carry the delegation-rules routing criteria verbatim; sdlc-orchestrator gained explicit efficiency duties (parallelize disjoint specialists, no re-reading unchanged artifacts, loop-log consolidation, §21-only stops).

### Pass C — Commands + skills (30 commands, 6 skills, `.claude/commands/`, `.claude/skills/`)

Every phase-citing command aligned to the canonical 01–24 model. Review commands (review-code/security-review/accessibility-review/performance-review) now describe the full Iterative Review-Fix Loop with three-tier evidence requirements (quoted code / rendered app at 360-768-1280px / runtime measurements). Autopilot commands (run-full-sdlc/run-sdlc-phase/sdlc-next) gained the four efficiency provisions (parallel disjoint specialists; PO-pre-approved doc-phase batching; workflow-state/current-handoff as resume point; safe commands never stop). sdlc-status is now a fast reader of the three state files only (no tree scans). spec-readiness-check/implement-feature/create-feature-specs wire in the design-spec-before-build gate; execute-tests-summary/write-tests enforce non-watch runs and exact output-derived counts; direct-merge-git-flow records the standing no-push rule and the nested-duplicate-folder hazard. accessibility-review's allowed-tools gained Bash (orchestrator addition).

### Orchestrator resolutions of the flagged contradictions

1. Dev servers vs. §14: transient dev-server runs for rendered-UI verification, PO demos, and e2e execution are now pre-approved in CLAUDE.md §14/§15, provided servers are stopped after evidence capture.
2. Branch deletion vs. §14: CLAUDE.md §22 now states `--auto-commit-merge` includes approval to delete a phase branch after its successful merge — and nothing else.
3. Branch naming: CLAUDE.md §18 scoped to out-of-band work; phased autopilot uses the `sdlc/<nn>-…` scheme from `phased-execution.md`.

### Cross-scope verification (orchestrator)

Post-edit sweep: zero stale "16–19" citations anywhere; `docs/handoffs/<phase>-loop-log.md` convention identical across CLAUDE.md, rules, agents, commands, and skills; review loop cited as 15–18 in all scopes.
