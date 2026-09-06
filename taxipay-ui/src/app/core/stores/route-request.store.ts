import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { CreatePassengerRouteRequestDto, PassengerRouteRequestDto } from '../models/passenger.models';
import { PassengerRouteRequestNotificationDto } from '../models/realtime.models';
import { PassengerService } from '../services/passenger.service';
import { AuthService } from '../services/auth.service';
import { LiveSyncService } from '../services/live-sync.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface RouteRequestState {
  requests: PassengerRouteRequestDto[];
  isLoading: boolean;
  isSubmitting: boolean;
  error: string | null;
}

const initialRouteRequestState: RouteRequestState = {
  requests: [],
  isLoading: false,
  isSubmitting: false,
  error: null
};

export const RouteRequestStore = signalStore(
  { providedIn: 'root' },
  withState(initialRouteRequestState),
  withComputed((store) => ({
    pendingCount: computed(() => store.requests().filter(r => r.status.toLowerCase() === 'pending').length),
    approvedCount: computed(() => store.requests().filter(r => r.status.toLowerCase() === 'approved').length),
    rejectedCount: computed(() => store.requests().filter(r => r.status.toLowerCase() === 'rejected').length),
    totalCount: computed(() => store.requests().length)
  })),
  withMethods((
    store,
    passengerService = inject(PassengerService),
    authService = inject(AuthService),
    snackBar = inject(MatSnackBar)
  ) => ({
    loadPassengerRequests(passengerId?: string): void {
      const pid = passengerId || authService.currentUser()?.id;
      if (!pid) return;

      patchState(store, { isLoading: true, error: null });

      passengerService.getRouteRequests(pid).pipe(
        tap({
          next: requests => {
            patchState(store, { requests: requests || [], isLoading: false });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load route requests';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    },

    submitRequest(passengerId: string, req: CreatePassengerRouteRequestDto): void {
      patchState(store, { isSubmitting: true, error: null });

      passengerService.submitRouteRequest(passengerId, req).pipe(
        tap({
          next: newReq => {
            patchState(store, {
              requests: [newReq, ...store.requests()],
              isSubmitting: false
            });
            snackBar.open('Route suggestion submitted successfully! 🚀', 'OK', {
              duration: 3500,
              panelClass: 'snack-success'
            });
          },
          error: err => {
            patchState(store, { isSubmitting: false });
            const msg = err?.error?.detail || err?.error?.message || 'Failed to submit route request.';
            snackBar.open(msg, 'Dismiss', { duration: 4000 });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    },

    handleRequestReviewed(notification: PassengerRouteRequestNotificationDto): void {
      const updated = store.requests().map(r =>
        r.id === notification.requestId
          ? { ...r, status: notification.status, adminNotes: notification.adminNote || r.adminNotes }
          : r
      );
      patchState(store, { requests: updated });
      snackBar.open(
        `📬 Your route request status has been updated to ${notification.status}!`,
        'OK',
        { duration: 5000, panelClass: notification.status === 'Approved' ? 'snack-success' : 'snack-warning' }
      );
    },

    handleRequestSubmitted(request: PassengerRouteRequestDto): void {
      const exists = store.requests().some(r => r.id === request.id);
      if (!exists) {
        patchState(store, { requests: [request, ...store.requests()] });
      }
    }
  })),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      liveSync.passengerRouteRequestReviewed$.subscribe(notif => {
        store.handleRequestReviewed(notif);
      });

      liveSync.passengerRouteRequestSubmitted$.subscribe(req => {
        store.handleRequestSubmitted(req);
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadPassengerRequests();
      });
    }
  })
);
