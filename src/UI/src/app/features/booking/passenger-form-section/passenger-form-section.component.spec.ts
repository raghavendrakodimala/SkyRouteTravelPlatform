import { FormBuilder } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import {
  ageValidator,
  documentNumberValidator,
  emailFormatValidator,
  fullNameValidator,
} from '../../../shared/validators/document-number.validators';
import { PassengerFormSectionComponent } from './passenger-form-section.component';

describe('PassengerFormSectionComponent', () => {
  let fixture: ComponentFixture<PassengerFormSectionComponent>;
  let fb: FormBuilder;

  // Field order mirrors the template: 1 = fullName, 2 = age, 3 = email, 4 = documentNumber.
  function buildGroup(routeType: 'International' | 'Domestic' = 'International') {
    return fb.nonNullable.group({
      fullName: fb.nonNullable.control('', { validators: [fullNameValidator()] }),
      age: fb.control<number | null>(null, { validators: [ageValidator()] }),
      email: fb.nonNullable.control('', { validators: [emailFormatValidator()] }),
      documentNumber: fb.nonNullable.control('', { validators: [documentNumberValidator(routeType)] }),
    });
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PassengerFormSectionComponent],
    }).compileComponents();

    fb = TestBed.inject(FormBuilder);
    fixture = TestBed.createComponent(PassengerFormSectionComponent);
  });

  function render(overrides: {
    group?: ReturnType<typeof buildGroup>;
    index?: number;
    isLead?: boolean;
    documentLabel?: string;
    documentHint?: string;
  } = {}): void {
    fixture.componentRef.setInput('group', overrides.group ?? buildGroup());
    fixture.componentRef.setInput('index', overrides.index ?? 0);
    fixture.componentRef.setInput('isLead', overrides.isLead ?? false);
    fixture.componentRef.setInput('documentLabel', overrides.documentLabel ?? 'Passport Number');
    fixture.componentRef.setInput(
      'documentHint',
      overrides.documentHint ?? '6–9 characters, uppercase letters (A–Z) and digits only — e.g. A1234567',
    );
    fixture.detectChanges();
  }

  it('renders "Passport Number" as the document label when documentLabel input is "Passport Number"', () => {
    render({ documentLabel: 'Passport Number' });

    const label = fixture.nativeElement.querySelector('label[for="documentNumber-0"]');
    expect(label.textContent.trim()).toContain('Passport Number');
  });

  it('renders "National ID" as the document label when documentLabel input is "National ID"', () => {
    render({ documentLabel: 'National ID' });

    const label = fixture.nativeElement.querySelector('label[for="documentNumber-0"]');
    expect(label.textContent.trim()).toContain('National ID');
  });

  it('renders "Passenger {index + 1}" in the legend', () => {
    render({ index: 2 });

    const legend = fixture.nativeElement.querySelector('legend');
    expect(legend.textContent).toContain('Passenger 3');
  });

  it('renders the primary contact badge only when isLead is true', () => {
    render({ isLead: true });
    expect(fixture.nativeElement.querySelector('.lead-badge')).toBeTruthy();

    render({ isLead: false, index: 1 });
    expect(fixture.nativeElement.querySelector('.lead-badge')).toBeFalsy();
  });

  it('shows a fullName validation message once the control is touched and invalid', () => {
    const group = buildGroup();
    render({ group });

    expect(fixture.nativeElement.querySelector('.field:nth-of-type(1) .error')).toBeFalsy();

    group.controls.fullName.markAsTouched();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.field:nth-of-type(1) .error');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Full name is required');
  });

  it('shows an email validation message once the control is touched and invalid', () => {
    const group = buildGroup();
    render({ group });

    group.controls.email.setValue('not-an-email');
    group.controls.email.markAsTouched();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.field:nth-of-type(3) .error');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('valid email address is required');
  });

  it('shows a documentNumber validation message once the control is touched and invalid', () => {
    const group = buildGroup('International');
    render({ group, documentLabel: 'Passport Number' });

    group.controls.documentNumber.setValue('bad');
    group.controls.documentNumber.markAsTouched();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.field:nth-of-type(4) .error');
    expect(errorEl).toBeTruthy();
    // PO 2026-07-08: the error must state the expected format, not just "wrong format".
    expect(errorEl.textContent).toContain('Passport Number must be 6–9 characters, uppercase letters (A–Z) and digits only');
  });

  it('renders a persistent format hint under the document field, wired via aria-describedby (WCAG 3.3.2, PO 2026-07-08)', () => {
    render({ documentHint: '6–9 characters, uppercase letters (A–Z) and digits only — e.g. A1234567' });

    const hint = fixture.nativeElement.querySelector('#documentHint-0');
    expect(hint).toBeTruthy();
    expect(hint.classList.contains('hint')).toBe(true);
    expect(hint.textContent).toContain('e.g. A1234567');

    const input = fixture.nativeElement.querySelector('#documentNumber-0');
    expect(input.getAttribute('aria-describedby')).toBe('documentHint-0');

    // The hint is persistent — visible BEFORE any validation error exists.
    expect(fixture.nativeElement.querySelector('.field:nth-of-type(4) .error')).toBeNull();
  });

  it('renders the National ID hint when the domestic hint input is supplied', () => {
    render({
      documentLabel: 'National ID',
      documentHint: '5–20 characters, letters, digits and hyphens — e.g. ID-102-3345',
    });

    const hint = fixture.nativeElement.querySelector('#documentHint-0');
    expect(hint.textContent).toContain('ID-102-3345');
  });

  // ── Age field (PO age feature 2026-07-08; DEC-022: pure data capture, 0–120 sanity
  // bounds only — the former AGE-LEAD-18 lead-adult rule was removed) ──────────

  it('renders the Age field between Full Name and Email as a required, bounded number input', () => {
    render();

    const label = fixture.nativeElement.querySelector('label[for="age-0"]');
    expect(label.textContent.replace(/\s+/g, ' ').trim()).toBe('Age (required)');

    const input = fixture.nativeElement.querySelector('#age-0');
    expect(input).toBeTruthy();
    expect(input.getAttribute('type')).toBe('number');
    expect(input.getAttribute('inputmode')).toBe('numeric');
    expect(input.getAttribute('min')).toBe('0');
    expect(input.getAttribute('max')).toBe('120');
    expect(input.getAttribute('aria-required')).toBe('true');

    // Positional check: field 1 = fullName, field 2 = age, field 3 = email.
    expect(fixture.nativeElement.querySelector('.field:nth-of-type(2) #age-0')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.field:nth-of-type(3) #email-0')).toBeTruthy();
  });

  it('renders no age hint and no aria-describedby for any passenger, lead included (DEC-022 — no lead-adult rule)', () => {
    render({ isLead: true });
    expect(fixture.nativeElement.querySelector('#ageHint-0')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('#age-0').getAttribute('aria-describedby')).toBeNull();

    render({ isLead: false, index: 1 });
    expect(fixture.nativeElement.querySelector('#ageHint-1')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('#age-1').getAttribute('aria-describedby')).toBeNull();
  });

  it('shows the range message once the age control is touched and invalid', () => {
    const group = buildGroup();
    render({ group });

    group.controls.age.setValue(121);
    group.controls.age.markAsTouched();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.field:nth-of-type(2) .error');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Age is required and must be a whole number between 0 and 120.');
  });

  it('shows no error for a lead passenger under 18 — age is pure data capture (DEC-022)', () => {
    const group = buildGroup();
    render({ group, isLead: true });

    group.controls.age.setValue(15);
    group.controls.age.markAsTouched();
    fixture.detectChanges();

    expect(group.controls.age.valid).toBe(true);
    expect(fixture.nativeElement.querySelector('.field:nth-of-type(2) .error')).toBeNull();
  });

  it('renders a server-side age error message when serverErrors.age is supplied', () => {
    render();
    fixture.componentRef.setInput('serverErrors', { age: 'Age is required and must be a whole number between 0 and 120.' });
    fixture.detectChanges();

    const errors: string[] = Array.from<Element>(fixture.nativeElement.querySelectorAll('.error')).map(
      (el: Element) => el.textContent ?? '',
    );
    expect(errors.some((text) => text.includes('Age is required and must be a whole number between 0 and 120.'))).toBe(true);
  });

  it('renders a server-side error message for a field when serverErrors is supplied', () => {
    const group = buildGroup();
    render({ group });
    fixture.componentRef.setInput('serverErrors', { email: 'Email already used for another passenger.' });
    fixture.detectChanges();

    const errors: string[] = Array.from<Element>(fixture.nativeElement.querySelectorAll('.error')).map(
      (el: Element) => el.textContent ?? '',
    );
    expect(errors.some((text) => text.includes('Email already used for another passenger.'))).toBe(true);
  });

  it('does not show a validation message for an untouched, invalid control', () => {
    render();

    expect(fixture.nativeElement.querySelectorAll('.error').length).toBe(0);
  });

  it('sets autocomplete="name"/"email" on the lead passenger inputs (WCAG 1.3.5)', () => {
    render({ isLead: true });

    expect(fixture.nativeElement.querySelector('#fullName-0').getAttribute('autocomplete')).toBe('name');
    expect(fixture.nativeElement.querySelector('#email-0').getAttribute('autocomplete')).toBe('email');
  });

  it('omits autocomplete on non-lead passengers (other people — no wrong-person autofill)', () => {
    render({ isLead: false, index: 1 });

    expect(fixture.nativeElement.querySelector('#fullName-1').getAttribute('autocomplete')).toBeNull();
    expect(fixture.nativeElement.querySelector('#email-1').getAttribute('autocomplete')).toBeNull();
  });
});
