import { Router, UrlTree, provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BookingStateService } from '../../features/booking/booking-state.service';
import { SearchStateService } from '../../features/search/search-state.service';
import { hasBookingResponseGuard, hasSearchedGuard, hasSelectedFlightGuard } from './booking-flow.guards';

describe('booking-flow guards', () => {
  let fakeBookingState: {
    selectedFlight: ReturnType<typeof signal<unknown>>;
    bookingResponse: ReturnType<typeof signal<unknown>>;
  };
  let fakeSearchState: {
    hasSearched: ReturnType<typeof signal<boolean>>;
  };
  let router: Router;

  beforeEach(() => {
    fakeBookingState = {
      selectedFlight: signal<unknown>(null),
      bookingResponse: signal<unknown>(null),
    };
    fakeSearchState = {
      hasSearched: signal(false),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: BookingStateService, useValue: fakeBookingState },
        { provide: SearchStateService, useValue: fakeSearchState },
      ],
    });

    router = TestBed.inject(Router);
  });

  describe('hasSelectedFlightGuard', () => {
    it('returns true when a flight has been selected', () => {
      fakeBookingState.selectedFlight.set({ provider: 'GlobalAir' });

      const result = TestBed.runInInjectionContext(() =>
        hasSelectedFlightGuard({} as never, {} as never),
      );

      expect(result).toBe(true);
    });

    it('returns a UrlTree pointing at /search when no flight has been selected', () => {
      fakeBookingState.selectedFlight.set(null);
      const createUrlTreeSpy = vi.spyOn(router, 'createUrlTree');

      const result = TestBed.runInInjectionContext(() =>
        hasSelectedFlightGuard({} as never, {} as never),
      );

      expect(result).toBeInstanceOf(UrlTree);
      expect(createUrlTreeSpy).toHaveBeenCalledWith(['/search']);
      expect((result as UrlTree).toString()).toContain('/search');
    });
  });

  describe('hasBookingResponseGuard', () => {
    it('returns true when a booking response exists', () => {
      fakeBookingState.bookingResponse.set({ bookingReference: 'SR-000001' });

      const result = TestBed.runInInjectionContext(() =>
        hasBookingResponseGuard({} as never, {} as never),
      );

      expect(result).toBe(true);
    });

    it('returns a UrlTree pointing at /search when no booking response exists', () => {
      fakeBookingState.bookingResponse.set(null);
      const createUrlTreeSpy = vi.spyOn(router, 'createUrlTree');

      const result = TestBed.runInInjectionContext(() =>
        hasBookingResponseGuard({} as never, {} as never),
      );

      expect(result).toBeInstanceOf(UrlTree);
      expect(createUrlTreeSpy).toHaveBeenCalledWith(['/search']);
      expect((result as UrlTree).toString()).toContain('/search');
    });
  });

  // AUD-005: /results guard — redirect to /search on refresh/deep-link with no search in state.
  describe('hasSearchedGuard', () => {
    it('returns true when a search has been run', () => {
      fakeSearchState.hasSearched.set(true);

      const result = TestBed.runInInjectionContext(() => hasSearchedGuard({} as never, {} as never));

      expect(result).toBe(true);
    });

    it('returns a UrlTree pointing at /search when no search has been run', () => {
      fakeSearchState.hasSearched.set(false);
      const createUrlTreeSpy = vi.spyOn(router, 'createUrlTree');

      const result = TestBed.runInInjectionContext(() => hasSearchedGuard({} as never, {} as never));

      expect(result).toBeInstanceOf(UrlTree);
      expect(createUrlTreeSpy).toHaveBeenCalledWith(['/search']);
      expect((result as UrlTree).toString()).toContain('/search');
    });
  });
});
