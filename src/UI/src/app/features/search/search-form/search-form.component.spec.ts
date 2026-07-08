import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AIRPORTS } from '../../../shared/constants/airports.constants';
import { SearchRequest } from '../../../shared/models/search-request.model';
import { SearchStateService } from '../search-state.service';
import { SearchFormComponent } from './search-form.component';

// Trivial stand-in for the real `/results` route target. `onSubmit()` calls
// `router.navigate(['/results'])` on a successful search, so the test router config needs a
// matching route registered or that navigation rejects with NG04002 ("Cannot match any
// routes"). This component renders nothing and is never asserted against.
@Component({ standalone: true, template: '' })
class ResultsStubComponent {}

describe('SearchFormComponent', () => {
  let fixture: ComponentFixture<SearchFormComponent>;
  let component: SearchFormComponent;
  let fakeSearchState: {
    loading: ReturnType<typeof signal<boolean>>;
    fieldErrors: ReturnType<typeof signal<Record<string, string[]> | null>>;
    errorMessage: ReturnType<typeof signal<string | null>>;
    lastCriteria: ReturnType<typeof signal<SearchRequest | null>>;
    search: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    fakeSearchState = {
      loading: signal(false),
      fieldErrors: signal(null),
      errorMessage: signal(null),
      lastCriteria: signal<SearchRequest | null>(null),
      search: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [SearchFormComponent],
      providers: [
        provideRouter([{ path: 'results', component: ResultsStubComponent }]),
        { provide: SearchStateService, useValue: fakeSearchState },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function submitButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[type="submit"]');
  }

  /** Dispatches a real DOM submit (ngSubmit) — the path a keyboard Enter/click takes. */
  function submitForm(): void {
    const formEl: HTMLFormElement = fixture.nativeElement.querySelector('form');
    formEl.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();
  }

  function alertTexts(): string {
    return Array.from(fixture.nativeElement.querySelectorAll('[role="alert"]'))
      .map((el) => (el as HTMLElement).textContent ?? '')
      .join(' ');
  }

  // `form` and `sameAirportSelected` are `protected` on SearchFormComponent (template-only
  // access by design). An `any` cast is used here purely to reach them from the test without
  // weakening the component's public API — this is test-only code, never application code.
  function form(c: SearchFormComponent) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return (c as any).form as {
      invalid: boolean;
      controls: {
        origin: { setValue: (v: string) => void };
        destination: { setValue: (v: string) => void };
        departureDate: { setValue: (v: string) => void };
        cabinClass: { setValue: (v: string) => void };
      };
    };
  }

  function sameAirportSelected(c: SearchFormComponent): boolean {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return (c as any).sameAirportSelected();
  }

  it('renders one origin/destination option per configured airport (plus the disabled placeholder)', () => {
    const originOptions = fixture.nativeElement.querySelectorAll('#origin option');
    // +1 for the disabled "Select origin airport" placeholder.
    expect(originOptions.length).toBe(AIRPORTS.length + 1);
  });

  it('renders no passenger field at all — passenger count is determined during booking (PO decision 2026-07-07)', () => {
    expect(fixture.nativeElement.querySelector('#passengerCount')).toBeNull();
    expect(fixture.nativeElement.querySelector('label[for="passengerCount"]')).toBeNull();
    const labelTexts = Array.from(fixture.nativeElement.querySelectorAll('label')).map(
      (el) => (el as HTMLElement).textContent ?? '',
    );
    expect(labelTexts.some((text) => text.includes('Passengers'))).toBe(false);
  });

  it('always submits SearchRequest.passengerCount as the numeric constant 1 (PO decision 2026-07-07)', async () => {
    fakeSearchState.search.mockResolvedValue('success');
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('JFK');
    form(component).controls.departureDate.setValue('2026-08-01');

    await component.onSubmit();

    expect(fakeSearchState.search).toHaveBeenCalledTimes(1);
    // Strict matching: 1 (number) would NOT match '1' (string), so this pins the type too.
    expect(fakeSearchState.search).toHaveBeenCalledWith(expect.objectContaining({ passengerCount: 1 }));
  });

  it('renders the fixed cabin class options', () => {
    const options: NodeListOf<HTMLOptionElement> = fixture.nativeElement.querySelectorAll('#cabinClass option');
    const values = Array.from(options).map((o) => o.value);
    expect(values).toEqual(['Economy', 'Business', 'First Class']);
  });

  // ── A11Y-007/A11Y-008: submit is never natively disabled; invalid submits surface alerts ──

  it('never natively disables the submit button, even while the form is invalid (A11Y-008)', () => {
    expect(form(component).invalid).toBe(true);
    expect(submitButton().hasAttribute('disabled')).toBe(false);
    expect(submitButton().getAttribute('aria-disabled')).toBeNull(); // only in-flight sets it
  });

  it('surfaces the required-field alerts and focuses the first invalid control when an empty form is submitted (A11Y-008)', () => {
    submitForm();

    expect(alertTexts()).toContain('Origin airport is required.');
    expect(alertTexts()).toContain('Destination airport is required.');
    expect(alertTexts()).toContain('Departure date is required and cannot be in the past.');
    expect(fakeSearchState.search).not.toHaveBeenCalled();
    expect(document.activeElement?.id).toBe('origin');
    expect(document.activeElement).not.toBe(document.body);
  });

  it('surfaces the same-airport alert on submit instead of silently blocking Search (US-001 AC8, A11Y-008)', () => {
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('LHR');
    form(component).controls.departureDate.setValue('2026-08-01');
    fixture.detectChanges();

    expect(sameAirportSelected(component)).toBe(true);
    expect(submitButton().hasAttribute('disabled')).toBe(false); // the submit path is reachable

    submitForm();

    expect(alertTexts()).toContain('Origin and destination airports must be different.');
    expect(fakeSearchState.search).not.toHaveBeenCalled();
    expect(document.activeElement?.id).toBe('destination'); // group-level rule → destination select
  });

  it('clears the same-airport alert and submits once the destination is corrected (PO-reported regression 2026-07-07)', async () => {
    // Regression: sameAirportSelected was a computed() over raw (non-signal) FormControl
    // values — zero signal dependencies meant it cached `true` forever after the first
    // same-airport submit, permanently blocking Search even after the user fixed the field.
    fakeSearchState.search.mockResolvedValue('success');
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('LHR');
    form(component).controls.departureDate.setValue('2026-08-01');
    fixture.detectChanges();

    submitForm();
    expect(alertTexts()).toContain('Origin and destination airports must be different.');
    expect(fakeSearchState.search).not.toHaveBeenCalled();

    // User corrects the destination — the stale computed must re-evaluate.
    form(component).controls.destination.setValue('JFK');
    fixture.detectChanges();

    expect(sameAirportSelected(component)).toBe(false);
    expect(alertTexts()).not.toContain('Origin and destination airports must be different.');

    await component.onSubmit();
    expect(fakeSearchState.search).toHaveBeenCalledTimes(1);
  });

  it('marks the in-flight state with aria-disabled, never the native disabled attribute (A11Y-007)', () => {
    fakeSearchState.loading.set(true);
    fixture.detectChanges();

    const button = submitButton();
    expect(button.hasAttribute('disabled')).toBe(false); // focus must survive (§B.4 pattern)
    expect(button.getAttribute('aria-disabled')).toBe('true');
    expect(button.textContent).toContain('Searching…');
  });

  it('no-ops a re-entrant submit while a search is already in flight (A11Y-007 click guard)', async () => {
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('JFK');
    form(component).controls.departureDate.setValue('2026-08-01');
    fakeSearchState.loading.set(true);

    await component.onSubmit();

    expect(fakeSearchState.search).not.toHaveBeenCalled();
  });

  it('keeps focus on the Search button (never <body>) through submit and a failed search (A11Y-007)', async () => {
    fakeSearchState.search.mockImplementation(async () => {
      fakeSearchState.loading.set(true);
      fixture.detectChanges(); // render the in-flight state, as the browser would mid-request
      fakeSearchState.loading.set(false);
      fakeSearchState.errorMessage.set("We couldn't complete your search. Please try again.");
      return 'error';
    });
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('JFK');
    form(component).controls.departureDate.setValue('2026-08-01');
    fixture.detectChanges();

    const button = submitButton();
    button.focus();
    submitForm();
    await new Promise((resolve) => setTimeout(resolve, 0)); // flush onSubmit's await chain
    fixture.detectChanges();

    expect(fakeSearchState.search).toHaveBeenCalledTimes(1);
    expect(alertTexts()).toContain("We couldn't complete your search. Please try again.");
    // No navigation happened, so nothing recovers focus for us — the button must have kept it.
    expect(document.activeElement).toBe(button);
    expect(document.activeElement).not.toBe(document.body);
  });

  it('does not call SearchStateService.search when the form is invalid on submit', async () => {
    await component.onSubmit();

    expect(fakeSearchState.search).not.toHaveBeenCalled();
  });

  it('calls SearchStateService.search with a OneWay SearchRequest built from the form on a valid submit', async () => {
    fakeSearchState.search.mockResolvedValue('success');
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('JFK');
    form(component).controls.departureDate.setValue('2026-08-01');
    form(component).controls.cabinClass.setValue('Business');

    await component.onSubmit();

    expect(fakeSearchState.search).toHaveBeenCalledTimes(1);
    expect(fakeSearchState.search).toHaveBeenCalledWith({
      origin: 'LHR',
      destination: 'JFK',
      departureDate: '2026-08-01',
      passengerCount: 1,
      cabinClass: 'Business',
      tripType: 'OneWay',
    });
  });

  // ── AUD-008: "Modify search" pre-fills from the last criteria ────────────────
  it('AUD-008: pre-fills the form from lastCriteria on init (Modify search edits the prior query)', () => {
    fakeSearchState.lastCriteria.set({
      origin: 'LHR',
      destination: 'JFK',
      departureDate: '2026-08-01',
      passengerCount: 1,
      cabinClass: 'Business',
      tripType: 'OneWay',
    });
    const fresh = TestBed.createComponent(SearchFormComponent);
    fresh.detectChanges();
    const root: HTMLElement = fresh.nativeElement;

    expect(root.querySelector<HTMLSelectElement>('#origin')!.value).toBe('LHR');
    expect(root.querySelector<HTMLSelectElement>('#destination')!.value).toBe('JFK');
    expect(root.querySelector<HTMLInputElement>('#departureDate')!.value).toBe('2026-08-01');
    expect(root.querySelector<HTMLSelectElement>('#cabinClass')!.value).toBe('Business');
  });

  it('AUD-008: leaves the form blank when there is no prior search', () => {
    // Default fake has lastCriteria = null (set in beforeEach); the beforeEach fixture is blank.
    expect(fixture.nativeElement.querySelector('#origin').value).toBe('');
    expect(fixture.nativeElement.querySelector('#departureDate').value).toBe('');
  });

  // ── AUD-002: local-date min ─────────────────────────────────────────────────
  it('AUD-002: the departure-date min is the LOCAL today, not a UTC toISOString() date', () => {
    const now = new Date();
    const expected = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
    expect(fixture.nativeElement.querySelector('#departureDate').getAttribute('min')).toBe(expected);
  });

  // ── AUD-010: reactive past-date validator ───────────────────────────────────
  it('AUD-010: a typed past departure date fails client validation and does not call search', () => {
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('JFK');
    form(component).controls.departureDate.setValue('2000-01-01'); // clearly in the past
    fixture.detectChanges();

    submitForm();

    expect(alertTexts()).toContain('Departure date is required and cannot be in the past.');
    expect(fakeSearchState.search).not.toHaveBeenCalled();
  });

  // ── AUD-018: errors are programmatically associated with their controls ─────
  it('AUD-018: an invalid submitted control exposes aria-invalid and aria-describedby to its error', () => {
    submitForm();

    const origin: HTMLSelectElement = fixture.nativeElement.querySelector('#origin');
    expect(origin.getAttribute('aria-invalid')).toBe('true');
    expect(origin.getAttribute('aria-describedby')).toBe('origin-error');
    expect(fixture.nativeElement.querySelector('#origin-error')).toBeTruthy();
  });
});
