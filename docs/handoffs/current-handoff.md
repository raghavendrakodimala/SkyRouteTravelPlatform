# Current Handoff — HO-044 (rev 2 — AGE-LEAD-18 removed per DEC-022)

| Field | Value |
|---|---|
| Handoff ID | HO-044 |
| Date | 2026-07-08 |
| Branch | main (uncommitted working tree — orchestrator to branch/commit per git workflow) |
| Phase | Post-closure PO feature — passenger AGE capture (pure data capture, DEC-022) |
| From agent | senior-full-stack-engineer |
| To agent | sdlc-orchestrator |
| Status | Complete — implementation + tests green; awaiting orchestrator live verification and commit/merge |

## Work completed

**Rev 1 — PO feature (2026-07-08): capture passenger age.** Passenger details include `age` — required, whole number 0–120 — full-stack: request/domain/response contracts, `BookingRequestValidator` per-passenger checks on `passengers[i].age`, persistence, response echo (`fullName` + `age`; email/document data still never echoed), Angular form field between Full Name and Email (`type="number" inputmode="numeric" min=0 max=120`), numeric payload, "Age {n}" card line, `passengers[i].age` server-error mapping, draft/edit/structural-clear handling, `ageValidator` + `AGE_MIN`/`AGE_MAX` constants mirrored from the backend (DP-014/DP-015).

**Rev 2 — PO decision after requirements check (2026-07-08, DEC-022): AGE-LEAD-18 removed.** Age stays a PURE data-capture field — "no calculations and business logic should be bound to age for now" (the challenge PDF has no age requirements; age is a documented PO extension kept logic-free; IATA category/age-based pricing simulation explicitly deferred since it would contradict the PDF's fixed pricing rules). Removed from both layers, same day it was added:

- Backend: `LeadPassengerMinAge` constant + the `passengers[0].age` adult branch and its "Primary contact must be 18 or older." message deleted from `BookingRequestValidator`; contract/domain doc comments updated; validator tests replaced lead-17/lead-18 cases with `ValidateStructure_LeadPassengerFifteen_IsValid` (a lead aged 15 → zero errors) and position-independent boundary tests; integration comment updated.
- Frontend: `ageValidator()` lost its `isLead` parameter/branch and `LEAD_PASSENGER_MIN_AGE` was deleted; passenger-form-section lost the lead-only hint + its `aria-describedby` and the `leadAge` error branch (single range/required message remains); booking-form's age control is now one static validator (the index-0 switching, `isLeadActiveIndex()`, remove-time `updateValueAndValidity`, and phase-before-reset reorders were all removed — they existed only for AGE-LEAD-18); specs updated: lead-17-blocked replaced by out-of-range-121-blocked + "accepts a lead passenger aged 15" (submits `age: 15` as a number), lead-hint tests replaced by no-hint assertions, server-error mapping test now uses the range message.
- E2E: `booking-validation.spec.ts` lead-under-18 case replaced with an out-of-range (121) inline-error step; `fillActivePassengerForm(age default 34)` kept; helper comments de-AGE-LEAD-18'd. Playwright NOT run (per brief).
- Docs: `feature-booking-flow.md` → **v1.3** (§2.5 rewritten as pure 0–120 data capture with an explicit AGE-LEAD-18 removal note, §3 field table, §7 error table row removed, §9, closing line); `data-model.md` (Age column stays TINYINT CHECK 0–120; note now "no business rules bound to age", traceability/integrity/DDL rows updated); `README.md` AGE-LEAD-18 sentence replaced with the data-only wording; `docs/delivery/decision-log.md` **DEC-022** appended.

Note: the orchestrator's concurrent OpenAPI/Scalar/health-check additions to SkyRoute.Api (new csproj packages, Program.cs sections, `OpenApiDocumentTests.cs`, `HealthCheckTests.cs`) were left untouched and pass in the same run.

## Artifacts created or updated (cumulative, rev 1 + rev 2)

Backend: `src/Service/SkyRoute.Application/Contracts/PassengerRequest.cs`, `Contracts/PassengerNameResponse.cs`, `Domain/PassengerDetail.cs`, `Validation/BookingRequestValidator.cs`, `Services/BookingService.cs`; tests `Validation/BookingRequestValidatorTests.cs`, `Services/BookingServiceTests.cs`, `Persistence/InMemoryBookingStoreTests.cs`, `Controllers/BookingControllerTests.cs`.
Frontend: `shared/validators/document-number.validators.ts` (+spec), `shared/models/passenger-detail.model.ts`, `features/booking/passenger-form-section/passenger-form-section.component.ts/.html` (+spec), `features/booking/booking-form/booking-form.component.ts/.html` (+spec), `features/booking/booking.service.spec.ts`, `booking-state.service.spec.ts`, `features/confirmation/confirmation/confirmation.component.spec.ts`, `e2e/support/helpers.ts`, `e2e/booking-validation.spec.ts`.
Docs: `docs/features/feature-booking-flow.md` (v1.3), `docs/architecture/data-model.md`, `docs/delivery/decision-log.md` (DEC-022), `README.md`.

## Validation evidence (final, post-removal)

- `dotnet build SkyRoute.slnx` (redirected output — see impediment): **Build succeeded. 0 Warning(s), 0 Error(s).**
- `dotnet test SkyRoute.slnx --no-build` (redirected): **SkyRoute.Application.Tests 181/181 passed; SkyRoute.Api.IntegrationTests 24/24 passed (includes the new OpenAPI/health tests). Total backend 205/205, 0 failed.**
- `npm run build` (src/UI): **Application bundle generation complete.**
- `npm test -- --watch=false` (src/UI): **Test Files 17 passed (17); Tests 204 passed (204).**
- E2E specs + helpers type-checked clean with standalone `tsc --strict` (checked-in e2e tsconfig still cannot resolve `@types/node` here — pre-existing).
- Lead passenger aged 15 validates clean on both ends: backend `ValidateStructure_LeadPassengerFifteen_IsValid` (empty error dictionary) and frontend "accepts a lead passenger aged 15" (real DOM fill + submit → `submitBooking` called with `age: 15` as a number).

## Decisions made

- DEC-022 (PO): age = sanity-bounded (0–120) pure data capture; no business logic bound to age; AGE-LEAD-18 removed same day it was added; IATA category/pricing simulation explicitly deferred.
- `PassengerRequest.Age` stays `int?` so a missing JSON field remains a validator-addressed 400 rather than a silent 0.
- No persistent hint under the age field at all now (the inline error states the range); confirmation screen display unchanged (names only; age remains in the payload).

## Open questions

None blocking.

## Risks and impediments

- A running `SkyRoute.Api` dev-server process locks `src/Service/SkyRoute.Api/bin/`, so default-path solution builds fail on the output-copy step; per standing instruction the process was NOT killed and validation used redirected output paths (`-p:OutDir=...` + `--results-directory`). **The orchestrator must rebuild + restart the backend before live verification**, or the running API will still enforce/reject per the stale binaries.
- Rendered-UI verification in a live browser was not performed by this agent (no browser tooling in this session); the ui-ux-quality-gates rendered-evidence step remains for the orchestrator/reviewer walkthrough.

## Required next agent action

sdlc-orchestrator: restart the backend from fresh binaries, walk the booking flow live (age field renders between Full Name and Email with no hint; a lead aged 15 is accepted end-to-end; 121/blank rejected with the range message; ages echoed in the 201 payload), run the Playwright suite, then branch/commit/merge per the git workflow with user approval.

## Completion criteria for next step

Live walkthrough passes; Playwright suite green against rebuilt backend; changes committed and merged per phased workflow.

## Relevant files

- `src/Service/SkyRoute.Application/Validation/BookingRequestValidator.cs` — the only age rules (0–120, required)
- `src/UI/src/app/shared/validators/document-number.validators.ts` — `ageValidator()` + `AGE_MIN`/`AGE_MAX`
- `docs/features/feature-booking-flow.md` §2.5 — v1.3 pure-data-capture spec + removal note
- `docs/delivery/decision-log.md` — DEC-022
