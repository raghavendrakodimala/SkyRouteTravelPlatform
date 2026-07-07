import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FlightResult } from '../../shared/models/flight-result.model';
import { SearchRequest } from '../../shared/models/search-request.model';
import { mapHttpError } from '../../shared/utils/http-error.util';

/**
 * The sole place HttpClient is injected for search (DP-010, DP-PROTOCOL-006). No component
 * injects HttpClient directly. Errors are mapped to the shared ApiError shape at this HTTP
 * boundary (docs/features/feature-error-handling-and-validation.md Section 4) before ever
 * reaching a consumer.
 */
@Injectable({ providedIn: 'root' })
export class FlightSearchService {
  private readonly http = inject(HttpClient);

  search(request: SearchRequest): Observable<FlightResult[]> {
    return this.http.post<FlightResult[]>(`${environment.apiBaseUrl}/search`, request).pipe(
      catchError((error: HttpErrorResponse) =>
        throwError(() => mapHttpError(error, "We couldn't complete your search. Please try again.")),
      ),
    );
  }
}
