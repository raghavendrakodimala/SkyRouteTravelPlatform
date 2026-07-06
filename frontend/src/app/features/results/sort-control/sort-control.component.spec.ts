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

  it('marks the button matching the active input with aria-pressed="true" and the active class', async () => {
    await setActive('priceAsc');

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.sort-option');
    const activeButtons = Array.from(buttons).filter((btn) => btn.getAttribute('aria-pressed') === 'true');
    expect(activeButtons.length).toBe(1);
    expect(activeButtons[0].classList.contains('active')).toBe(true);
    expect(activeButtons[0].textContent).toContain('Price: low to high');
  });

  it('updates the active indication when a different option becomes active', async () => {
    await setActive('priceAsc');
    await setActive('durationAsc');

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.sort-option');
    const activeButtons = Array.from(buttons).filter((btn) => btn.getAttribute('aria-pressed') === 'true');
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
});
