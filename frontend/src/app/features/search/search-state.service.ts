import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiError, FieldErrors } from '../../shared/models/api-error.model';
import { FlightResult } from '../../shared/models/flight-result.model';
import { SearchRequest } from '../../shared/models/search-request.model';
import { FlightSearchService } from './flight-search.service';

export type SearchOutcome = 'success' | 'validation' | 'error';

/**
 * Signal-based shared state (DP-013, AD-006). firstValueFrom(...) inside search() is the
 * ONE Observable→Signal conversion point for the search data flow — everything downstream
 * (ResultsListComponent, SortControlComponent) reads only signals/computed(), never an
 * Observable or the async pipe, for this same data flow.
 */
@Injectable({ providedIn: 'root' })
export class SearchStateService {
  private readonly flightSearchService = inject(FlightSearchService);

  private readonly _results = signal<FlightResult[]>([]);
  private readonly _loading = signal(false);
  private readonly _errorMessage = signal<string | null>(null);
  private readonly _fieldErrors = signal<FieldErrors | null>(null);
  private readonly _lastCriteria = signal<SearchRequest | null>(null);
  private readonly _hasSearched = signal(false);

  readonly results = this._results.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();
  readonly fieldErrors = this._fieldErrors.asReadonly();
  readonly lastCriteria = this._lastCriteria.asReadonly();
  readonly hasSearched = this._hasSearched.asReadonly();

  /** US-002 AC6 — a legitimate 200 response with zero results, not an error. */
  readonly isEmpty = computed(
    () => this._hasSearched() && !this._loading() && !this._errorMessage() && this._results().length === 0,
  );

  async search(request: SearchRequest): Promise<SearchOutcome> {
    this._loading.set(true);
    this._errorMessage.set(null);
    this._fieldErrors.set(null);
    this._lastCriteria.set(request);

    try {
      const results = await firstValueFrom(this.flightSearchService.search(request));
      this._results.set(results);
      this._hasSearched.set(true);
      this._loading.set(false);
      return 'success';
    } catch (err) {
      this._loading.set(false);
      const apiError = err as ApiError;
      if (apiError.kind === 'validation') {
        this._fieldErrors.set(apiError.errors);
        return 'validation';
      }
      this._errorMessage.set(apiError.message);
      return 'error';
    }
  }
}
