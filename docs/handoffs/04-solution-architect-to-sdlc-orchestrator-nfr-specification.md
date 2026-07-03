# Handoff: HO-004

| Field | Value |
|---|---|
| Handoff ID | HO-004 |
| Date | 2026-07-03 |
| Branch | sdlc/04-nfr-specification-skyroute-mvp |
| Phase | Phase 04 — Non-Functional Requirements Specification |
| From agent | solution-architect |
| To agent | sdlc-orchestrator |
| Status | Complete — Pending Human Product Owner Confirmation of Proposed Numeric Targets |

---

## Work Completed

- Read `docs/requirements.md` v1.4 (Approved) in full, including Section 5 (high-level NFR targets) and Section 3.10 (Design Principles and Architectural Constraints — all DP-* groups: interfaces, separation of concerns, testability, DP-AUTH, DP-PERSIST, DP-DB, DP-TENANT, DP-PROTOCOL, DP-UPGRADE, DP-POLICY, DP-ZEROTRUST, DP-CLOUD, DP-DEPLOY).
- Read prior handoffs (`workflow-state.md`, `handoff-index.md`, `current-handoff.md`, HO-003 + Revision 2 addendum) to confirm Phase 03 approval status and continuity.
- Produced `docs/specs/non-functional-requirements.md` (v1.0), covering all 14 NFR governance categories required by `.claude/rules/nfr-governance.md`: Performance, Scalability, Availability/Reliability, Security, Privacy/Data Protection, Accessibility, Usability, Maintainability, Testability, Observability/Logging, Compatibility, Deployability, Data Integrity, On-Premise/Cloud Readiness.
- Every NFR is stated with: ID, requirement text, MoSCoW priority, target/measurement, validation method, traceability to a US-*/FR-*/BR-*/DP-* item in requirements.md, and an explicit architecture link citing the relevant DP-* constraint(s).
- Backlog linkage is explicitly deferred: every NFR section notes "To be linked at Phase 07 Backlog Creation" rather than inventing backlog IDs, since the backlog does not yet exist.
- Elevated the specific items called out in the task brief:
  - Performance: turned "under 2s"/"under 1s" into p95/p50 percentile targets (NFR-PERF-001, NFR-PERF-002) and added a frontend sort-rerender NFR (NFR-PERF-003, sub-100ms) tied to US-003/FR-021.
  - Security: split into "required now" (NFR-SEC-001–007, citing OWASP Top 10 2021 categories A01/A03/A04/A05 by name) and "must not preclude later" (NFR-SEC-008–011, citing NIST SP 800-207 Zero Trust and DP-AUTH-005/006 OIDC/OAuth2/SAML/SSO addability).
  - Deployability: elevated DP-DEPLOY-001–006 and DP-CLOUD-001–005 into NFR-DEPLOY-001–006 and NFR-ONPREM-001–004, with the 12-factor "zero source-code-change" acceptance test stated explicitly.
  - Maintainability/Testability: elevated DP-UPGRADE-001–004, DP-POLICY-001–003, and DP-017–020 into NFR-MAINT-001–006 and NFR-TEST-001–006, including a proposed 80% service-layer coverage target (NFR-TEST-005) explicitly flagged for PO/Scrum Master confirmation, not mandated unilaterally.
  - Accessibility: elevated the WCAG 2.1 AA baseline into NFR-A11Y-001–006 (keyboard navigation, label association, ARIA live regions, 4.5:1/3:1 contrast, screen-reader-accessible booking reference, focus order), noting the dedicated Phase 17 review sets its bar against these.
  - Compatibility: elevated §5.10 into NFR-COMPAT-001–003 with explicit browser/OS/Node.js version NFRs, including a proposed minimum-verified-browser-pair for MVP sign-off (flagged for confirmation).
  - On-Premise/Cloud Readiness: NFR-ONPREM-001–004 directly tie to DP-CLOUD-001–005 and DP-DEPLOY-001–006 as the NFR-level statement of the existing architecture-level constraint.
- No approved decision in requirements.md was reopened or contradicted (OQ-001–006, BR-001–013, pricing rules, booking reference format, and all DP-* constraints are elevated, not changed).
- No code, configuration, or infrastructure was implemented. No dependencies installed. No git commands run.
- Did not use WebFetch/WebSearch — OWASP Top 10 (2021) category names and NIST SP 800-207 were cited from the categories already referenced in requirements.md's own DP-ZEROTRUST section and from general knowledge of the well-established OWASP Top 10 category names; no verification fetch was judged necessary. If the security-reviewer (Phase 16) wants current OWASP category numbering re-confirmed against owasp.org at review time, that is within their remit.

---

## Artifacts Created or Updated

| Artifact | Path | Action |
|---|---|---|
| Non-Functional Requirements Specification | `docs/specs/non-functional-requirements.md` | Created (v1.0) |
| Handoff HO-004 | `docs/handoffs/04-solution-architect-to-sdlc-orchestrator-nfr-specification.md` | Created |
| Current Handoff | `docs/handoffs/current-handoff.md` | Replaced with fresh HO-004 pointer (HO-003 content preserved in its own numbered file, not deleted) |
| Handoff Index | `docs/handoffs/handoff-index.md` | Updated — HO-004 row added |
| Workflow State | `docs/handoffs/workflow-state.md` | Updated — Phase 04 marked complete pending PO confirmation; Phase 05 set as next |

---

## Decisions Made

| # | Decision | Rationale |
|---|---|---|
| 1 | Numeric percentile-based performance targets (p95/p50) were added on top of the already-approved "under 2s"/"under 1s" figures, rather than replacing them | The qualitative target is approved; percentile/sample-size definitions are new measurement detail needed for testability, so they are flagged for confirmation rather than presented as already approved |
| 2 | Security NFRs were split into "required now" and "must not preclude later" groups | Mirrors the task brief's explicit structure and keeps BR-010 (no auth) unambiguous — nothing in the "must not preclude" group implies auth is being implemented in MVP |
| 3 | A suggested 80% service-layer unit test coverage target (NFR-TEST-005) was proposed but explicitly marked "Should Have" and flagged for PO/Scrum Master confirmation | Requirements.md requires unit-testability (DP-017–020) but sets no percentage; proposing one without flagging it as new would misrepresent it as already-approved scope |
| 4 | A minimum-verified-browser-pair (Chrome + one of Edge/Firefox, Safari best-effort) was proposed for Phase 14 MVP sign-off | Exhaustive 4-browser × latest-2-version manual testing is disproportionate given the EOD 2026-07-03 deadline; flagged for confirmation rather than assumed |
| 5 | Backlog linkage deferred to Phase 07 for every NFR, using the literal phrase "To be linked at Phase 07 Backlog Creation" | Per task instruction — no backlog exists yet; inventing backlog IDs would create false traceability |
| 6 | OWASP Top 10 (2021) category names (A01, A03, A04, A05) were cited from general knowledge without a WebFetch verification call | High confidence in these well-established, unchanged category names; reserved WebFetch for cases requiring verification per task brief's optional guidance |

---

## Open Questions

None new. The following proposed numeric targets require explicit Human Product Owner / Scrum Master confirmation before being treated as enforced gates (full detail in `docs/specs/non-functional-requirements.md` Section 17):

| # | NFR ID | Proposed Target |
|---|---|---|
| 1 | NFR-PERF-001 | Search API: p95 < 2000 ms, p50 < 800 ms, over ≥20 sequential local requests |
| 2 | NFR-PERF-002 | Booking API: p95 < 1000 ms, p50 < 400 ms |
| 3 | NFR-PERF-003 | Frontend sort re-render < 100 ms for result sets up to 50 flights |
| 4 | NFR-PERF-004 | Airport dropdown population < 50 ms |
| 5 | NFR-PERF-005 | Aggregation overhead < 100 ms above slowest provider |
| 6 | NFR-TEST-005 | 80% line coverage target for backend service-layer classes |
| 7 | NFR-COMPAT-001 | Minimum verified browser pair for MVP sign-off (Chrome + Edge/Firefox; Safari best-effort) |

If the Human PO does not respond before implementation begins, these targets should be treated as non-binding guidance only (per their "If Not Confirmed" fallback stated in the spec document), not as blocking gates — the qualitative targets already approved in requirements.md remain in force regardless.

---

## Risks and Impediments

| # | Risk / Impediment | ID | Severity | Status |
|---|---|---|---|---|
| 1 | EOD 2026-07-03 deadline — compressed delivery timeline; a PO confirmation round-trip on 7 numeric targets adds review time | RISK-001 | Critical | Open — carry forward |
| 2 | Test execution blocked — cannot run npm/dotnet commands autonomously; this affects when NFR-PERF-*/NFR-TEST-005 can actually be measured, not just specified | RISK-004 / IMP-001 | High | Open — carry forward |
| 3 | Hiring challenge evaluation criteria not fully specified | RISK-005 | High | Open — carry forward |
| 4 | If the Human PO requests changes to the proposed numeric targets, a revision cycle to `docs/specs/non-functional-requirements.md` would be needed before Phase 05 finalizes its test strategy against these numbers | New — RISK-012 (suggested) | Low–Medium | Open — new, introduced by this phase; low risk because the document already treats unconfirmed targets as non-blocking guidance |

No Critical/High finding is newly introduced by this phase — this is a documentation-only deliverable with no code, dependency, or destructive action.

---

## Required Next Agent Action

The SDLC Orchestrator should:

1. Present `docs/specs/non-functional-requirements.md` — specifically Section 17 (Summary — Proposed Numeric Targets Requiring Confirmation) — to the Human Product Owner / Scrum Master for confirmation of the 7 proposed numeric targets.
2. This is a **recommended checkpoint, not a hard blocker**: per the document's own "If Not Confirmed" fallback column, the qualitative targets already approved in `docs/requirements.md` v1.4 remain valid regardless of PO response, so Phase 05 (Test Strategy) may proceed in parallel or immediately after, using the proposed numbers as draft guidance pending confirmation.
3. Commit and merge the Phase 04 branch (`sdlc/04-nfr-specification-skyroute-mvp`) to `main` per the phased-execution model, once the orchestrator confirms the working tree is clean and artifacts/handoffs are complete.
4. Create the Phase 05 branch and invoke `functional-tester` for Phase 05 — Test Strategy and Acceptance Planning, using both `docs/requirements.md` v1.4 and `docs/specs/non-functional-requirements.md` v1.0 as inputs (test scenarios should reference the NFR validation methods defined in Section 19 of the NFR spec).

---

## Completion Criteria for Next Step (Phase 05 — Test Strategy)

- `docs/specs/non-functional-requirements.md` exists, covers all 14 required NFR categories, and is traceable to requirements.md v1.4. **[DONE]**
- HO-004 handoff is complete. **[DONE]**
- Human PO has been presented with the 7 proposed numeric targets (confirmation may proceed in parallel with Phase 05, per the non-blocking fallback design).
- functional-tester produces a test strategy document informed by both `docs/requirements.md` and `docs/specs/non-functional-requirements.md`.

---

## Relevant Files

- `docs/specs/non-functional-requirements.md` — primary deliverable of Phase 04
- `docs/requirements.md` — v1.4, source baseline (unchanged, not modified by this phase)
- `docs/handoffs/04-solution-architect-to-sdlc-orchestrator-nfr-specification.md` — this file
- `docs/handoffs/current-handoff.md` — updated to point to HO-004
- `docs/handoffs/handoff-index.md` — updated with HO-004 row
- `docs/handoffs/workflow-state.md` — updated, Phase 04 marked complete pending PO confirmation, Phase 05 next
- `docs/handoffs/03-solution-architect-to-sdlc-orchestrator-requirements.md` — historical, unmodified
