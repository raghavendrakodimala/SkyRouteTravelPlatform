import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, RouterOutlet, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { RouteFocusService } from './route-focus.service';

// Trivial stand-ins for two of the app's real routed screens, mirroring the "root element
// carries the page heading" shape used by SearchFormComponent/ResultsListComponent, etc.
@Component({ standalone: true, template: '<h1>Screen A</h1>' })
class ScreenAComponent {}

@Component({ standalone: true, template: '<h1>Screen B</h1>' })
class ScreenBComponent {}

// Stands in for app.html's `<main><router-outlet /></main>` shell — RouteFocusService searches
// for its focus target scoped to `main`, matching the real app shell structure.
@Component({ standalone: true, imports: [RouterOutlet], template: '<main><router-outlet /></main>' })
class HostComponent {}

describe('RouteFocusService', () => {
  let router: Router;
  let service: RouteFocusService;
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'a', component: ScreenAComponent },
          { path: 'b', component: ScreenBComponent },
        ]),
      ],
    });

    service = TestBed.inject(RouteFocusService);
    service.start();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();

    document.body.focus();
  });

  async function flushFocusTimer(): Promise<void> {
    // RouteFocusService defers the actual .focus() call with a macrotask so the routed
    // component has finished rendering first — flush one real macrotask to let it run.
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  it('does not move focus on the very first navigation (initial app bootstrap, not a screen transition)', async () => {
    await router.navigate(['/a']);
    fixture.detectChanges();
    await flushFocusTimer();

    expect(document.activeElement).toBe(document.body);
  });

  it("moves focus to the new screen's <h1> after a subsequent navigation (A11Y-001)", async () => {
    await router.navigate(['/a']);
    fixture.detectChanges();
    await flushFocusTimer();

    await router.navigate(['/b']);
    fixture.detectChanges();
    await flushFocusTimer();

    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading?.textContent).toBe('Screen B');
    expect(document.activeElement).toBe(heading);
    expect(heading?.getAttribute('tabindex')).toBe('-1');
  });

  it('does not leave focus on <body> after a screen transition (NFR-A11Y-006 target outcome)', async () => {
    await router.navigate(['/a']);
    fixture.detectChanges();
    await flushFocusTimer();

    await router.navigate(['/b']);
    fixture.detectChanges();
    await flushFocusTimer();

    expect(document.activeElement).not.toBe(document.body);
  });
});
