import { CanDeactivateFn } from '@angular/router';
import { BookingFormComponent } from './booking-form/booking-form.component';

/**
 * DESIGN-FLOW-001 §B.11 — router leg of the navigation guard against multi-passenger data
 * loss. Armed/disarmed logic (and the exact confirm() wording) lives in
 * BookingFormComponent.canLeave(): the guard never fires after a successful confirmation,
 * on the post-201 navigate to /confirmation, or when nothing has been entered (S0).
 *
 * Registration (one line, app.routes.ts `booking` route): `canDeactivate: [bookingLeaveGuard]`.
 * The `window:beforeunload` leg (tab close/refresh) is a host listener on the component.
 */
export const bookingLeaveGuard: CanDeactivateFn<BookingFormComponent> = (component) => component.canLeave();
