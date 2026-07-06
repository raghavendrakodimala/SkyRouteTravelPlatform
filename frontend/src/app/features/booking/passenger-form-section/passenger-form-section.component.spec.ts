import { FormBuilder } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { documentNumberValidator, emailFormatValidator, fullNameValidator } from '../../../shared/validators/document-number.validators';
import { PassengerFormSectionComponent } from './passenger-form-section.component';

describe('PassengerFormSectionComponent', () => {
  let fixture: ComponentFixture<PassengerFormSectionComponent>;
  let component: PassengerFormSectionComponent;
  let fb: FormBuilder;

  function buildGroup(routeType: 'International' | 'Domestic' = 'International') {
    return fb.nonNullable.group({
      fullName: fb.nonNullable.control('', { validators: [fullNameValidator()] }),
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
    component = fixture.componentInstance;
  });

  function render(overrides: {
    group?: ReturnType<typeof buildGroup>;
    index?: number;
    isLead?: boolean;
    documentLabel?: string;
  } = {}): void {
    fixture.componentRef.setInput('group', overrides.group ?? buildGroup());
    fixture.componentRef.setInput('index', overrides.index ?? 0);
    fixture.componentRef.setInput('isLead', overrides.isLead ?? false);
    fixture.componentRef.setInput('documentLabel', overrides.documentLabel ?? 'Passport Number');
    fixture.detectChanges();
  }

  it('renders "Passport Number" as the document label when documentLabel input is "Passport Number"', () => {
    render({ documentLabel: 'Passport Number' });

    const label = fixture.nativeElement.querySelector('label[for="documentNumber-0"]');
    expect(label.textContent.trim()).toBe('Passport Number');
  });

  it('renders "National ID" as the document label when documentLabel input is "National ID"', () => {
    render({ documentLabel: 'National ID' });

    const label = fixture.nativeElement.querySelector('label[for="documentNumber-0"]');
    expect(label.textContent.trim()).toBe('National ID');
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

    const errorEl = fixture.nativeElement.querySelector('.field:nth-of-type(2) .error');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('valid email address is required');
  });

  it('shows a documentNumber validation message once the control is touched and invalid', () => {
    const group = buildGroup('International');
    render({ group, documentLabel: 'Passport Number' });

    group.controls.documentNumber.setValue('bad');
    group.controls.documentNumber.markAsTouched();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.field:nth-of-type(3) .error');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Passport Number does not match the required format');
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
});
