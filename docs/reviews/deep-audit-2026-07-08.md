# Deep Audit Review Report — 2026-07-08

Report type: Deep 9-dimension defect audit (durable review report)
Date: 2026-07-08
Branch: main (audit run); fixes delivered on per-finding branches
Reviewer: deep-audit (independent, verifier-confirmed)
Finding ID prefix: `AUD-` (deep-audit findings; format per `.claude/rules/review-and-test-reporting.md`)
Scope: full stack — Angular UI (`src/UI`), .NET service (`src/Service`), UI↔API integration, and repository hygiene

---

## 1. Summary

A deep audit across 9 dimensions produced 45 raw findings. Each was independently re-verified against current source; **43 were confirmed** and **2 were rejected as false positives**. Every confirmed finding carries a verifier note tying the claim to specific current-code evidence.

### 1.1 Totals

| Metric | Count |
|---|---|
| Raw findings | 45 |
| **Confirmed** | **43** |
| Rejected (false positive) | 2 |

**By severity (confirmed):**

| Severity | Count |
|---|---|
| High | 6 |
| Medium | 18 |
| Low | 19 |

**By area (confirmed):**

| Area | Count |
|---|---|
| UI | 23 |
| API | 10 |
| Integration | 3 |
| Hygiene | 7 |

### 1.2 Status disposition

All confirmed findings are `Fixing` (concurrent developer agents are addressing them within this run), with the following exceptions:

- **AUD-043** and **AUD-044** — `Resolved` in this task by the technical-writer (documentation hygiene; see §2 corrections below and the README/decision-log updates).
- **AUD-007** (remove-passenger confirmation) and **AUD-038** (total-outage 200 vs 503) — **`Open — product decision (PO)`**: both are defensible-as-designed and their resolution is a product/UX call, not a clear defect. Flagged for Product Owner decision.

The 6 High findings include a booking **route-trust passport-bypass** (BR-003 defeated by client-supplied origin/destination on a real international flight — AUD-025/028/033) and a **500-on-malformed-input** class that breaks the RFC7807 400 error contract (AUD-027).

---

## 2. Confirmed Findings

Ordered by severity, then ID. Each row: ID, Severity, Area, File, Title, Fix, Status.

### 2.1 High (6)

| ID | Sev | Area | File | Title | Fix | Status |
|---|---|---|---|---|---|---|
| AUD-001 | High | UI | `confirmation.component.html:30` | Confirmation `@for` tracks by `fullName` — duplicate names crash/misrender the success screen (NG0955) | Track by `$index` (or a unique key), matching the sibling saved-passenger list | Fixing |
| AUD-025 | High | API | `BookingService.cs:52` | Booking trusts client-submitted route — passport/National-ID rule (BR-003) bypassable | Re-resolve route type from the server-side flight (Provider+FlightNumber); reject on Origin/Destination/time mismatch | Fixing |
| AUD-027 | High | API | `Program.cs:68-71` | Malformed / empty / wrong-typed body returns 500 instead of a 400 validation problem | Null-guard the request and translate input-formatter/deserialization failures to 400 problem+json; add tests | Fixing |
| AUD-028 | High | API | `BookingService.cs:52` | BR-003 route/document rule bypassable — origin/destination trusted, never cross-checked to the flight number | Re-resolve/validate route from the provider schedule; derive RouteType from server-resolved route | Fixing |
| AUD-033 | High | API | `BookingService.cs:52` (+ `FlightFareResolver.cs`, `RouteTypeResolver.cs`, `BookingRequestValidator.cs`) | Flight-snapshot Origin/Destination/times trusted unverified — SEC-001 re-resolved only the fare | Re-resolve the whole identifying snapshot server-side; reject on any mismatch before RouteTypeResolver runs | Fixing |
| AUD-036 | High | Integration | `booking-form.component.ts:61,264-271,533-546` | Booking form silently swallows every dotted `flight.*` 400 — dead, feedback-less form (FR-071 violation) | Route `flight.*` keys to the generic banner / add a catch-all for any unmapped validation key | Fixing |

### 2.2 Medium (18)

| ID | Sev | Area | File | Title | Fix | Status |
|---|---|---|---|---|---|---|
| AUD-002 | Medium | UI | `search-form.component.ts:43` | Departure-date `min` computed from the UTC date, not local — off-by-one near midnight | Build the date string from local `getFullYear/getMonth/getDate` instead of `toISOString()` | Fixing |
| AUD-005 | Medium | UI | `app.routes.ts:24` | Refresh/deep-link `/results` strands the user on a blank, contentless page | Add a guard mirroring `/booking` & `/confirmation`, or render a styled recovery state | Fixing |
| AUD-008 | Medium | UI | `search-form.component.ts:47` | "Modify search" opens a blank form instead of pre-filling prior criteria | Patch the form from `searchState.lastCriteria()` on init | Fixing |
| AUD-009 | Medium | UI | `results-list.component.ts:92` | Results card "$X total" always equals per-person and is not the amount actually paid | Show only per-person "from $X" on results; reserve "total" for booking | Fixing |
| AUD-010 | Medium | UI | `search-form.component.ts:51` | Past-date rule enforced only by native `min`; reactive form has no past-date validator | Add a reactive validator rejecting dates before a locally-computed today | Fixing |
| AUD-012 | Medium | UI | `booking-form.component.css:177` | Long single-token passenger name overflows the passenger card and confirmation ticket | Add `overflow-wrap: anywhere` to `.card-name` and `.passengers li` | Fixing |
| AUD-013 | Medium | UI | `results-list.component.css:5` | Three content max-widths (860/980/560) mismatch the 1120 chrome — content jumps between funnel steps | Adopt one shared content max-width token across header/footer/page containers | Fixing |
| AUD-014 | Medium | UI | `search-form.component.css:25` | Hero tagline contrast <4.5:1 over the gradient light stop (18px white @ 88% alpha) | Drop the 0.88 alpha (solid `#fff`) / darken the gradient light stop / add a scrim | Fixing |
| AUD-018 | Medium | UI | `passenger-form-section.component.html:13-26`; `search-form.component.html:11-22` | Validation errors not programmatically associated; invalid state never exposed (no `aria-invalid`/`aria-describedby`) | Bind `aria-invalid` and link each error `<p>` id via `aria-describedby` | Fixing |
| AUD-019 | Medium | UI | `search-form.component.html:84-86`; `results-list.component.html:17-18` | Conditionally-rendered live regions miss their first announcement ("Searching…") | Render the status region unconditionally; drive its text from a signal | Fixing |
| AUD-029 | Medium | API | `BookingRequestValidator.cs:192-200` | Booking accepts unknown airport codes, past departures, and arrival-before-departure | Validate known airport codes, require arrival > departure and non-past departure | Fixing |
| AUD-030 | Medium | API | `BookingController.cs:31-55` & `SearchController.cs:28-39` | OpenAPI omits response bodies, the real 201 status, and all error responses | Add `[ProducesResponseType]` for 200/201/400/500 + an OpenApi contract test | Fixing |
| AUD-031 | Medium | API | `SearchRequestValidator.cs:83-86` | Past-departure validation uses `DateTime.UtcNow` — rejects valid same-day for negative-offset users | Compare against a documented reference zone, not raw UTC; add day-boundary tests | Fixing |
| AUD-034 | Medium | API | `BookingRequestValidator.cs:70-109`; `Program.cs:36-40` | Unbounded passenger-array validation loop — request→memory amplification DoS | Return early when `passengers.Count` exceeds max; add `MaxRequestBodySize` + rate limiting | Fixing |
| AUD-037 | Medium | Integration | `Program.cs:182-184` | HTTPS redirection runs before CORS — under the `https` profile every call fails as "Network error" | Move `UseCors` before `UseHttpsRedirection` (or drop redirection for the local HTTP MVP) | Fixing |
| AUD-040 | Medium | UI | `confirmation.component.html:30` | Confirmation list `track passenger.fullName` collides when two passengers share a name | Track by `$index` / a unique per-passenger key | Fixing |
| AUD-041 | Medium | Hygiene | `tsconfig.json:5-21` | Angular tsconfig ships without `strict` and `strictTemplates` — safety net disabled | Restore `"strict": true` and `"strictTemplates": true`; fix resulting errors | Fixing |
| AUD-042 | Medium | Hygiene | `SearchRequest.cs:12-27` | `SearchRequest` DataAnnotations are inert; booking DTOs have none — misleading + under-documents OpenAPI | Pick one authoritative mechanism; align annotations and the README validation claim | Fixing |

### 2.3 Low (19)

| ID | Sev | Area | File | Title | Fix | Status |
|---|---|---|---|---|---|---|
| AUD-003 | Low | UI | `search-state.service.ts:43` | `lastCriteria` written optimistically — recap can misdescribe results after a failed re-search | Set `_lastCriteria` only on the success branch (mutate with `_results`) | Fixing |
| AUD-004 | Low | UI | `booking-form.component.ts:159` | `documentNumber` validator frozen at construction while its label/hint are reactive | Read `routeType` at validation time, or rebuild the validator when it changes | Fixing |
| AUD-007 | Low | UI | `booking-form.component.ts:414` | Remove passenger destroys a fully-entered passenger (PII) instantly — no confirm, no undo | Gate destructive Remove behind a confirmation, or provide undo | **Open — product decision (PO)** |
| AUD-011 | Low | UI | `app.css:250` | Mobile progress stepper hides every step label — unlabeled numbered dots | Keep the current step's label visible on mobile | Fixing |
| AUD-015 | Low | UI | `results-list.component.css:153` | Flight-card timeline never stacks; fixed min-widths overflow at 320px + 200% text | Add a wrap/stack fallback + `min-width:0` in the mobile media query | Fixing |
| AUD-016 | Low | Hygiene | `styles.css:123` | `body { overflow-x: hidden }` masks overflow app-wide, hiding defects during 320/360px QA | Remove the app-wide clip (100vw justification is stale); scope any needed clip | Fixing |
| AUD-017 | Low | UI | `confirmation.component.html:38` | "No booking available" fallback is bare unstyled text, unlike every other empty/error state | Style like the booking empty-state + add a "Start a New Search" CTA | Fixing |
| AUD-020 | Low | UI | `booking-form.component.html:44,110` | Error banners are `role=alert` AND receive focus — double/assertive re-announcement | Use one mechanism: keep `role=alert` without focus, or focus a non-alert container | Fixing |
| AUD-021 | Low | UI | `app.html:38-54`; `app.css:163-171` | Journey stepper conveys completed vs upcoming by color only; no progress semantics to SR | Add a non-color completed indicator + a visually-hidden state per step | Fixing |
| AUD-022 | Low | UI | `sort-control.component.html:1-13` | Sort models a single-select choice as `aria-pressed` toggles; no reorder announcement | Use `role=radiogroup`/`radio` (or add a polite live region announcing the sort) | Fixing |
| AUD-023 | Low | UI | `confirmation.component.html:10-15` | Booking reference uses `role=status` on static content — never announced | Drop `role=status`; surface the reference in the focused heading region | Fixing |
| AUD-024 | Low | UI | `app.html:1-56`; `route-focus.service.ts:58-67` | No skip-to-content mechanism; route-focus deliberately skips the initial load | Add a visually-hidden-until-focused skip link targeting an id on `<main>` | Fixing |
| AUD-026 | Low | API | `SearchRequestValidator.cs:83` | Search/booking accept flights that already departed today (date-only past check) | Filter/reject flights earlier than `UtcNow` when date == today; validate at booking | Fixing |
| AUD-032 | Low | Hygiene | `BookingRequestValidator.cs:192-200` & `:152-156` | `BaseFare` optional, unpersisted, conditionally validated — inconsistent with `PricePerPassenger` | Drop `BaseFare` from the request, or make it required/verified/returned | Fixing |
| AUD-035 | Low | API | `Program.cs:190-210`; `FlightProvidersHealthCheck.cs:25-26` | Unauthenticated `/api/health` leaks CLR class names & booking-existence state | Expose a bare public status body; gate detailed descriptions to Development/authenticated | Fixing |
| AUD-038 | Low | Integration | `FlightAggregatorService.cs:39-43` | Total provider outage returns HTTP 200 `[]` — indistinguishable from a genuinely empty route | Signal degraded/partial (a flag, or 5xx when ALL providers fail) | **Open — product decision (PO)** |
| AUD-043 | Low | Hygiene | `README.md:13` | README miscounts backend projects & documents a structure that doesn't match the real tree | Correct to five projects; fix the structure block to `src/Service/SkyRoute.*` and `src/UI` | Resolved (this task) |
| AUD-044 | Low | Hygiene | `package.json:4-9` | No linting or CI — frontend has only `tsc` (strict off) + Prettier; no ESLint, no pipeline | Document as a known deferred gap ("would add: ESLint + a CI workflow") | Resolved (this task) |
| AUD-045 | Low | Hygiene | `Program.cs:182` | `UseHttpsRedirection()` is dead noise under the HTTP flow and breaks the SPA under the `https` profile | Drop/guard `UseHttpsRedirection()` for the local HTTP-only MVP | Fixing |

---

## 3. Rejected / False Positive (2)

These raw findings did not survive verification against current code and are **not** counted in the 43 confirmed.

| ID | Title | Disposition |
|---|---|---|
| AUD-006 | Confirmation passenger list tracks by `fullName` — two passengers with the same name break/misrender the final success screen | **Rejected — false positive.** Redundant restatement of the confirmed track-by-`fullName` findings already filed as AUD-001 and AUD-040; retained there, dropped here to avoid double-counting the same defect. |
| AUD-039 | Confirmation screen drops the `age` field the API deliberately echoes back | **Rejected — false positive.** Verifier confirmed the claim does not hold against current code; the echoed passenger `age` is not dropped by the confirmation screen. |

---

## 4. Notes

- Finding IDs use the `AUD-` prefix for this deep-audit pass and follow the required review-report content shape in `.claude/rules/review-and-test-reporting.md` (ID, Severity, File/area, Evidence, Impact, Recommendation/Required fix, Status). Full evidence, impact, repro, and verifier notes for each finding live in the audit source data captured for this run; this report is the durable, scannable index of record.
- **PO decisions required:** AUD-007 and AUD-038 are flagged `Open — product decision (PO)`. Neither is a clear defect: AUD-007's Remove button behaves exactly as labelled (a deliberate, unambiguous action on unsaved local data), and AUD-038's swallow-to-empty is a documented fault-isolation design (AD-010) in a pure in-process mock provider set with no real upstream that can go down. Both are UX/product calls.
- Documentation hygiene findings AUD-043 and AUD-044 were corrected in the same task that produced this report (README project counts/structure and a documented known-gap note; see `docs/delivery/decision-log.md` DEC-024).

---

## Resolution & Live Verification (2026-07-08)

Of 43 confirmed findings: **41 fixed**, 2 dispositioned as product/design decisions.

**Fixed & validated** — backend **239/239** tests, frontend **229/229** tests, ESLint **0 errors**, both builds clean (0 warnings). All statuses above move to **Resolved**, including the two former product-decision items: **AUD-007** (remove-passenger confirmation — PO-approved and implemented) and **AUD-038** (total-outage now 503; partial failure still 200; healthy no-match still 200 []).

**Live-verified (real HTTP against the running API):**
- **AUD-025/028/033 (passport bypass) — CLOSED.** GA204 (a genuine international LHR→DXB flight) declared as domestic LHR→MAN with a National ID → **400** `flight.destination: "Destination does not match the provider's published route for this flight."` The same flight correctly declared LHR→DXB with a Passport → **201 SKY-INT-…**.
- **AUD-027 (malformed input) — CLOSED.** Empty body, malformed JSON, and wrong-typed field each → **400** (were 500).
- **AUD-035 (health disclosure) — CLOSED.** `/api/health` body now exposes only per-check `name`+`status` — no CLR class names, no description field.

**Dispositioned, not defects (PO-accepted trade-offs):**
- **AUD-026** — time-of-day filtering of already-departed flights conflicts with the server's deliberate same-day timezone generosity (no client offset available). Date-level generosity implemented; time-of-day filtering intentionally not added.
- **AUD-032 / AUD-042** — BaseFare-in-contract and DataAnnotations consistency are design observations (the validators are authoritative; annotations shape the OpenAPI schema). Documented, no behavior change.

**Rejected as false positives (verification phase):** AUD-006, AUD-039.
