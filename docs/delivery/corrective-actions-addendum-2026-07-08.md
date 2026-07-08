# Corrective Actions Addendum — PO Closing Verdict, 2026-07-08

Addendum to `corrective-actions-2026-07-07.md` (Findings 1–8). This addendum records the Product Owner's systemic closing verdict as Finding 9 and the structural fix that institutionalizes it.

---

## Finding 9 — Systemic: the human PO was the quality mechanism (~15 issues the agents should have caught)

**PO verdict (substance):** Across the delivery, the human Product Owner personally caught roughly 15 issues that the agent team should have caught itself. The PO's standing expectation: "agents should work perfectly where the human is not supposed to point out the issues." Individually, Findings 1–8 were fixed; this finding names the systemic pattern behind all of them.

**Issue tally by class:**

| Class | Issues the PO caught | Count |
|---|---|---|
| Spec-as-ceiling omissions — professional-baseline items no spec mentioned, so nobody built them | Missing API documentation (OpenAPI + interactive reference UI); missing health-check endpoint with real dependency checks; no API versioning strategy; per-controller route prefix duplication (no global routing convention); no designed relational data model before entities/DTOs (Findings 1/5); incoherent workspace layout | ~6 |
| Experience-validation gaps — artifacts were validated, the running product was not used adversarially | First-pass UI far below production quality (Finding 2); select overflow shipping through every review (Finding 3); stale same-airport validation error wedging the form (Finding 7); most route combinations returning the empty state (Finding 8); missing format hints on format-validated fields and empty/error states not demonstrably reachable through real usage | ~5 |
| Hygiene defects — repository state nobody owned | Nested duplicate repository tree; e2e tsconfig not running clean standalone; stale delivery registers | ~3 |

**Root causes (confirmed across all findings):**

1. **Requirements were treated as a CEILING.** Agents built exactly what specs enumerated and nothing more. Professional-baseline items that no requirement mentioned — API docs, health checks, versioning, route conventions, data model, workspace hygiene — were silently never built, and no gate registered their absence as a defect.
2. **Agents validated ARTIFACTS instead of EXPERIENCES.** Reviews read code and checked documents; nobody used the running product the way a demanding human does — arbitrary inputs, every screen, hostile paths. Defects that only manifest in real usage (overflow, stale validation, empty routes) were structurally invisible.

**Structural fix:**

| File | Change |
|---|---|
| `.claude/rules/production-readiness-baseline.md` (new) | Standing production-readiness baseline — "requirements are floors, not ceilings." Every delivery must implement each baseline item (API/service, data, frontend/UX, engineering hygiene) or explicitly waive it as a dated decision-log entry; silent omission is a defect. Includes the Red-Team Product Review gate: before any PO demo checkpoint or phase closure of user-facing work, code-reviewer or ux-ui-designer (by subject) role-plays a demanding client/senior engineer against the RUNNING product with hostile inputs, and files findings through the Iterative Review-Fix Loop. A demo without a preceding red-team pass is incomplete. Closes root causes 1 and 2 respectively. |
| `CLAUDE.md` | §8 spec-readiness list now requires baseline items planned/implemented/waived; §22 notes the red-team pass precedes the PO visual demo gate. |
| `.claude/rules/definition-of-done.md` | Done now requires the production-readiness baseline satisfied or items explicitly waived. |
| `.claude/rules/ui-ux-quality-gates.md` | PO Visual Demo Gate (§4) now requires a preceding Red-Team Product Review. |
| `.claude/rules/common-agent-rules.md` | New universal Pre-Handoff Self-Check: "what would a demanding senior human engineer point out here?" — if the answer is known, fix it before handing off. |
| `.claude/agents/sdlc-orchestrator.md` | New duties: schedule the red-team review before demo gates; enforce baseline-or-waiver at phase completion. |
| `.claude/commands/run-full-sdlc.md`, `.claude/commands/sdlc-next.md` | Baseline and red-team gate wired into the phase checkpoints (implementation pre-commit, code review, accessibility/demo closure, per-phase completion). |
| `corrective-actions-2026-07-07.md` (prior pass) | Findings 1–8 structural fixes (data-model-first, designer production benchmark, accessibility viewport/zoom/overflow protocol, PO demo-gate rigor, architect completeness bar, layout-integrity sweep, stale-computed rule, input-domain sweep) remain in force and are the per-finding complement to this systemic fix. |

---

## Closing Statement

The measure of success for this corrective-action program is simple: **zero PO-caught defects in the next delivery cycle.** The human Product Owner reviews to confirm quality, not to find what the agents missed.
