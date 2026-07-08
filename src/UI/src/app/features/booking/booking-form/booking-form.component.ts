import {
  Component,
  ElementRef,
  HostListener,
  afterRenderEffect,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { getCountryForCode } from '../../../shared/constants/airports.constants';
import { FieldErrors } from '../../../shared/models/api-error.model';
import { BookingFlightSnapshot, BookingRequest } from '../../../shared/models/booking-request.model';
import { PassengerDetail } from '../../../shared/models/passenger-detail.model';
import { formatDuration, formatTime } from '../../../shared/utils/datetime-format.util';
import { calculateTotalPrice, formatUsd } from '../../../shared/utils/pricing.util';
import {
  RouteType,
  ageValidator,
  documentHintForRouteType,
  documentLabelForRouteType,
  documentNumberValidator,
  documentTypeForRouteType,
  emailFormatValidator,
  fullNameValidator,
  resolveRouteType,
} from '../../../shared/validators/document-number.validators';
import { BookingStateService } from '../booking-state.service';
import {
  PassengerFormSectionComponent,
  PassengerServerErrors,
} from '../passenger-form-section/passenger-form-section.component';

/**
 * In-place flow phases (PO UX correction 2026-07-07). 'collecting' = the single blank/new
 * passenger form is open below the cards; 'editing' = a saved card's values are loaded into
 * that same form. There is no prompt step and no separate review step anymore.
 */
type Phase = { kind: 'collecting' } | { kind: 'editing'; index: number };

/** Saved passengers are plain data, not form controls — single reusable FormGroup. */
interface SavedPassenger {
  fullName: string;
  age: number;
  email: string;
  documentNumber: string;
}

/** Raw active-form snapshot for the parked-draft mechanism — unlike a SavedPassenger it may
 * still have an empty (null) age, since a draft is parked before validation. */
interface ActiveFormDraft {
  fullName: string;
  age: number | null;
  email: string;
  documentNumber: string;
}

/** The only key shape mapped back onto a passenger card (server field-error mapping). */
const PASSENGER_ERROR_KEY = /^passengers\[(\d+)\]\.(fullName|age|email|documentNumber|documentType)$/;

const FORM_FIELD_ORDER = ['fullName', 'age', 'email', 'documentNumber'] as const;

function passengerWord(count: number): string {
  return count === 1 ? 'passenger' : 'passengers';
}

/**
 * Single-button in-place passenger flow (PO UX correction 2026-07-07 — overrides
 * DESIGN-FLOW-001 Part B where they conflict). Replaces the save-then-prompt wizard:
 * exactly one active passenger form, rendered in the same place below the saved-passenger
 * cards, with exactly two persistent actions under it —
 *
 * 1. "Add another passenger" (secondary): validates the active form; valid → append a card,
 *    reset the same form in place, focus its first field; invalid → touched errors + focus.
 * 2. "Confirm Booking" (primary): a dirty/filled form is validated and saved first, then ALL
 *    saved passengers are submitted; a blank form with ≥ 1 saved passenger submits as-is;
 *    blank + none saved surfaces the required errors for passenger 1.
 *
 * While editing a card, the actions become "Save changes" / "Cancel edit" in the same spot.
 * Price breakdown counts max(saved, 1) — an in-progress blank form never inflates the total.
 * Kept from the previous wizard: flight summary, price breakdown, summary cards with
 * Edit/Remove + positional renumbering, document label/validation switching, loading and
 * alreadyConfirmed guards, passengers[i].* server-error mapping (reopens the right passenger),
 * and post-201 navigation to /confirmation. BookingRequest wire shape unchanged;
 * passengerCount = passengers.length.
 */
@Component({
  selector: 'app-booking-form',
  standalone: true,
  imports: [ReactiveFormsModule, PassengerFormSectionComponent, RouterLink],
  templateUrl: './booking-form.component.html',
  styleUrl: './booking-form.component.css',
})
export class BookingFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef);
  protected readonly bookingState = inject(BookingStateService);

  protected readonly MAX_PASSENGERS = 9;

  protected readonly flight = this.bookingState.selectedFlight;
  protected readonly loading = this.bookingState.loading;

  /** DP-016: frontend equivalent of the backend's authoritative RouteTypeResolver, used only
   * for immediate label/validation feedback — the backend re-resolves and authoritatively
   * enforces route type independently at booking time (BR-003, NFR-DATA-004). */
  protected readonly routeType = computed<RouteType>(() => {
    const flight = this.flight();
    if (!flight) {
      return 'International';
    }
    return resolveRouteType(getCountryForCode(flight.origin), getCountryForCode(flight.destination));
  });

  protected readonly documentLabel = computed(() => documentLabelForRouteType(this.routeType()));

  /** Format hint for the document field (WCAG 3.3.2, PO 2026-07-08) — switches with routeType. */
  protected readonly documentHint = computed(() => documentHintForRouteType(this.routeType()));

  protected readonly alreadyConfirmed = computed(() => this.bookingState.bookingResponse() !== null);

  protected readonly phase = signal<Phase>({ kind: 'collecting' });

  protected readonly savedPassengers = signal<SavedPassenger[]>([]);

  /** Only ever set when Edit is pressed over a dirty in-progress new-passenger form —
   * the draft is restored (never silently discarded) when the edit resolves. */
  protected readonly parkedDraft = signal<ActiveFormDraft | null>(null);

  /** Mirrors the active form's dirtiness reactively (drives the edit lock + leave guard). */
  protected readonly activeFormDirty = signal(false);

  /** Local mutable copy of submit-time validation errors — the service signal is reset on
   * the next submit; this copy is what gets cleared as passengers are repaired. */
  private readonly serverFieldErrors = signal<FieldErrors | null>(null);

  /** Text for the single persistent polite live region. */
  protected readonly liveMessage = signal('');

  /** CSS selector consumed by the afterRenderEffect below — the structural guarantee that
   * every DOM-removing transition names its focus target in the same call. */
  private readonly pendingFocus = signal<string | null>(null);

  /** Set before the post-201 navigate so the leave guard never fires on success. */
  private readonly navigatedAfterSuccess = signal(false);

  /** The single reusable FormGroup; exactly one passenger form is ever editable. The
   * documentNumber validator is built once from the resolved route type, so new and edit
   * forms revalidate identically (label + validation switch together, BR-003). The age
   * validator is static — required whole number 0–120, identical at every position
   * (DEC-022, PO 2026-07-08: age is pure data capture, no business rule bound to it). */
  protected readonly activeForm = this.fb.nonNullable.group({
    fullName: this.fb.nonNullable.control('', { validators: [fullNameValidator()] }),
    age: this.fb.control<number | null>(null, { validators: [ageValidator()] }),
    email: this.fb.nonNullable.control('', { validators: [emailFormatValidator()] }),
    // AUD-004: the documentNumber validator reads routeType() at validation time (not frozen at
    // construction), so the enforced pattern always matches the reactive label/hint. A constructor
    // effect re-runs validation when routeType changes, keeping label + validation in lockstep for
    // any future in-place "change flight"/route-reuse flow (BR-003 "switch together").
    documentNumber: this.fb.nonNullable.control('', {
      validators: [(control: AbstractControl): ValidationErrors | null => documentNumberValidator(this.routeType())(control)],
    }),
  });

  protected readonly activeFormIndex = computed(() => {
    const p = this.phase();
    return p.kind === 'editing' ? p.index : this.savedPassengers().length;
  });

  /** The cap hides the new-passenger form and the add action; editing stays possible. */
  protected readonly atCap = computed(() => this.savedPassengers().length >= this.MAX_PASSENGERS);

  /** The single in-place form renders while collecting below the cap, or during any edit. */
  protected readonly showActiveForm = computed(
    () => !this.alreadyConfirmed() && (this.phase().kind === 'editing' || !this.atCap()),
  );

  /** Submit-lockdown: freezes every mutating control while the POST is in flight. */
  protected readonly mutationLocked = computed(() => this.loading());

  /** While an edit form is dirty, every other card's Edit is blocked (never a silent discard). */
  protected readonly editDirtyLock = computed(() => this.phase().kind === 'editing' && this.activeFormDirty());

  /** PO rule: the price counts max(saved, 1) — a blank/in-progress form never inflates the
   * total; while entering passenger 1 it shows × 1. Once confirmed (back-nav, S8) the page
   * shows what was actually BOOKED — the response's own totalPrice and count (CR-003). */
  protected readonly priceBreakdown = computed(() => {
    const flight = this.flight();
    if (!flight) {
      return null;
    }
    const response = this.bookingState.bookingResponse();
    if (response && this.savedPassengers().length === 0) {
      return {
        perPerson: response.flight.pricePerPassenger,
        count: response.passengers.length,
        total: response.totalPrice,
      };
    }
    const count = Math.max(this.savedPassengers().length, 1);
    return {
      perPerson: flight.pricePerPassenger,
      count,
      total: calculateTotalPrice(flight.pricePerPassenger, count),
    };
  });

  /** Simple visible status replacing the old searched-count progress list. */
  protected readonly savedCountLabel = computed<string | null>(() => {
    const count = this.savedPassengers().length;
    if (count === 0 || this.alreadyConfirmed()) {
      return null;
    }
    return `${count} ${passengerWord(count)} added`;
  });

  /** Indexed server errors parsed from the local copy, grouped per passenger. */
  private readonly indexedServerErrors = computed(() => {
    const byIndex = new Map<number, { field: string; message: string }[]>();
    const errors = this.serverFieldErrors();
    if (!errors) {
      return byIndex;
    }
    for (const [key, messages] of Object.entries(errors)) {
      const match = PASSENGER_ERROR_KEY.exec(key);
      if (!match || messages.length === 0) {
        continue;
      }
      const index = Number(match[1]);
      const entries = byIndex.get(index) ?? [];
      entries.push({ field: match[2], message: messages[0] });
      byIndex.set(index, entries);
    }
    return byIndex;
  });

  protected readonly flaggedIndices = computed(() =>
    Array.from(this.indexedServerErrors().keys()).sort((a, b) => a - b),
  );

  /** Error summary — one line per offending passenger, backend's own message text. */
  protected readonly errorSummaryLines = computed(() => {
    const byIndex = this.indexedServerErrors();
    return this.flaggedIndices().map(
      (index) => `Passenger ${index + 1}: ${(byIndex.get(index) ?? []).map((e) => e.message).join(' ')}`,
    );
  });

  /** Server errors routed into the open edit form via PassengerFormSectionComponent. */
  protected readonly activeServerErrors = computed<PassengerServerErrors | null>(() => {
    const p = this.phase();
    if (p.kind !== 'editing') {
      return null;
    }
    const entries = this.indexedServerErrors().get(p.index);
    if (!entries || entries.length === 0) {
      return null;
    }
    const result: PassengerServerErrors = {};
    for (const entry of entries) {
      result[entry.field as keyof PassengerServerErrors] = entry.message;
    }
    return result;
  });

  /** AUD-036: fallback shown when a 400 carried no renderable key at all (empty/malformed
   * errors object) — guarantees FR-071 feedback for every rejected booking. */
  private readonly submitFallbackError = signal<string | null>(null);

  /** Non-passenger submit errors → generic banner. AUD-036: this previously matched only the
   * exact keys `flight` and `passengerCount`, so every DOTTED `flight.*` key (e.g.
   * `flight.flightNumber`, exactly what the SEC-001 fare/price-tamper responses return) was
   * dropped silently — the 400 left a dead, feedback-less form. It now surfaces EVERY
   * validation key that is not a per-passenger field error (those are rendered in the error
   * summary + reopened passenger), so no 400 key can ever be swallowed. */
  protected readonly genericServerError = computed(() => {
    const message = this.bookingState.errorMessage();
    if (message) {
      return message;
    }
    const errors = this.bookingState.fieldErrors();
    const unmapped: string[] = [];
    if (errors) {
      for (const [key, messages] of Object.entries(errors)) {
        if (PASSENGER_ERROR_KEY.test(key) || messages.length === 0) {
          continue;
        }
        unmapped.push(messages[0]);
      }
    }
    if (unmapped.length > 0) {
      return unmapped.join(' ');
    }
    return this.submitFallbackError();
  });

  /** Armed while unconfirmed passenger data exists that navigation would destroy. */
  protected readonly guardArmed = computed(
    () =>
      !this.alreadyConfirmed() &&
      !this.navigatedAfterSuccess() &&
      (this.savedPassengers().length > 0 || this.activeFormDirty() || this.parkedDraft() !== null),
  );

  constructor() {
    // Dirtiness mirrored into a signal (PristineChangeEvent fires on user edits, reset(),
    // markAsDirty/markAsPristine) so the edit lock re-enables the moment an edit is saved
    // or cancelled.
    this.activeForm.events.pipe(takeUntilDestroyed()).subscribe(() => {
      this.activeFormDirty.set(this.activeForm.dirty);
    });

    // AUD-004: when the resolved route type changes, re-run the documentNumber validator so its
    // validity reflects the new pattern in lockstep with the label/hint computeds (emitEvent:false
    // keeps this from perturbing the dirty/touched state).
    effect(() => {
      this.routeType();
      this.activeForm.controls.documentNumber.updateValueAndValidity({ emitEvent: false });
    });

    // Focus mechanism: every transition sets `pendingFocus` in the same call that mutates
    // `phase`/`savedPassengers`; this effect applies it after the render that reflects the
    // change — the structural guarantee focus never silently drops to <body>. The page h1
    // carries tabindex="-1" purely as a defensive last-resort anchor.
    afterRenderEffect(() => {
      const selector = this.pendingFocus();
      if (!selector) {
        return;
      }
      this.pendingFocus.set(null);
      const root = this.host.nativeElement as HTMLElement;
      const target = root.querySelector<HTMLElement>(selector) ?? root.querySelector<HTMLElement>('h1');
      target?.focus();
    });
  }

  /** beforeunload covers tab close/refresh with the browser's generic dialog. */
  @HostListener('window:beforeunload', ['$event'])
  handleBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.guardArmed()) {
      event.preventDefault();
    }
  }

  /** Consumed by bookingLeaveGuard (canDeactivate). */
  canLeave(): boolean {
    if (!this.guardArmed()) {
      return true;
    }
    return window.confirm("Leave this page? Passenger details you've entered will be lost.");
  }

  /** AUD-007: the single copy of the Remove-confirmation prompt (extracted so it reads as the
   * intent and is trivially stubbable in tests). Wording mirrors canLeave()'s data-loss tone. */
  protected confirmRemoval(index: number, fullName: string): boolean {
    return window.confirm(
      `Remove passenger ${index + 1}, ${fullName}? The details you've entered will be lost.`,
    );
  }

  protected isEditingIndex(index: number): boolean {
    const p = this.phase();
    return p.kind === 'editing' && p.index === index;
  }

  protected isFlagged(index: number): boolean {
    return this.indexedServerErrors().has(index);
  }

  protected editAriaLabel(index: number, fullName: string): string {
    const base = `Edit passenger ${index + 1}, ${fullName}`;
    return this.editDirtyLock() ? `${base} (finish the current passenger first)` : base;
  }

  protected removeAriaLabel(index: number, fullName: string): string {
    return `Remove passenger ${index + 1}, ${fullName}`;
  }

  protected passengerNoun(count: number): string {
    return passengerWord(count);
  }

  /** ngSubmit of the single in-place form (Enter key included; QA-003 stays intercepted):
   * while editing it saves the edit; while collecting it is the Confirm Booking path. */
  protected onFormSubmit(): void {
    if (this.phase().kind === 'editing') {
      this.saveChanges();
      return;
    }
    void this.onConfirm();
  }

  /** "Add another passenger": validate → append card → reset the SAME form in place. */
  protected addAnotherPassenger(): void {
    if (this.mutationLocked() || this.atCap() || this.phase().kind !== 'collecting') {
      return;
    }
    const passenger = this.validateAndReadActiveForm();
    if (!passenger) {
      return; // invalid: touched errors are visible, focus is on the first invalid control
    }
    this.appendPassenger(passenger);

    const count = this.savedPassengers().length;
    if (count >= this.MAX_PASSENGERS) {
      // The 9th save removes the add path and the blank form — Confirm is the only action.
      this.pendingFocus.set('#confirm-booking-btn');
      this.announce(
        `Passenger ${count} added. Maximum of ${this.MAX_PASSENGERS} passengers reached. Total for ${count} ${passengerWord(count)}: ${this.totalFor(count)}.`,
      );
    } else {
      // Same form, same place, blanked for the next passenger; focus moves to its first field.
      this.pendingFocus.set(`#fullName-${count}`);
      this.announce(`Passenger ${count} added. Total for ${count} ${passengerWord(count)}: ${this.totalFor(count)}.`);
    }
  }

  /** "Cancel edit" — discards the edit and restores the blank/parked collecting form. */
  protected cancelEdit(): void {
    if (this.mutationLocked()) {
      return;
    }
    const p = this.phase();
    if (p.kind === 'editing') {
      this.leaveEditForm(p, 'Edit cancelled.');
    }
  }

  protected beginEdit(index: number): void {
    if (this.mutationLocked() || this.alreadyConfirmed()) {
      return; // lockdown / read-only click guards (aria-disabled)
    }
    const p = this.phase();
    if (p.kind === 'editing') {
      if (p.index === index) {
        return; // that card is already loaded in the form
      }
      if (this.activeFormDirty()) {
        return; // never silently discard a dirty edit — click guard no-ops
      }
      // Pristine edit: cancel silently (nothing lost) and open the other card.
      this.openEditForm(index);
    } else {
      if (this.activeFormDirty()) {
        // Park the in-progress new-passenger draft; restored when this edit resolves.
        this.parkedDraft.set(this.activeForm.getRawValue());
      }
      this.openEditForm(index);
    }
    this.pendingFocus.set(`#fullName-${index}`);
    this.announce(`Editing passenger ${index + 1}.`);
  }

  protected removePassenger(index: number): void {
    if (this.mutationLocked() || this.alreadyConfirmed()) {
      return;
    }
    const p = this.phase();
    if (p.kind === 'editing' && p.index === index) {
      return; // the card being edited has no visible actions; defensive guard
    }
    // AUD-007 (PO-approved 2026-07-08): a saved card holds a full name, age, email and document
    // number — real effort and PII. Confirm before destroying it, consistent with canLeave()'s
    // data-loss guard. Native confirm is keyboard-operable, announced by AT, and returns focus
    // to the invoking Remove button when cancelled, so no extra focus handling is needed for the
    // declined path (nothing mutates and no pendingFocus is set).
    const saved = this.savedPassengers()[index];
    if (saved && !this.confirmRemoval(index, saved.fullName)) {
      return;
    }
    const removedNumber = index + 1;
    this.savedPassengers.update((list) => list.filter((_, i) => i !== index));
    // Structural change: ALL indexed server errors/badges cleared — indices no longer
    // correspond to what the backend saw.
    this.clearAllIndexedServerErrors();

    let current = this.phase();
    if (current.kind === 'editing' && index < current.index) {
      current = { ...current, index: current.index - 1 };
      this.phase.set(current);
    }

    const remaining = this.savedPassengers();
    if (remaining.length === 0) {
      // The collecting form is already open below (min-1 by construction; Remove is never
      // `disabled`); its inputs re-id to index 0 positionally.
      this.pendingFocus.set('#fullName-0');
      this.announce('Passenger removed. Add at least one passenger to continue.');
      return;
    }

    // Focus the Remove button of the card now at position K (or the new last card if K was
    // last); if that position is being edited, its card has no actions — focus the form.
    const focusIndex = Math.min(index, remaining.length - 1);
    if (current.kind === 'editing' && current.index === focusIndex) {
      this.pendingFocus.set(`#fullName-${focusIndex}`);
    } else {
      this.pendingFocus.set(`#card-remove-${focusIndex}`);
    }
    const count = Math.max(remaining.length, 1);
    this.announce(
      `Passenger ${removedNumber} removed. Remaining passengers renumbered. Total for ${count} ${passengerWord(count)}: ${this.totalFor(count)}.`,
    );
  }

  protected startNewSearch(): void {
    void this.router.navigate(['/search']);
  }

  /**
   * "Confirm Booking": a filled active form is validated and saved first, then ALL saved
   * passengers are submitted; a blank form with saved passengers submits as-is; blank +
   * nothing saved is treated as an invalid first-passenger submission.
   */
  protected async onConfirm(): Promise<void> {
    // Double-submit / re-submission / wrong-state guards; the button is aria-disabled
    // (never `disabled`) so focus survives the in-flight state.
    if (this.loading() || this.alreadyConfirmed() || this.phase().kind === 'editing') {
      return;
    }
    const flight = this.flight();
    if (!flight) {
      return;
    }

    if (this.activeFormHasInput()) {
      // Dirty/filled form: validate + save it, then submit everything.
      const passenger = this.validateAndReadActiveForm();
      if (!passenger) {
        return; // invalid: same handling as an invalid add — errors shown, focus moved
      }
      this.appendPassenger(passenger);
    } else if (this.savedPassengers().length === 0) {
      // Blank form, nothing saved: surface the required errors for passenger 1.
      this.activeForm.markAllAsTouched();
      this.focusFirstInvalidControl();
      return;
    }

    const documentType = documentTypeForRouteType(this.routeType());
    // Passengers in card order; passengerCount = passengers.length; the submit lockdown
    // freezes the array while the request is in flight, so index mapping holds. `age` is
    // already a parsed number on the SavedPassenger (validateAndReadActiveForm), never a string.
    const passengers: PassengerDetail[] = this.savedPassengers().map((p) => ({
      fullName: p.fullName,
      age: p.age,
      email: p.email,
      documentType,
      documentNumber: p.documentNumber,
    }));

    const flightSnapshot: BookingFlightSnapshot = {
      provider: flight.provider,
      flightNumber: flight.flightNumber,
      origin: flight.origin,
      destination: flight.destination,
      departureDateTime: flight.departureDateTime,
      arrivalDateTime: flight.arrivalDateTime,
      durationMinutes: flight.durationMinutes,
      cabinClass: flight.cabinClass,
      baseFare: flight.baseFare,
      pricePerPassenger: flight.pricePerPassenger,
    };

    const request: BookingRequest = {
      flight: flightSnapshot,
      passengerCount: passengers.length,
      passengers,
    };

    this.serverFieldErrors.set(null);
    this.submitFallbackError.set(null);
    this.announce('Submitting your booking…');

    const outcome = await this.bookingState.submitBooking(request);

    if (outcome === 'success') {
      this.navigatedAfterSuccess.set(true); // disarm the leave guard before navigating
      await this.router.navigate(['/confirmation']);
      return;
    }

    if (outcome === 'validation') {
      const errors = this.bookingState.fieldErrors();
      this.serverFieldErrors.set(errors ? { ...errors } : null);
      const flagged = this.flaggedIndices();
      if (flagged.length > 0) {
        // Summary banner + reopen the smallest offending passenger in the in-place form.
        this.openEditForm(flagged[0]);
        this.pendingFocus.set('#error-summary');
        return;
      }
      // No per-passenger keys → every remaining key (flight.*/passengerCount/any unmapped key)
      // is surfaced by the generic banner (AUD-036). If the 400 carried no renderable key at
      // all, fall back to a default so a rejected booking is never feedback-less (FR-071).
      if (!this.genericServerError()) {
        this.submitFallbackError.set(
          "We couldn't confirm your booking. Please review your details and try again.",
        );
      }
      this.pendingFocus.set('#generic-error-banner');
      return;
    }

    // Network/5xx — generic error banner, focused.
    this.pendingFocus.set('#generic-error-banner');
  }

  protected formatFlightTime(isoDateTime: string): string {
    return formatTime(isoDateTime);
  }

  protected formatFlightDuration(durationMinutes: number): string {
    return formatDuration(durationMinutes);
  }

  protected formatPrice(amount: number): string {
    return formatUsd(amount);
  }

  /** "Save changes" while editing (also the edit form's ngSubmit path). */
  private saveChanges(): void {
    if (this.mutationLocked()) {
      return;
    }
    const p = this.phase();
    if (p.kind !== 'editing') {
      return;
    }
    const passenger = this.validateAndReadActiveForm();
    if (!passenger) {
      return;
    }

    const wasFlagged = this.indexedServerErrors().has(p.index);
    this.savedPassengers.update((list) => list.map((existing, i) => (i === p.index ? passenger : existing)));
    this.clearServerErrorsForIndex(p.index);

    if (wasFlagged) {
      const remainingFlagged = this.flaggedIndices();
      if (remainingFlagged.length > 0) {
        // Chain to the next server-flagged passenger.
        const next = remainingFlagged[0];
        this.openEditForm(next);
        this.pendingFocus.set(`#fullName-${next}`);
        this.announce(`Editing passenger ${next + 1}.`);
        return;
      }
      this.leaveEditForm(p, 'All corrections saved.');
      return;
    }

    this.leaveEditForm(p, `Passenger ${p.index + 1} updated.`);
  }

  /** Resolves an edit (save or cancel) back to the collecting form, restoring any parked draft. */
  private leaveEditForm(p: Extract<Phase, { kind: 'editing' }>, message: string): void {
    const draft = this.parkedDraft();
    this.parkedDraft.set(null);
    this.resetActiveForm();
    this.phase.set({ kind: 'collecting' });
    if (draft) {
      this.activeForm.reset(draft);
      // The draft was dirty when parked — keep it protected (leave-guard arming).
      this.activeForm.markAsDirty();
      this.activeFormDirty.set(true);
      const nextNumber = this.savedPassengers().length + 1;
      this.pendingFocus.set(`#fullName-${this.savedPassengers().length}`);
      this.announce(`${message} Returning to passenger ${nextNumber} details.`);
      return;
    }
    // Card returns to the list; focus goes back to its Edit button.
    this.pendingFocus.set(`#card-edit-${p.index}`);
    this.announce(message);
  }

  private openEditForm(index: number): void {
    this.activeForm.reset(this.savedPassengers()[index]);
    this.activeFormDirty.set(false);
    this.phase.set({ kind: 'editing', index });
  }

  /** markAllAsTouched + focus-first-invalid on failure; trimmed values on success. */
  private validateAndReadActiveForm(): SavedPassenger | null {
    this.activeForm.markAllAsTouched();
    if (this.activeForm.invalid) {
      this.focusFirstInvalidControl();
      return null;
    }
    const raw = this.activeForm.getRawValue();
    return {
      fullName: raw.fullName.trim(),
      // Parsed to a number for the wire payload (the number input's accessor already yields
      // a number; Number() also covers a programmatically-set string value). Non-null is
      // guaranteed here — the age validator marks an empty control invalid.
      age: Number(raw.age),
      email: raw.email.trim(),
      documentNumber: raw.documentNumber.trim(),
    };
  }

  /** Append + structural-change bookkeeping shared by Add-another and Confirm-with-input. */
  private appendPassenger(passenger: SavedPassenger): void {
    this.savedPassengers.update((list) => [...list, passenger]);
    // Adding a passenger is a structural change: all indexed server errors are stale
    // against what the backend saw and are cleared.
    this.clearAllIndexedServerErrors();
    this.resetActiveForm();
  }

  /** Values-based "has input": a dirtied-then-cleared form counts as blank again. */
  private activeFormHasInput(): boolean {
    const raw = this.activeForm.getRawValue();
    return (
      raw.fullName.trim() !== '' ||
      raw.age !== null ||
      raw.email.trim() !== '' ||
      raw.documentNumber.trim() !== ''
    );
  }

  private resetActiveForm(): void {
    // If focus is still inside a form input (possible with synthetic clicks, which don't
    // blur the focused field the way a real pointer press does), blur it BEFORE resetting —
    // otherwise the later programmatic focus move fires that blur after the reset and
    // Angular re-marks the freshly blank control as touched, surfacing a premature error.
    const focusedControl = (this.host.nativeElement as HTMLElement).querySelector<HTMLElement>('input:focus');
    focusedControl?.blur();
    this.activeForm.reset({ fullName: '', age: null, email: '', documentNumber: '' });
    this.activeFormDirty.set(false);
  }

  private focusFirstInvalidControl(): void {
    const index = this.activeFormIndex();
    for (const name of FORM_FIELD_ORDER) {
      if (this.activeForm.get(name)?.invalid) {
        this.pendingFocus.set(`#${name}-${index}`);
        return;
      }
    }
  }

  private clearServerErrorsForIndex(index: number): void {
    const errors = this.serverFieldErrors();
    if (!errors) {
      return;
    }
    const prefix = `passengers[${index}].`;
    const remaining = Object.fromEntries(Object.entries(errors).filter(([key]) => !key.startsWith(prefix)));
    this.serverFieldErrors.set(Object.keys(remaining).length > 0 ? remaining : null);
  }

  private clearAllIndexedServerErrors(): void {
    const errors = this.serverFieldErrors();
    if (!errors) {
      return;
    }
    const remaining = Object.fromEntries(Object.entries(errors).filter(([key]) => !PASSENGER_ERROR_KEY.test(key)));
    this.serverFieldErrors.set(Object.keys(remaining).length > 0 ? remaining : null);
  }

  private totalFor(count: number): string {
    const flight = this.flight();
    return formatUsd(calculateTotalPrice(flight ? flight.pricePerPassenger : 0, count));
  }

  /** Identical consecutive messages must still re-announce — zero-width-space variation. */
  private announce(message: string): void {
    const zeroWidthSpace = String.fromCharCode(0x200b);
    this.liveMessage.set(this.liveMessage() === message ? message + zeroWidthSpace : message);
  }
}
