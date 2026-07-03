# Risk Register — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
Author: Project Coordinator
Status: Active

---

## 1. Purpose

This register tracks delivery and technical risks for the SkyRoute Travel Platform MVP. Risks are assessed for probability and impact, assigned a severity, and assigned a mitigation strategy and owner.

The register is reviewed at each phase boundary and updated when new risks are identified or existing risk status changes.

---

## 2. Probability Scale

| Value | Meaning |
|---|---|
| Low | Unlikely to occur (less than 25% chance) |
| Medium | May occur (25–60% chance) |
| High | Likely to occur (more than 60% chance) |

---

## 3. Impact Scale

| Value | Meaning |
|---|---|
| Low | Minor inconvenience; no phase delay |
| Medium | Causes delay or rework but recoverable within sprint |
| High | Threatens sprint goal or delivery quality |
| Critical | Would prevent delivery or require Human PO scope decision |

---

## 4. Severity Matrix

| Probability \ Impact | Low | Medium | High | Critical |
|---|---|---|---|---|
| Low | Low | Low | Medium | High |
| Medium | Low | Medium | High | Critical |
| High | Medium | High | Critical | Critical |

---

## 5. Risk Status Values

| Status | Meaning |
|---|---|
| Open | Risk is active and being monitored |
| Mitigated | Mitigation actions applied; risk is reduced |
| Accepted | Risk accepted by owner (Low/Medium) or by Human PO (High/Critical) |
| Closed | Risk no longer applicable |
| Escalated | Risk escalated to Human Product Owner for decision |

---

## 6. Risk Register

| ID | Risk Description | Category | Probability | Impact | Severity | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|---|
| RISK-001 | EOD 2026-07-03 deadline cannot be met due to compressed delivery timeline — insufficient time to complete all SDLC phases with full quality gates | Timeline | High | Critical | Critical | Enforce strict phase sequencing with no scope creep; prioritise Must Have items; invoke parallel delivery plan; raise scope trade-off decision with Human PO immediately if delays emerge | project-coordinator | Open |
| RISK-002 | In-memory data store for flight data is lost on application restart — no persistence across sessions | Technical | High | Medium | High | Accepted by design: in-memory store is a deliberate MVP trade-off; document limitation in README and architecture docs; ensure evaluators are aware this is intentional | solution-architect | Open |
| RISK-003 | Flight provider integration is tightly coupled — adding a second flight provider requires significant backend rework | Technical | Medium | High | High | Design the provider layer with a provider abstraction (interface/strategy pattern) from the start; document extensibility in architecture; validate in code review | solution-architect | Open |
| RISK-004 | Test execution is blocked because npm install / dotnet test / dotnet restore cannot run autonomously — requires human approval | Process | High | High | Critical | Logged as IMP-001; test execution commands to be presented to Human PO at Phase 14; test execution summary will document "tests not run — pending approval" until resolved | functional-tester | Open |
| RISK-005 | Hiring challenge evaluation criteria are not fully specified — implemented features may not align with evaluator expectations | Scope | Medium | High | High | Implement the most visible, demonstrable capabilities: search, results, booking summary; document all trade-offs; ensure README clearly explains what was built and why | product-owner | Open |
| RISK-006 | Angular 17 standalone component model requires specific patterns — team agent may produce incompatible component declarations | Technical | Medium | Medium | Medium | Enforce standalone component pattern in architecture guidance; code reviewer to check for NgModule anti-patterns; lead engineer to use Angular 17 canonical examples | lead-full-stack-engineer | Open |
| RISK-007 | ASP.NET Core 8 minimal API vs controller pattern decision not finalised — inconsistent implementation if not decided early | Technical | Medium | Medium | Medium | Architecture planning phase (Phase 06) to make explicit decision and record in decision log; enforce pattern in feature specs | solution-architect | Open |
| RISK-008 | Multi-passenger booking support scope is ambiguous — incorrect assumption could cause significant rework | Scope | Low | High | Medium | Requirements analysis (Phase 03) to clarify and document multi-passenger scope; Human PO to approve before sprint planning | product-owner | Open |
| RISK-009 | No historical velocity baseline — sprint capacity estimate may be incorrect, leading to over-commitment | Delivery | High | Medium | High | Treat Phase 01–11 (planning phases) as the primary capacity consumer for the day; implementation phases are the core delivery block; use T-shirt sizing to right-size commitment | scrum-master | Open |
| RISK-010 | Review phases (15–18) may surface Critical/High findings that require significant fix effort, risking the merge deadline | Quality | Medium | High | High | Run reviews concurrently where possible (Code + Security can overlap; A11Y + Perf can overlap); address High findings promptly; Critical findings require immediate Human PO decision | project-coordinator | Open |
| RISK-011 | CORS configuration between Angular (port 4200) and ASP.NET Core (port 5000/5001) may not be configured correctly, blocking frontend-backend integration | Technical | Medium | Medium | Medium | DevOps/architecture phase to include CORS policy definition; lead engineer to configure CORS explicitly; functional tester to include CORS in integration test scope | lead-full-stack-engineer | Open |
| RISK-012 | Accessibility review may not be executable in full without a running browser environment — static review only | Process | Medium | Medium | Medium | Accessibility tester to perform static code review against WCAG 2.1 AA criteria and document findings; note limitation in review report | accessibility-tester | Open |
| RISK-013 | Single agent context window limitations may cause loss of context across phases if handoff files are not read correctly | Process | Low | High | Medium | Enforce strict handoff file protocol; all agents read current-handoff.md and workflow-state.md before starting; SDLC Orchestrator validates handoff completeness | sdlc-orchestrator | Open |

---

## 7. Risk Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial register created for Phase 02 |

---

## 8. Reference Documents

- `docs/delivery/dependency-register.md`
- `docs/delivery/impediment-log.md`
- `docs/handoffs/workflow-state.md`
- `docs/delivery/sdlc-operating-model.md`
- `.claude/rules/sdlc-rules.md`
