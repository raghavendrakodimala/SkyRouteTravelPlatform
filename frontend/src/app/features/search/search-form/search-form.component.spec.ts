import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AIRPORTS } from '../../../shared/constants/airports.constants';
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
    search: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    fakeSearchState = {
      loading: signal(false),
      fieldErrors: signal(null),
      errorMessage: signal(null),
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
        passengerCount: { setValue: (v: number) => void };
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

  it('renders the fixed passenger count options (1 through 9)', () => {
    const options: NodeListOf<HTMLOptionElement> = fixture.nativeElement.querySelectorAll('#passengerCount option');
    const values = Array.from(options).map((o) => o.value);
    expect(values).toEqual(['1', '2', '3', '4', '5', '6', '7', '8', '9']);
  });

  it('renders the fixed cabin class options', () => {
    const options: NodeListOf<HTMLOptionElement> = fixture.nativeElement.querySelectorAll('#cabinClass option');
    const values = Array.from(options).map((o) => o.value);
    expect(values).toEqual(['Economy', 'Business', 'First Class']);
  });

  it('disables the submit button while required fields (origin/destination/departureDate) are empty', () => {
    expect(form(component).invalid).toBe(true);
    expect(submitButton().disabled).toBe(true);
  });

  it('enables the submit button once all required fields are validly filled with different airports', () => {
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('JFK');
    form(component).controls.departureDate.setValue('2026-08-01');
    fixture.detectChanges();

    expect(submitButton().disabled).toBe(false);
  });

  it('disables the submit button when origin and destination are the same airport (US-001 AC8)', () => {
    form(component).controls.origin.setValue('LHR');
    form(component).controls.destination.setValue('LHR');
    form(component).controls.departureDate.setValue('2026-08-01');
    fixture.detectChanges();

    expect(sameAirportSelected(component)).toBe(true);
    expect(submitButton().disabled).toBe(true);
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
    form(component).controls.passengerCount.setValue(2);
    form(component).controls.cabinClass.setValue('Business');

    await component.onSubmit();

    expect(fakeSearchState.search).toHaveBeenCalledTimes(1);
    expect(fakeSearchState.search).toHaveBeenCalledWith({
      origin: 'LHR',
      destination: 'JFK',
      departureDate: '2026-08-01',
      passengerCount: 2,
      cabinClass: 'Business',
      tripType: 'OneWay',
    });
  });
});
