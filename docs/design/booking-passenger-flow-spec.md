# Booking Passenger Flow — Final Interaction Specification

| Field | Value |
|---|---|
| Document ID | DESIGN-FLOW-001 |
| Date | 2026-07-07 |
| Author | ux-ui-designer |
| Branch | fix/requirements-compliance-gaps-skyroute-mvp |
| Status | Implemented (2026-07-07) — subsequently amended in part by a Product Owner UX correction later the same day: the booking screen now uses a **single in-place passenger form** with two persistent actions ("Add another passenger" / "Confirm Booking"; "Save changes" / "Cancel edit" while editing) instead of Part B's save → prompt → review wizard (no prompt step, no separate review step, no progress stepper), and Part A's restored passenger select was later removed from the search form entirely — passenger count is captured during booking and `SearchRequest.passengerCount` is always `1`. Part B's cross-cutting mandates (focus choreography, persistent live region, aria-disabled-never-disabled controls, saved-card Edit/Remove, parked-draft protection, `passengers[i].*` server-error repair loop, B.11 leave guards, 9-passenger cap) remain implemented in the amended flow. See `frontend/src/app/features/booking/booking-form/booking-form.component.ts` and `docs/features/feature-booking-flow.md` v1.1 Section 2 for the implemented behavior. |
| Supersedes | DESIGN-IXD-002 (winning "Progressive List" interaction design), amended per orchestrator MUST-INCLUDE list |
| Depends on | `docs/design/visual-design-spec.md` §1 tokens (referenced by name only); `docs/features/feature-booking-flow.md` field rules (unchanged); backend contract unchanged |
| Drives | `frontend/src/app/features/booking/booking-form/booking-form.component.{ts,html,css}`; `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.{ts,html}` (one additive change, §B.7); `frontend/src/app/features/search/search-form/search-form.component.{ts,html,css}` |

## Amendments incorporated over DESIGN-IXD-002

| # | Amendment | Section |
|---|---|---|
| 1 | Saved-card document number shown **unmasked** (masking dropped) | B.3 |
| 2 | Stepper progress indicator seeded from searched passenger count | B.0, B.10 |
| 3 | Discriminated-union `Phase` type replaces `stage` + `editIndex` + `stageBeforeEdit` | B.7 |
| 4 | Edit-over-dirty-edit is blocked (aria-disabled), never silently discarded | B.1, B.3, B.4 |
| 5 | Submit-time lockdown: all mutating controls aria-disabled during POST | B.1, B.4 |
| 6 | Corrections-complete announcement after last server-flagged passenger is repaired | B.2, B.6 |
| 7 | Navigation guard (canDeactivate + beforeunload) against multi-passenger data loss | B.11 |
| 8 | `autocomplete="name"` / `autocomplete="email"` for the primary contact (WCAG 1.3.5) | B.7 |

---

## Part A — Search Form: Restored Passenger Select

### A.1 Behavior

Replace the entire "Add another passenger?" yes/no counter block (`search-form.component.html`, currently lines 51–73) with one native select. Delete from `search-form.component.ts`: `passengerCount` signal, `awaitingPassengerResponse`, `canAddPassenger`, `maxPassengersReached`, `passengerCountAnnouncement`, `continuePassengerCountLabel`, `addPassenger()`, `declinePassengerPrompt()`, and `describePassengerCount()`. The existing `form.controls.passengerCount` (`FormControl<number>`, default 1) is the single source of truth again; `onSubmit()` needs no change.

### A.2 Markup

```html
<div class="field">
  <label for="passengerCount">Passengers <span class="required-indicator">(required)</span></label>
  <select id="passengerCount" formControlName="passengerCount" required aria-required="true">
    @for (n of passengerOptions; track n) {
      <option [ngValue]="n">{{ n }}</option>
    }
  </select>
  @if (fieldError('passengerCount')) {
    <p class="error" role="alert">{{ fieldError('passengerCount') }}</p>
  }
</div>
```

- `protected readonly passengerOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9];`
- Use `[ngValue]` (not `[value]`) so the control keeps its `number` type — with `[value]` the select control value accessor would write strings into a `FormControl<number>`.
- Option text is the bare numeral ("1" … "9"); the label "Passengers" provides context. Default selected: 1.
- No live region, no count announcement — a native select announces its own value change to screen readers; adding aria-live here would double-announce.
- Delete the now-orphaned CSS (`.passenger-count-field`, `.passenger-count-display`, `.passenger-prompt`, `.passenger-prompt-actions`, `.passenger-max-note`) from `search-form.component.css`.
- Visual treatment: exactly visual-design-spec §5.1 (`--color-border-control` border, `--radius-sm`, `--space-2 --space-3` padding, brand-blue focus glow). No `appearance: none`.

Accessibility expectations: label programmatically associated via `for`/`id`; keyboard-operable natively; value change announced natively; error message `role="alert"` adjacent to the field.

---

## Part B — Booking Screen: Progressive-List Passenger Wizard

### B.0 Page anatomy (top to bottom, always this order, one column, `.booking-page` max-width unchanged)

1. `h1` "Booking Details"
2. Flight summary card (unchanged content, visual-design-spec §4.2 card recipe)
3. Price breakdown card — now **live** (B.5), containing the searched-vs-actual note when applicable
4. `h2` "Passengers"
5. **Progress stepper** — non-interactive `<ol>` of pills seeded from the searched passenger count, plus a terminal "Review" pill (full spec: B.10)
6. **Saved-passenger list** — `<ul class="passenger-list" aria-label="Saved passengers">`, one `<li>` card per saved passenger (B.3)
7. **The slot** — exactly one of: the active passenger form (new or edit), the add-another prompt, or the review block. The slot is where the page "grows"; nothing above it ever disappears. (Exception: an edit form renders in place of card K within the list, per S4.)
8. A single **persistent** visually-hidden live region: `<p class="visually-hidden" role="status" aria-live="polite">{{ liveMessage() }}</p>` — rendered unconditionally (never inside an `@if`; conditionally-rendered live regions miss their first announcement).

### B.1 State machine

States (component-local; `BookingStateService` unchanged). The S-labels below are documentation labels; the implementation encodes them in the `Phase` union of B.7:

| ID | State | Slot contents |
|---|---|---|
| S0 | `no-flight` | Empty state only (no wizard) |
| S1 | `collecting` | Active form for new passenger N = saved.length + 1 |
| S2 | `prompt` | "Add another passenger?" prompt block |
| S3 | `review` | Review block: count, mismatch note, "Add another passenger" (if < 9) or cap note, Confirm Booking, banners |
| S4 | `editing(K, returnTo)` | Active form pre-filled with saved passenger K; card K is visually replaced in place by the form (the form renders at card K's position in the list, not at the bottom) |
| S5 | `submitting` | Review block with Confirm in in-flight state (`aria-disabled`); **full mutation lockdown** (below) |
| S6 | `validation-error` | Error summary banner + S4 for the first offending passenger; other offending cards badged "Needs correction" |
| S7 | `generic-error` | Review block + `.banner-error` |
| S8 | `confirmed` | Review block collapsed to the existing "already been confirmed" `.banner-info`; Confirm `aria-disabled` |

Transitions:

| From | Trigger | To | Side effects |
|---|---|---|---|
| (load, flight present, 0 saved) | — | S1 (passenger 1) | If searched count > 1, show intro note (B.2) |
| (load, no flight) | — | S0 | |
| S1 | Save passenger (valid), saved becomes < 9 | S2 | Card appended; per-passenger server errors for that index cleared |
| S1 | Save passenger (valid), saved becomes 9 | S3 | Cap note shown; "Add another passenger" hidden |
| S1 | Save passenger (invalid) | S1 | `markAllAsTouched`; focus first invalid field |
| S1 (N ≥ 2) | Cancel | S3 | Draft discarded |
| S2 | "Yes, add another" | S1 (next N) | Fresh empty form |
| S2 | "No, continue to review" | S3 | |
| S3 | "Add another passenger" (< 9 only) | S1 | |
| S3 | Confirm Booking | S5 | Guards: not loading, not confirmed, saved ≥ 1; lockdown engages |
| S5 | 201 | — | Set success flag (disarms B.11 guard), navigate `/confirmation` (existing) |
| S5 | 400 with `passengers[i].*` keys | S6 | Copy fieldErrors locally; open S4 (`returnTo: 'review'`) for smallest offending i; lockdown releases |
| S5 | 400 flight/passengerCount keys, or network/5xx | S7 | Lockdown releases |
| S4 / S6-edit | Save changes (valid) | *returnTo* (S2 or S3; S6 → next flagged edit per B.6 — `returnTo` carried through the chain — and, when repairs finish, S3 or, if `returnTo` is `'collecting'`, back to S1 restoring the parked draft per B.1 (CR-002)) | Card K updated; server errors for index K cleared |
| S4 | Cancel | *returnTo* | Form values discarded |
| any with cards (not S5) | Remove card K, ≥ 1 card remains | same stage | Renumber; clear ALL indexed server errors (stale indices); if an edit was open on index J > K, its index decrements |
| S2/S3 | Remove last remaining card | S1 (passenger 1) | Wizard restarts; min-1 rule enforced by construction, Remove is never `disabled` |
| S8 | (return via back-nav after confirm) | S8 | Read-only |

**Single-active-form invariant and edit-entry rules:**

- **Edit while S1 form open and dirty:** park the draft (`parkedDraft = activeForm.getRawValue()`), open S4 with `returnTo: 'collecting'`. On save/cancel of the edit, restore the parked draft into a reopened S1 form (same passenger number, recomputed) and announce "Returning to passenger {N} details."
- **Edit while S1 form open and pristine:** discard silently; open S4 with `returnTo: 'prompt'` — equivalent outcome, no data lost.
- **Edit while S2 prompt showing:** prompt closes, S4 opens with `returnTo: 'prompt'`.
- **Edit while another edit is open and PRISTINE:** the open edit is cancelled silently (nothing was changed, nothing is lost) and the new edit opens, carrying over the original `returnTo`.
- **Edit while another edit is open and DIRTY — blocked, never silently discarded (Amendment 4):** while an edit form is dirty, every other card's `Edit` button is set `aria-disabled="true"` with a click guard (no-op) and its accessible name gains the visually-hidden suffix `(finish the current passenger first)` — e.g. `Edit passenger 3, Priya Sharma (finish the current passenger first)`. The buttons stay in the tab order (never `disabled`), so focus never drops and screen-reader users hear *why* the action is unavailable. Dirtiness is tracked reactively (form `valueChanges`/`dirty` mirrored into a signal) so the buttons re-enable the moment the edit is saved or cancelled.

**Submit-time lockdown (S5, Amendment 5):** while the booking POST is in flight, **every mutating control** — each card's `Edit` and `Remove`, and `Add another passenger` (if visible) — is `aria-disabled="true"` with a click guard, exactly like the Confirm button. None use the `disabled` attribute (focus must survive, B.4). This guarantees `savedPassengers()` cannot diverge from the payload the server is validating, so the `passengers[i]` index mapping of B.6 stays trustworthy. Lockdown releases on any non-201 outcome.

### B.2 Microcopy (exact strings)

Headings and structure:
- Page: `Booking Details` (h1) — unchanged.
- Passenger section: `Passengers` (h2).
- Form legend (reused component): `Passenger {N}` + `(Primary Contact)` badge when N = 1 — unchanged.
- Review block heading: `Review and confirm` (h2, `tabindex="-1"`).

Notes:
- Intro note (S1, passenger 1, searched count > 1): `Your search included {S} passengers. Add each passenger's details below, one at a time.`
- Searched-vs-actual note (shown inside the price-breakdown card whenever liveCount ≠ searched count): `Note: your search was for {S} passenger{s}; the total shown is for the {C} passenger{s} in this booking.` Styled subtle (B.8), not a warning banner. Repeated in the review block for final-check visibility.
- Cap note (S3, saved = 9): `You've reached the maximum of 9 passengers for one booking.`
- Review summary line: `{C} passenger{s} · {formatTotalAndPerPersonLabel(perPerson, C)}` (reuses `pricing.util.ts` verbatim format, e.g. "2 passengers · USD 575.00 total / USD 287.50 per person").

Buttons:
- Save on a new form: `Save passenger`
- Save on an edit form: `Save changes`
- Cancel (edit form, and new form for N ≥ 2): `Cancel`
- Prompt: question text `Add another passenger?`; buttons `Yes, add another` / `No, continue to review`
- Review add: `Add another passenger`
- Submit: `Confirm Booking` (in flight: `Confirming…`) — unchanged, requirement wording.
- Card actions: visible text `Edit` and `Remove`; accessible names `Edit passenger {N}, {fullName}` / `Remove passenger {N}, {fullName}` via `aria-label`. While another edit is dirty, Edit names gain the suffix `(finish the current passenger first)` (B.1).
- Empty state (S0): `No flight selected. Please start a new search.` + button `Start a New Search` (routes to `/search`; same label as the confirmation screen's existing button).

Field labels/inline errors: unchanged from `passenger-form-section.component.html` (Full Name / Email Address / `documentLabel()` with `(required)` indicators and the existing error strings).

Error summary banner (S6, `role="alert"`, `tabindex="-1"`):
- Lead line: `We couldn't confirm your booking. Please correct the details below.`
- One line per offending passenger, using the backend's own message text: `Passenger {i+1}: {message}` (e.g. "Passenger 2: A valid email address is required.").

Card error badge (S6, on offending cards not currently open): `Needs correction`.

Already-confirmed banner: existing text unchanged: `This booking has already been confirmed. Start a new search to book again.`

Navigation-guard confirm (B.11, native `confirm()`): `Leave this page? Passenger details you've entered will be lost.`

Live-region announcements (exact, via `liveMessage` signal; `{total}` = `formatUsd(...)`):
- Save (new, < 9): `Passenger {N} saved. Total for {C} passengers: {total}.`
- Save (9th): `Passenger 9 saved. Maximum of 9 passengers reached. Review your booking.`
- Yes → new form: `Enter details for passenger {N}.`
- No → review: `Review your booking. {C} passenger{s} added.`
- Edit opened: `Editing passenger {N}.`
- Edit saved: `Passenger {N} updated.`
- Edit cancelled: `Edit cancelled.`
- New-form cancel: `Passenger not added.`
- Remove (≥ 1 remains): `Passenger {N} removed. Remaining passengers renumbered. Total for {C} passenger{s}: {total}.`
- Remove (last): `Passenger removed. Add at least one passenger to continue.`
- Returning from parked draft: `Returning to passenger {N} details.`
- Corrections complete (Amendment 6 — after the last server-flagged passenger is saved and the wizard returns to review): `All corrections saved. Review and confirm your booking.`
- Submitting: `Submitting your booking…` (existing string, now via the same persistent region).

Identical consecutive messages must still re-announce: append a zero-width variation or clear-then-set via microtask (implementation note).

Stepper visually-hidden per-pill text (B.10): `Passenger {N}: completed` / `Passenger {N}: in progress` / `Passenger {N}: not started`; Review pill: `Review: in progress` / `Review: not started` / `Review: completed`.

### B.3 Saved-passenger card

Content (in order):
1. `Passenger {N}` eyebrow (position-derived — renumbering is automatic because it is rendered from array index) + `(Primary Contact)` badge for index 0.
2. Full name (h3, `--font-weight-bold`).
3. Email — **unmasked**.
4. Document — `{documentLabel}: {documentNumber}`, **unmasked** (Amendment 1). Rationale: the value is user-typed in this session, never persisted client-side, and never echoed back by the server; it is the field with the strictest format rules and the highest typo cost, and the card's entire purpose under the locked error-correction mandate is at-a-glance verification — masking would force an Edit round-trip to check the one field most likely to be wrong. No Show/Hide toggle is needed once the value is unmasked.
5. `Needs correction` badge (S6 only).

Actions: `Edit` and `Remove` buttons, right-aligned in a row on wide viewports, stacked under the details below ~480px. Never `disabled`; `aria-disabled` states exist in exactly two situations — another edit form is dirty (Edit only, B.1) and submit-in-flight lockdown (both, B.1/S5). Remove of the last card restarts the wizard rather than being disabled — disabled buttons are a keyboard/SR dead end and the restart transition is self-explanatory.

Renumbering: purely positional. Removing passenger 2 of 3 makes the former passenger 3 render as "Passenger 2" everywhere (card eyebrow, aria-labels; the live announcement covers it: "Remaining passengers renumbered.").

### B.4 Focus choreography (complete; focus must never land on `<body>`)

Rule: any interaction that removes the focused element from the DOM sets an explicit focus target in the same action, applied after render. Non-interactive targets carry `tabindex="-1"`.

| Trigger | What leaves the DOM | Focus lands on |
|---|---|---|
| Save passenger (< 9) | Active form incl. its Save button | `Yes, add another` button |
| Save 9th passenger | Active form; prompt skipped | Review heading `Review and confirm` (`tabindex="-1"`) |
| `Yes, add another` | Prompt block | Full Name input of the new form |
| `No, continue to review` | Prompt block | Review heading |
| `Add another passenger` (review) | Review block | Full Name input of the new form |
| Cancel (new form, N ≥ 2) | Active form | `Add another passenger` button in review; if at cap (impossible in S1, defensive) the review heading |
| `Edit` on card K | Card K's content (replaced in place by form) | Full Name input of the edit form |
| Save changes / Cancel (edit) | Edit form (card K returns) | `Edit` button of card K |
| Edit resolved with parked draft | Edit form | Full Name input of the restored new form |
| `Remove` card K, cards remain | Card K | `Remove` button of the card now at position K; if K was last, the new last card's `Remove` |
| `Remove` last card (no form open) | Card + review/prompt block | Full Name input of the freshly opened passenger-1 form |
| `Remove` card while a form is open | Card K only | Next card's `Remove`; if no cards remain, the open form's Full Name input |
| Save with invalid form | Nothing | First invalid control (`markAllAsTouched` first) |
| Click on any `aria-disabled` control (locked Edit, lockdown, Confirm guards) | Nothing (click no-ops) | Stays where it is — control remains focusable by design |
| Confirm Booking → submitting | **Nothing** — see below | Stays on Confirm Booking |
| 400 validation | Review block collapses; edit form opens | Error summary banner (`role="alert"`, `tabindex="-1"`) — user tabs from the summary into the reopened form directly below it |
| Last flagged passenger repaired → review | Edit form (card returns; banner removed) | Review heading (announcement: corrections-complete string, B.2) |
| Generic error | Nothing (banner added) | `.banner-error` (`tabindex="-1"`) |
| Navigation guard: user cancels the confirm | Nothing | Stays on the element that triggered navigation |
| 201 success | Whole page (route change) | Confirmation page (existing behavior; out of this design's scope) |

Critical detail — the Confirm button: the current implementation binds `[disabled]="loading() || …"`, which would drop focus to `<body>` the instant the focused button becomes disabled. Change to `aria-disabled` semantics during in-flight/confirmed states: keep the button focusable, set `[attr.aria-disabled]="loading() || alreadyConfirmed()"`, and guard in the click handler (the guard already exists in `onSubmit`/`submitBooking`). Because Confirm only renders in review — where every saved passenger passed validation at save time — the old `!allPassengersValid` disable condition disappears entirely; validity gating happens per-passenger at `Save passenger` instead (a direct improvement: the user is never staring at a mysteriously disabled button 5 passengers away from the problem). The same `aria-disabled` + click-guard pattern applies to every lockdown/edit-blocked control (B.1).

aria-live usage: exactly one polite `role="status"` region, persistent (B.0 item 8). The S6 error summary uses `role="alert"` (assertive, appropriate for submit failure). Inline field errors keep their existing `role="alert"` paragraphs. The stepper is silent (no live region — state changes are conveyed by the B.2 announcements). No other live regions.

### B.5 Live price breakdown

- `liveCount = savedPassengers().length + (phase().kind === 'collecting' ? 1 : 0)` — i.e. saved + the in-progress **new** form when open. An edit form does not add (that passenger is already counted). In S2/S3 liveCount = saved count. On first load liveCount = 1 (0 saved + open form), matching a fresh single-passenger booking.
- Breakdown card shows, via the existing `pricing.util.ts` functions only: total (`--font-size-2xl`, dominant, per NFR-USE-002), then `{formatUsd(perPerson)} per person × {liveCount} passenger{s}`.
- Announcement: count/total changes are **not** announced by a separate live region (that would spam SR users on every transition); instead the total is folded into the Save/Remove announcements (B.2), which are the only moments the committed total meaningfully changes. The +1 for an opened empty form changes the displayed projection but is not announced (it's covered by "Enter details for passenger {N}").
- Searched-vs-actual note (B.2 wording) renders inside this card whenever `liveCount !== bookingState.passengerCount()`, and again in the review block.

### B.6 Backend `passengers[i].field` error mapping

1. On `submitBooking` outcome `'validation'`, copy `bookingState.fieldErrors()` into a component-local `serverFieldErrors` signal (the service signal is reset on next submit; the local copy is what the wizard mutates as the user repairs passengers).
2. Parse keys with regex `^passengers\[(\d+)\]\.(fullName|email|documentNumber|documentType)$`. Non-passenger keys (`flight`, `passengerCount`) render in the generic banner (existing `genericServerError` logic).
3. Build the error summary banner listing every offending passenger (B.2); badge every offending card `Needs correction`.
4. Auto-open S4 (edit, `returnTo: 'review'`) for the **smallest** offending index, passing that index's errors through the existing `serverErrors` input of `PassengerFormSectionComponent` (its server-error rendering already exists).
5. When the user saves that passenger, delete all `passengers[K].*` keys from the local copy, clear the card's badge, then auto-open the next offending index (if any) with focus on its Full Name input and announcement `Editing passenger {N}.` — **carrying the original `returnTo`, never overwriting it with `'review'`** (a repair chain entered from an edit over a dirty S1 draft keeps `returnTo: 'collecting'`, CR-002). When none remain: if `returnTo` is `'collecting'`, resolve through the B.1 parked-draft restore (reopen S1 with the draft, announce `All corrections saved. Returning to passenger {N} details.`); otherwise return to S3, remove the banner, focus the review heading, and announce `All corrections saved. Review and confirm your booking.` (Amendment 6). Either way the banner clears and `parkedDraft` never stays orphaned.
6. Staleness rule: any structural change (Remove, or adding a new passenger) clears **all** indexed server errors and badges — indices no longer correspond to what the backend saw, and stale errors on the wrong person are worse than none.
7. Index alignment is guaranteed at submit time because the request's `passengers` array is built from `savedPassengers()` in card order, `passengerCount = savedPassengers().length`, and the S5 lockdown (B.1) freezes the array while the request is in flight.

### B.7 Angular implementation sketch

State shape (all component-local; `BookingStateService` untouched — its `passengerCount` signal keeps meaning "searched count"). **Discriminated-union phase (Amendment 3)** — replaces the three parallel signals (`stage` + `editIndex` + `stageBeforeEdit`) so illegal combinations (e.g. an edit index with no edit stage) are unrepresentable:

```ts
type Phase =
  | { kind: 'collecting' }                                                    // new-passenger form open
  | { kind: 'prompt' }
  | { kind: 'review' }                                                        // covers S3/S5/S7/S8 slot; S5/S7/S8 derive from loading()/error/confirmed signals
  | { kind: 'editing'; index: number; returnTo: 'collecting' | 'prompt' | 'review' };

interface SavedPassenger { fullName: string; email: string; documentNumber: string; }

phase = signal<Phase>({ kind: 'collecting' });
savedPassengers = signal<SavedPassenger[]>([]);
parkedDraft = signal<SavedPassenger | null>(null);      // only ever set when entering editing with returnTo: 'collecting'
activeFormDirty = signal(false);                        // mirrors activeForm dirtiness; drives the edit-lock (B.1)
serverFieldErrors = signal<FieldErrors | null>(null);   // local mutable copy (B.6)
liveMessage = signal('');
pendingFocus = signal<FocusTarget | null>(null);        // consumed by afterRenderEffect(...)
readonly MAX_PASSENGERS = 9;

activeFormIndex = computed(() => {
  const p = this.phase();
  return p.kind === 'editing' ? p.index : this.savedPassengers().length;
});
liveCount = computed(() =>
  this.savedPassengers().length + (this.phase().kind === 'collecting' ? 1 : 0));
editLockActive = computed(() => this.phase().kind === 'editing' && this.activeFormDirty());
mutationLocked = computed(() => this.loading()); // S5 lockdown; OR alreadyConfirmed() for read-only S8
```

Transition helpers pattern-match on `phase().kind`; every transition sets `phase`, `pendingFocus`, and `liveMessage` in the same method call. `returnTo` is carried, not recomputed — resolving an edit does `phase.set(returnTo === 'collecting' ? { kind: 'collecting' } : { kind: returnTo })`, restoring `parkedDraft` when returning to `collecting`.

**Single reusable FormGroup, not a FormArray.** Exactly one form is ever editable; saved passengers are plain data. This eliminates FormArray index churn on remove, makes renumbering free (positional render), and the existing `buildPassengerGroup(routeType)` builds the one group. The current `passengersForm: FormArray` / `bookingForm` wrapper / `formStatus` / `allPassengersValid` members are all removed. `savePassenger()` does `markAllAsTouched()` → if valid, trim values, write into `savedPassengers` (push, or splice at `phase().index` when editing), `activeForm.reset()`, transition. Enter-key save works by wrapping the active form in `<form [formGroup]="activeForm" (ngSubmit)="savePassenger()">`; Confirm Booking is a plain `type="button"` in the review block (no native submit path, so the QA-003 page-reload concern does not reapply).

**`PassengerFormSectionComponent` reused with one additive change (Amendment 8):** bindings `[group]="activeForm"`, `[index]="activeFormIndex()"`, `[isLead]="activeFormIndex() === 0"`, `[documentLabel]="documentLabel()"`, `[serverErrors]="serverErrorsForActive()"`. Its index-suffixed input ids never collide because only one instance exists at a time. The one template change: WCAG 1.3.5 Identify Input Purpose — the Full Name input gains `[attr.autocomplete]="isLead() ? 'name' : null"` and the Email input `[attr.autocomplete]="isLead() ? 'email' : null"`. The primary contact is the user's own identity (autofill materially helps in a repeated-entry flow); passengers 2+ are other people, so the attribute is deliberately omitted there rather than inviting wrong-person autofill. No autocomplete token exists for document numbers; that field gets none.

**Focus mechanism:** template refs + `viewChild` signals for the fixed targets (yes-button, review heading, banners, first form input); per-card Edit/Remove targeted via `viewChildren` keyed by index. An `afterRenderEffect` consumes `pendingFocus`, calls `.focus()`, resets to null. Every transition method sets `pendingFocus` in the same call that mutates `phase` — this is the structural guarantee focus never falls to `<body>`.

**Submit:** builds `passengers` from `savedPassengers()` with `documentType = documentTypeForRouteType(routeType())` per passenger; `passengerCount: passengers.length`; flight snapshot unchanged. Contract identical to today: `BookingRequest { flight snapshot, passengerCount, passengers[] }`. No backend behavior changes.

Edge cases:
- **Double-submit:** Confirm guarded by `loading() || alreadyConfirmed() || phase().kind !== 'review' || saved.length === 0`; button `aria-disabled` (not `disabled`) so focus survives (B.4).
- **Submit-in-flight lockdown:** `mutationLocked()` gates Edit/Remove/Add handlers and sets their `aria-disabled` (Amendment 5).
- **Remove while form open:** list mutation only; `activeFormIndex` recomputes so an open new form's legend renumbers live; if an edit is open on index J and a card K < J is removed, `phase` is rewritten with `index: J - 1`; all indexed server errors cleared.
- **Edit-over-edit:** pristine → silent cancel + reopen at new index (carry `returnTo`); dirty → other Edit buttons `aria-disabled` with visually-hidden suffix and click guard (Amendment 4). Never a silent discard of dirty data.
- **Cap:** `savePassenger` at saved.length = 8 (becoming 9) routes to S3 directly; "Add another passenger" and the prompt path are unreachable at 9 (also guarded in the handlers).
- **Route type:** unchanged — resolved once from the selected flight; the single `activeForm`'s documentNumber validator is built with it, so edit forms revalidate identically. International route → 'Passport Number' label + passport validation; domestic → 'National ID' + its validation (both switch together; existing `document-number.validators.ts` unchanged).
- **alreadyConfirmed on back-nav:** cards render read-only (Edit/Remove hidden), slot shows the confirmed banner, Confirm `aria-disabled` — matching current FR-038 behavior. The B.11 guard is disarmed.

### B.8 Visual composition (tokens by name, from visual-design-spec §1 only)

- **Saved card (`<li>`):** §4.2 recipe verbatim — `1px solid var(--color-neutral-200)`, `var(--radius-md)`, `padding: var(--space-4)`, `margin-bottom: var(--space-4)`, `background: var(--color-white)`, `box-shadow: var(--shadow-sm)`. Eyebrow "Passenger N": `var(--font-size-sm)`, `var(--color-neutral-600)`. Name: `var(--font-size-lg)`, `var(--font-weight-bold)`, `var(--color-neutral-900)`. Email/document lines: `var(--font-size-base)` / `var(--color-neutral-700)`. Primary Contact badge: existing `.lead-badge` treatment (`--font-size-sm`, `--color-neutral-600`).
- **Active form:** the existing `.passenger-section` fieldset card (§4.2, `--color-neutral-300` border) plus a `3px solid var(--color-primary)` left border — the one visual cue distinguishing "the thing you're working on" from saved cards, using the existing brand token, no new color.
- **Prompt block:** `background: var(--color-surface-subtle)`, `var(--radius-md)`, `padding: var(--space-4)`, question in `var(--font-weight-semibold)`; buttons row `gap: var(--space-3)`.
- **Buttons:** `Save passenger` / `Save changes` / `Yes, add another` / `Confirm Booking` = §5.2 primary (filled `--color-primary`, hover `--color-primary-hover`, active `--color-primary-active`). `No, continue to review` / `Add another passenger` / `Cancel` / `Edit` = §5.2 secondary (outline, hover `--color-primary-tint`). `Remove` = secondary shape with `color/border: var(--color-error)` on `--color-white` (7.32:1 pair already verified in the Phase 17 table) and hover `background: var(--color-error-bg)` (≈6.9:1, also verified). Card action buttons use compact padding `var(--space-2) var(--space-3)` and `var(--font-size-sm)`. All buttons keep the additive `:focus-visible` outline from §5.2; never `outline: none`. `aria-disabled` state (edit lock / S5 lockdown): `opacity: 0.55` + `cursor: not-allowed`; element stays focusable and keeps its focus outline.
- **Needs-correction badge:** pill, `var(--color-error)` on `var(--color-error-bg)`, `var(--radius-sm)`, `var(--font-size-xs)`, padding `var(--space-1) var(--space-2)`.
- **Error summary banner:** existing `.banner-error` tokens (`--color-error` on `--color-error-bg`).
- **Notes (intro, cap, searched-vs-actual):** deliberately subtle per the locked decision — `var(--font-size-sm)`, `var(--color-neutral-600)`, no border/background (not the yellow `.banner-info`, which stays reserved for the already-confirmed state).
- **Review block:** §4.2 card; heading `var(--font-size-xl)`; total line `var(--font-size-2xl)` / `var(--font-weight-bold)` (harmonized size per §2).
- **Stepper pills:** see B.10 — done = `--color-primary` on `--color-primary-tint` (5.69:1, verified §3.1); current = `--color-white` on `--color-primary`; upcoming = `--color-neutral-600` on `--color-white` with `1px solid var(--color-neutral-300)`.
- No new hex values anywhere; the only "new" pairing (`--color-error` hover on `--color-error-bg`) reuses a Phase-17-verified pair.

### B.9 Accessibility expectations (summary for the review gate)

Semantic list for cards; one persistent polite live region with the exact strings in B.2; `role="alert"` for submit failures and inline errors; explicit focus target for every DOM-removing transition (B.4 table) with `tabindex="-1"` on non-interactive targets; `aria-disabled` (never `disabled`) on the in-flight Confirm, the edit-lock, and the S5 lockdown, all with click guards; per-card action buttons with name-qualified `aria-label`s including the edit-lock reason suffix; stepper as a non-interactive `<ol>` with `aria-current="step"` and visually-hidden per-step state text; `autocomplete="name"`/`"email"` on the primary contact's inputs (WCAG 1.3.5); state never conveyed by color alone (badges and hidden text carry it); heading order h1 → h2 → h3 preserved; native controls throughout; focus outlines additive only.

### B.10 Progress stepper (Amendment 2)

Non-interactive progress indicator, rendered directly under the "Passengers" h2 (B.0 item 5). It sets count expectations from the first second — a user who searched for 4 passengers immediately sees four pills plus Review.

Markup:

```html
<ol class="wizard-steps" aria-label="Booking progress">
  @for (step of steps(); track step.id) {
    <li [class.step-done]="step.state === 'done'"
        [class.step-current]="step.state === 'current'"
        [class.step-upcoming]="step.state === 'upcoming'"
        [attr.aria-current]="step.state === 'current' ? 'step' : null">
      <span aria-hidden="true">{{ step.visibleLabel }}</span>
      <span class="visually-hidden">{{ step.srLabel }}</span>
    </li>
  }
</ol>
```

Rules:

- **Pill count:** `min(9, max(searchedCount, savedCount + (phase().kind === 'collecting' ? 1 : 0)))` passenger pills, always followed by one terminal `Review` pill. Seeded from the searched passenger count so all expected steps are visible as ghosts before any are filled; grows dynamically if the user adds beyond the searched count (cap 9).
- **Passenger pill i (1-based) state:** `current` when the active form (new or edit) is for passenger i; `done` when passenger i is saved (and not currently being edited); `upcoming` (ghost) otherwise.
- **Review pill state:** `current` when the slot shows the review block (S3/S5/S6-banner/S7); `done` in S8 (confirmed); `upcoming` otherwise.
- **During S2 (prompt):** no pill carries `aria-current` (all pills are done or upcoming) — valid, `aria-current` is simply absent.
- **Visible labels:** the bare numeral `1`…`9`; the word `Review` for the terminal pill. **Visually-hidden labels:** the exact strings in B.2 ("Passenger 1: completed", "Passenger 3: not started", "Review: in progress", …). Visible numerals are `aria-hidden="true"` so SR users hear only the full hidden string.
- **Not interactive, not focusable:** plain `<li>` content, no links or buttons, no `tabindex`. Sequence control belongs to the wizard's own buttons; a clickable stepper would create a second, conflicting navigation model.
- **No live region:** stepper state changes are already narrated by the B.2 announcements.

Visual (verified token pairs only): pills in a wrapping flex row, `gap: var(--space-2)`, `margin-bottom: var(--space-4)`. Each pill: `var(--font-size-sm)`, padding `var(--space-1) var(--space-2)`, fully rounded (`border-radius: 999px`). `done` = `var(--color-primary)` text on `var(--color-primary-tint)` (5.69:1, §3.1-verified pair); `current` = `var(--color-white)` on `var(--color-primary)`; `upcoming` = `var(--color-neutral-600)` on `var(--color-white)` with `1px solid var(--color-neutral-300)`. State is never conveyed by color alone — the hidden text carries it.

### B.11 Navigation guard (Amendment 7)

In a one-at-a-time wizard the accumulated cost of accidental navigation (back button, header link, "Start a New Search") is far higher than in the old all-at-once form — up to nine saved passengers can vanish. Two complementary guards:

1. **Router `canDeactivate` guard** on the booking route. When armed, show native `confirm()` with the exact B.2 string: `Leave this page? Passenger details you've entered will be lost.` OK → navigation proceeds (state discarded as today); Cancel → navigation aborted, focus stays on the triggering element (B.4).
2. **`window:beforeunload` handler** (host binding in the booking component) calling `event.preventDefault()` when armed — covers tab close/refresh with the browser's own generic dialog (custom text is not possible there by browser design).

**Armed** when ALL hold: booking not confirmed (`!alreadyConfirmed()` and no post-201 success flag), AND (`savedPassengers().length > 0` OR the active form is dirty OR `parkedDraft()` is non-null).

**Never fires:** on the programmatic navigate to `/confirmation` after a 201 (the success flag is set before `router.navigate`), in S0 (nothing entered), or after S8. The guard protects data, not the route.

---

## Part C — Acceptance checklist

Engineers self-check and reviewers verify every line. Requirement traceability: search capture incl. passengers 1–9 (client PDF), total-first pricing (client PDF), booking summary + price breakdown + passenger form + reference code (client PDF), document label/validation switch (client PDF), one-at-a-time wizard (PO directive), Edit/Remove + focus + aria-live + server-error mapping (orchestrator decisions).

### Search form (Part A)
- [ ] Yes/no passenger counter fully removed from template, component class, and CSS (all listed members/classes deleted; no orphans).
- [ ] Native `<select id="passengerCount">` with options 1–9, label "Passengers", default 1, `[ngValue]` bindings (control value stays `number`).
- [ ] No aria-live/count announcement added for the select.
- [ ] Submitted search still carries passenger count; results price totals for all passengers.

### Wizard structure and states (B.0–B.1)
- [ ] Page order matches B.0; stepper renders under the "Passengers" h2; exactly one slot element at a time; edit form replaces card K in place.
- [ ] Passenger 1's form opens immediately when a flight is present; intro note shown iff searched count > 1.
- [ ] Save (valid, < 9) → prompt "Add another passenger?" with `Yes, add another` / `No, continue to review`.
- [ ] Save of 9th passenger skips the prompt → review with cap note; no add path reachable at 9.
- [ ] Review block: count + total line, Add another (if < 9), Confirm Booking; Confirm submits and yields a booking reference (existing flow).
- [ ] Removing the last card restarts the wizard at passenger 1 (Remove never `disabled`).
- [ ] `Phase` is the discriminated union of B.7 — no separate `stage`/`editIndex`/`stageBeforeEdit` members exist.

### Edit / Remove / error correction (B.1, B.3)
- [ ] Every saved card shows Passenger N, full name, unmasked email, unmasked `{documentLabel}: {number}`, Edit + Remove with name-qualified `aria-label`s.
- [ ] Edit over a dirty S1 form parks the draft and restores it afterward with the "Returning to passenger {N} details." announcement.
- [ ] Edit while another edit is DIRTY is blocked: other Edit buttons `aria-disabled` + click guard + visually-hidden "(finish the current passenger first)" suffix; re-enabled on save/cancel. No silent discard path exists for dirty edits.
- [ ] Removing a card renumbers all later cards positionally (eyebrows, aria-labels) and clears ALL indexed server errors/badges.
- [ ] Remove during an open edit decrements the edit index when a preceding card is removed.

### Focus management (B.4)
- [ ] Every row of the B.4 table verified manually with keyboard: after each listed trigger, `document.activeElement` is the specified target, never `<body>`.
- [ ] Confirm button and all lockdown/edit-locked controls use `aria-disabled` + click guard, never the `disabled` attribute.
- [ ] Non-interactive focus targets (review heading, banners) carry `tabindex="-1"`.

### Announcements (B.2)
- [ ] One persistent (unconditionally rendered) polite `role="status"` region; all B.2 strings emitted verbatim, including totals via `formatUsd`.
- [ ] Corrections-complete announcement fires exactly when the last flagged passenger is saved.
- [ ] Consecutive identical messages still re-announce (clear-then-set or zero-width variation).

### Live price breakdown (B.5)
- [ ] Total = per-person × liveCount using `pricing.util.ts` only; total is the dominant number; per-person shown as secondary.
- [ ] liveCount includes an open NEW form (+1) but not an edit form; verified across all states.
- [ ] Searched-vs-actual note appears (breakdown card and review block) iff liveCount ≠ searched count, with exact B.2 wording, subtle styling.

### Server error mapping (B.6)
- [ ] 400 with `passengers[i].*` → local error copy, summary banner (`role="alert"`, focused), badges, auto-edit of smallest index, chained repair, banner cleared when done.
- [ ] `flight`/`passengerCount` keys and network/5xx → generic `.banner-error` (focused).
- [ ] Structural change during repair clears all indexed errors and badges.

### Submit lockdown and contract (B.1, B.7)
- [ ] During POST: Confirm, every Edit/Remove, and Add another are all `aria-disabled` with click guards; array cannot mutate mid-flight.
- [ ] Request body: flight snapshot unchanged, `passengerCount === passengers.length`, passengers in card order, per-passenger `documentType` from route type. No backend changes.
- [ ] Document label AND validation switch together by route type (Passport Number ↔ National ID), identical for new and edit forms.

### Stepper (B.10)
- [ ] `<ol aria-label="Booking progress">`, non-interactive, pill count = max(searched, active) capped at 9, terminal Review pill.
- [ ] `aria-current="step"` on exactly the active pill (absent during prompt); visually-hidden state text per pill matches B.2.
- [ ] Token pairs exactly as specified (done: primary-on-tint; current: white-on-primary; upcoming: neutral-600 ghost).

### Navigation guard (B.11)
- [ ] canDeactivate confirm with exact wording + beforeunload, armed per B.11 conditions.
- [ ] Guard does NOT fire on post-201 navigation to `/confirmation`, in S0, or after confirmation.
- [ ] Cancelling the confirm leaves focus on the triggering element.

### Autocomplete (B.7)
- [ ] Lead passenger's Full Name has `autocomplete="name"` and Email has `autocomplete="email"`; passengers 2+ have neither attribute.

### Visual (B.8)
- [ ] All colors/spacing/radii via visual-design-spec §1 tokens; zero new hex values; focus outlines additive; `aria-disabled` styling keeps elements focusable.
- [ ] Card actions row → stacked below ~480px; heading hierarchy h1 → h2 → h3 intact.

### Regression guardrails
- [ ] `alreadyConfirmed` back-nav state: read-only cards, confirmed banner, Confirm `aria-disabled` (FR-038 behavior preserved).
- [ ] Confirm Booking is `type="button"` outside any native submit path (QA-003 page-reload regression cannot reapply); Enter in the active form triggers `savePassenger()` only.
- [ ] `BookingStateService` public API unchanged; `PassengerFormSectionComponent` changed only by the two `autocomplete` attribute bindings.

---

*End of specification.*
