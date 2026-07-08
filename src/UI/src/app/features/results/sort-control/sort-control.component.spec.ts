import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { SortOption } from '../../../shared/utils/sort-flights.util';
import { SortControlComponent } from './sort-control.component';

describe('SortControlComponent', () => {
  let fixture: ComponentFixture<SortControlComponent>;
  let component: SortControlComponent;

  async function setActive(option: SortOption): Promise<void> {
    fixture.componentRef.setInput('active', option);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SortControlComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SortControlComponent);
    component = fixture.componentInstance;
  });

  it('renders one button per sort option', async () => {
    await setActive('priceAsc');

    const buttons = fixture.nativeElement.querySelectorAll('button.sort-option');
    expect(buttons.length).toBe(4);
  });

  // AUD-022: the control is an ARIA radiogroup (single-select), not aria-pressed toggles.
  it('is a radiogroup and marks the active option with role=radio + aria-checked + roving tabindex', async () => {
    await setActive('priceAsc');

    const group = fixture.nativeElement.querySelector('.sort-control');
    expect(group.getAttribute('role')).toBe('radiogroup');

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.sort-option');
    Array.from(buttons).forEach((btn) => expect(btn.getAttribute('role')).toBe('radio'));

    const checked = Array.from(buttons).filter((btn) => btn.getAttribute('aria-checked') === 'true');
    expect(checked.length).toBe(1);
    expect(checked[0].classList.contains('active')).toBe(true);
    expect(checked[0].textContent).toContain('Price: low to high');
    // Roving tabindex: only the checked option is tab-reachable.
    expect(checked[0].getAttribute('tabindex')).toBe('0');
    expect(Array.from(buttons).filter((btn) => btn.getAttribute('tabindex') === '-1').length).toBe(3);
  });

  it('updates the active indication when a different option becomes active', async () => {
    await setActive('priceAsc');
    await setActive('durationAsc');

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.sort-option');
    const activeButtons = Array.from(buttons).filter((btn) => btn.getAttribute('aria-checked') === 'true');
    expect(activeButtons.length).toBe(1);
    expect(activeButtons[0].textContent).toContain('Duration: shortest first');
  });

  it('emits optionSelected with the clicked option when select() is invoked', async () => {
    await setActive('priceAsc');
    let emitted: SortOption | undefined;
    component.optionSelected.subscribe((option) => (emitted = option));

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.sort-option');
    // Second button corresponds to 'priceDesc' per the fixed OPTIONS order.
    buttons[1].click();

    expect(emitted).toBe('priceDesc');
  });

  // AUD-022: radio-group keyboard pattern — Arrow keys move + select.
  it('ArrowRight on the active radio selects the next option (radio-group keyboard pattern)', async () => {
    await setActive('priceAsc');
    let emitted: SortOption | undefined;
    component.optionSelected.subscribe((option) => (emitted = option));

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.sort-option');
    buttons[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));

    expect(emitted).toBe('priceDesc');
  });

  it('ArrowLeft wraps from the first option to the last (radio-group keyboard pattern)', async () => {
    await setActive('priceAsc');
    let emitted: SortOption | undefined;
    component.optionSelected.subscribe((option) => (emitted = option));

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.sort-option');
    buttons[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));

    expect(emitted).toBe('departureAsc');
  });
});
