import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { AdminRouteChangeRequest } from '../models/admin.models';
import { RouteChangeNotificationDto } from '../models/realtime.models';
import { AdminService } from '../services/admin.service';
import { LiveSyncService } from '../services/live-sync.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface AdminRouteRequestState {
  requests: AdminRouteChangeRequest[];
  selectedStatus: 'all' | 'pending' | 'approved' | 'rejected' | string;
  searchQuery: string;
  isLoading: boolean;
  isActionLoading: boolean;
  error: string | null;
}

const initialAdminRouteRequestState: AdminRouteRequestState = {
  requests: [],
  selectedStatus: 'all',
  searchQuery: '',
  isLoading: false,
  isActionLoading: false,
  error: null
};

export const AdminRouteRequestStore = signalStore(
  { providedIn: 'root' },
  withState(initialAdminRouteRequestState),
  withComputed((store) => ({
    totalCount: computed(() => store.requests().length),
    pendingCount: computed(() =>
      store.requests().filter(r => r.status.toLowerCase() === 'pending').length
    ),
    approvedCount: computed(() =>
      store.requests().filter(r => r.status.toLowerCase() === 'approved').length
    ),
    rejectedCount: computed(() =>
      store.requests().filter(r => r.status.toLowerCase() === 'rejected').length
    ),
    filteredRequests: computed(() => {
      let list = [...store.requests()];
      const status = store.selectedStatus().toLowerCase();
      const query = store.searchQuery().trim().toLowerCase();

      if (status !== 'all') {
        list = list.filter(r => r.status.toLowerCase() === status);
      }

      if (query) {
        list = list.filter(r =>
          r.driverName.toLowerCase().includes(query) ||
          (r.driverCode && r.driverCode.toLowerCase().includes(query)) ||
          r.driverPhone.includes(query) ||
          r.currentRouteCode.toLowerCase().includes(query) ||
          r.currentRouteName.toLowerCase().includes(query) ||
          r.newRouteCode.toLowerCase().includes(query) ||
          r.newRouteName.toLowerCase().includes(query) ||
          r.reason.toLowerCase().includes(query)
        );
      }

      list.sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime());
      return list;
    })
  })),
  withMethods((
    store,
    adminService = inject(AdminService),
    snackBar = inject(MatSnackBar)
  ) => {
    const loadRequests = (): void => {
      patchState(store, { isLoading: true, error: null });

      adminService.getRouteChangeRequests().pipe(
        tap({
          next: requests => {
            patchState(store, { requests: requests || [], isLoading: false });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load route change requests';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    return {
      loadRequests,

      setStatusFilter(status: string): void {
        patchState(store, { selectedStatus: status });
      },

      setSearchQuery(query: string): void {
        patchState(store, { searchQuery: query });
      },

      approveRequest(requestId: string, note?: string): void {
        patchState(store, { isActionLoading: true });

        adminService.reviewRouteChangeRequest(requestId, true, note).pipe(
          tap({
            next: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Route change request approved! 🛣️', 'OK', {
                duration: 3500,
                panelClass: 'snack-success'
              });
              loadRequests();
            },
            error: err => {
              patchState(store, { isActionLoading: false });
              const msg = err?.error?.detail || err?.error?.message || 'Failed to approve request. Ensure driver has no active trip.';
              snackBar.open(msg, 'Dismiss', { duration: 4000 });
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      rejectRequest(requestId: string, reason: string): void {
        patchState(store, { isActionLoading: true });

        adminService.reviewRouteChangeRequest(requestId, false, reason).pipe(
          tap({
            next: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Route change request rejected.', 'Dismiss', { duration: 3500 });
              loadRequests();
            },
            error: err => {
              patchState(store, { isActionLoading: false });
              const msg = err?.error?.detail || err?.error?.message || 'Failed to reject request.';
              snackBar.open(msg, 'Dismiss', { duration: 4000 });
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      handleRouteChangeReviewed(update: RouteChangeNotificationDto): void {
        const updated = store.requests().map(r => {
          if (r.driverId === update.driverId && r.newRouteId === update.newRouteId) {
            return {
              ...r,
              status: update.approved ? 'Approved' : 'Rejected',
              rejectionReason: update.rejectionReason || update.note || r.rejectionReason,
              reviewedAt: new Date().toISOString()
            };
          }
          return r;
        });
        patchState(store, { requests: updated });
      },

      handleDriverRouteChangeSubmitted(req: AdminRouteChangeRequest): void {
        const exists = store.requests().some(r => r.id === req.id);
        if (!exists) {
          patchState(store, {
            requests: [req, ...store.requests()]
          });
          snackBar.open(`🛣️ New route transfer request submitted by ${req.driverName} (${req.newRouteCode})`, 'View', {
            duration: 5000,
            panelClass: 'snack-info'
          });
        }
      }
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      liveSync.routeChangeReviewed$.subscribe(update => {
        store.handleRouteChangeReviewed(update);
      });

      liveSync.driverRouteChangeSubmitted$.subscribe(req => {
        store.handleDriverRouteChangeSubmitted(req);
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadRequests();
      });
    }
  })
);
