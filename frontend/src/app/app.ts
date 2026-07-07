import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RouteFocusService } from './core/services/route-focus.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  // A11Y-001 fix (docs/reviews/accessibility-review-phase-17.md, NFR-A11Y-006): starts the
  // centralized route-focus-management subscription for the lifetime of the app shell.
  private readonly routeFocusService = inject(RouteFocusService);

  constructor() {
    this.routeFocusService.start();
  }
}
