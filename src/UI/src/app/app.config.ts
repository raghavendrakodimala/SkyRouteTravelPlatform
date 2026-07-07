import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // provideHttpClient() is the only place HttpClient is configured for injection — every
    // HTTP call in the app flows through FlightSearchService/BookingService (DP-010).
    provideHttpClient(),
  ],
};
