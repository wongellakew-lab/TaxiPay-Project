import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { RouteChangeResponse } from '../models/driver.models';
import { RouteDetail } from '../models/admin.models';
import { RouteChangeNotificationDto } from '../models/realtime.models';
import { DriverService } from '../services/driver.service';
import { RouteService } from '../services/route.service';
import { AuthService } from '../services/auth.service';
import { LiveSyncService } from '../services/live-sync.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface DriverTransferRequestState {
  currentRequest: RouteChangeResponse | null;
  availableRoutes: RouteDetail[];
  currentRoute: RouteDetail | null;
  isLoading: boolean;
  isSubmitting: boolean;
  error: string | null;
}

const initialTransferState: DriverTransferRequestState = {
  currentRequest: null,
  availableRoutes: [],
  currentRoute: null,
  isLoading: false,
  isSubmitting: false,
  error: null
};

export const DriverTransferRequestStore = signalStore(
  { providedIn: 'root' },
  withState(initialTransferState),
  withComputed((store) => ({
    hasRequest: computed(() => !!store.currentRequest()),
    hasOngoingRequest: computed(() => store.currentRequest()?.status.toLowerCase() === 'pending'),
    isPending: computed(() => store.currentRequest()?.status.toLowerCase() === 'pending'),
    isApproved: computed(() => store.currentRequest()?.status.toLowerCase() === 'approved'),
    isRejected: computed(() => store.currentRequest()?.status.toLowerCase() === 'rejected'),
    requestedRouteName: computed(() => store.currentRequest()?.requestedRouteName || ''),
    rejectionReason: computed(() => store.currentRequest()?.rejectionReason || null)
  })),
  withMethods((
    store,
    driverService = inject(DriverService),
    routeService = inject(RouteService),
    authService = inject(AuthService),
    snackBar = inject(MatSnackBar)
  ) => {
    const loadCurrentRequest = (driverId?: string): void => {
      const user = authService.currentUser();
      const did = driverId || user?.id;
      if (!did) return;

      patchState(store, { isLoading: true, error: null });

      driverService.getCurrentRouteChange(did).pipe(
        tap({
          next: req => {
            patchState(store, { currentRequest: req, isLoading: false });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load transfer request';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const loadAvailableRoutes = (): void => {
      const user = authService.currentUser();
      const did = user?.id;

      routeService.getRoutes().pipe(
        tap({
          next: routes => {
            patchState(store, { availableRoutes: routes || [] });

            if (did) {
              driverService.getProfile(did).pipe(
                tap({
                  next: profile => {
                    if (routes && routes.length > 0) {
                      let matched = routes[0];
                      if (profile?.currentRouteId) {
                        const found = routes.find(r => r.id === profile.currentRouteId);
                        if (found) matched = found;
                      } else if (user?.driverData?.routeCode) {
                        const found = routes.find(r => r.routeCode === user.driverData?.routeCode);
                        if (found) matched = found;
                      }
                      patchState(store, { currentRoute: matched });
                    }
                  }
                }),
                catchError(() => of(null))
              ).subscribe();
            } else if (routes && routes.length > 0) {
              patchState(store, { currentRoute: routes[0] });
            }
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const submitTransferRequest = (
      requestedRouteId: string,
      reason: string,
      onSuccess?: () => void
    ): void => {
      const user = authService.currentUser();
      const did = user?.id;
      if (!did) {
        snackBar.open('Please sign in as a driver to submit a route transfer request.', 'Dismiss', { duration: 4000 });
        return;
      }

      if (store.hasOngoingRequest()) {
        snackBar.open('⚠️ You already have an ongoing transfer request. Wait for approval before submitting another.', 'Dismiss', {
          duration: 5000,
          panelClass: 'snack-warning'
        });
        return;
      }

      patchState(store, { isSubmitting: true, error: null });

      driverService.requestRouteChange(did, {
        newRouteId: requestedRouteId,
        requestedRouteId,
        reason: reason.trim()
      }).pipe(
        tap({
          next: res => {
            patchState(store, {
              currentRequest: res,
              isSubmitting: false
            });
            snackBar.open('✅ Route change request submitted for supervisor review!', 'OK', {
              duration: 4500,
              panelClass: 'snack-success'
            });
            if (onSuccess) onSuccess();
            loadCurrentRequest(did);
          },
          error: err => {
            patchState(store, { isSubmitting: false });
            const msg = err?.error?.detail || err?.error?.message || err?.error?.title || 'Could not submit route change request.';
            patchState(store, { error: msg });
            snackBar.open(msg, 'Dismiss', { duration: 5500, panelClass: 'snack-error' });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const handleRouteChangeReviewed = (update: RouteChangeNotificationDto): void => {
      const user = authService.currentUser();
      const current = store.currentRequest();
      if (update.approved) {
        const newRoute = store.availableRoutes().find(r => r.id === update.newRouteId);
        if (newRoute) {
          patchState(store, { currentRoute: newRoute });
        }
        patchState(store, {
          currentRequest: {
            requestId: current?.requestId || '',
            driverId: update.driverId,
            requestedRouteId: update.newRouteId,
            requestedRouteName: update.newRouteName || newRoute?.name || 'Assigned Corridor',
            status: 'Approved',
            requestedAt: current?.requestedAt || new Date().toISOString(),
            reviewedAt: new Date().toISOString()
          }
        });
        snackBar.open('🎉 Your corridor transfer request has been APPROVED by admin!', 'OK', {
          duration: 6000,
          panelClass: 'snack-success'
        });
      } else {
        patchState(store, {
          currentRequest: {
            requestId: current?.requestId || '',
            driverId: update.driverId,
            requestedRouteId: update.newRouteId,
            requestedRouteName: current?.requestedRouteName || 'Target Corridor',
            status: 'Rejected',
            rejectionReason: update.rejectionReason || update.note,
            requestedAt: current?.requestedAt || new Date().toISOString(),
            reviewedAt: new Date().toISOString()
          }
        });
        snackBar.open(`⚠️ Route transfer rejected: ${update.rejectionReason || update.note || 'Municipal transport regulation'}`, 'Dismiss', {
          duration: 6000,
          panelClass: 'snack-warning'
        });
      }

      if (user?.id) {
        loadCurrentRequest(user.id);
        loadAvailableRoutes();
      }
    };

    return {
      loadCurrentRequest,
      loadAvailableRoutes,
      submitTransferRequest,
      handleRouteChangeReviewed
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      store.loadCurrentRequest();
      store.loadAvailableRoutes();

      liveSync.routeChangeReviewed$.subscribe(update => {
        store.handleRouteChangeReviewed(update);
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadCurrentRequest();
        store.loadAvailableRoutes();
      });
    }
  })
);
