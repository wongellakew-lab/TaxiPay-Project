import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { AdminDriverListItem, RouteDetail } from '../models/admin.models';
import { DriverStatusUpdateDto } from '../models/realtime.models';
import { AdminService } from '../services/admin.service';
import { LiveSyncService } from '../services/live-sync.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface DriverManagementState {
  drivers: AdminDriverListItem[];
  routes: RouteDetail[];
  selectedStatus: string;
  searchQuery: string;
  sortColumn: string;
  sortDirection: 'asc' | 'desc';
  isLoading: boolean;
  isActionLoading: boolean;
  error: string | null;
}

const initialDriverManagementState: DriverManagementState = {
  drivers: [],
  routes: [],
  selectedStatus: 'all',
  searchQuery: '',
  sortColumn: 'date',
  sortDirection: 'desc',
  isLoading: false,
  isActionLoading: false,
  error: null
};

export const DriverManagementStore = signalStore(
  { providedIn: 'root' },
  withState(initialDriverManagementState),
  withComputed((store) => ({
    totalDrivers: computed(() => store.drivers().length),
    pendingApprovalCount: computed(() =>
      store.drivers().filter(d => d.status.toLowerCase() === 'pending' || d.status.toLowerCase() === 'submitted').length
    ),
    activeDriverCount: computed(() =>
      store.drivers().filter(d => d.status.toLowerCase() === 'approved' || d.status.toLowerCase() === 'active').length
    ),
    suspendedCount: computed(() =>
      store.drivers().filter(d => d.status.toLowerCase() === 'suspended' || d.status.toLowerCase() === 'rejected').length
    ),
    filteredDrivers: computed(() => {
      let list = [...store.drivers()];
      const status = store.selectedStatus();
      const query = store.searchQuery().trim().toLowerCase();

      if (status !== 'all') {
        list = list.filter(d => d.status.toLowerCase() === status.toLowerCase());
      }

      if (query) {
        list = list.filter(d =>
          d.firstName.toLowerCase().includes(query) ||
          d.lastName.toLowerCase().includes(query) ||
          d.phone.includes(query) ||
          d.licenseNumber.toLowerCase().includes(query) ||
          (d.driverId && d.driverId.toLowerCase().includes(query))
        );
      }

      const col = store.sortColumn();
      const dir = store.sortDirection() === 'asc' ? 1 : -1;

      list.sort((a, b) => {
        let comparison = 0;
        switch (col) {
          case 'name':
            comparison = `${a.firstName} ${a.lastName}`.localeCompare(`${b.firstName} ${b.lastName}`);
            break;
          case 'phone':
            comparison = a.phone.localeCompare(b.phone);
            break;
          case 'license':
            comparison = a.licenseNumber.localeCompare(b.licenseNumber);
            break;
          case 'route':
            const rA = a.currentRouteCode || a.preferredRouteCode || '';
            const rB = b.currentRouteCode || b.preferredRouteCode || '';
            comparison = rA.localeCompare(rB);
            break;
          case 'performance':
            comparison = (a.totalTrips || 0) - (b.totalTrips || 0);
            break;
          case 'status':
            comparison = a.status.localeCompare(b.status);
            break;
          case 'date':
          default:
            comparison = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
            break;
        }
        return dir === 1 ? comparison : -comparison;
      });

      return list;
    })
  })),
  withMethods((
    store,
    adminService = inject(AdminService),
    snackBar = inject(MatSnackBar)
  ) => {
    const loadDrivers = (): void => {
      patchState(store, { isLoading: true, error: null });

      adminService.getDrivers().pipe(
        tap({
          next: drivers => {
            patchState(store, { drivers: drivers || [], isLoading: false });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load drivers';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const loadRoutes = (): void => {
      adminService.getRoutes().pipe(
        tap({
          next: routes => {
            patchState(store, { routes: routes || [] });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    return {
      loadDrivers,
      loadRoutes,

      setStatusFilter(status: string): void {
        patchState(store, { selectedStatus: status });
      },

      setSearchQuery(query: string): void {
        patchState(store, { searchQuery: query });
      },

      setSort(column: string): void {
        if (store.sortColumn() === column) {
          patchState(store, {
            sortDirection: store.sortDirection() === 'asc' ? 'desc' : 'asc'
          });
        } else {
          patchState(store, {
            sortColumn: column,
            sortDirection: 'asc'
          });
        }
      },

      approveDriver(driverId: string, assignedRouteId?: string): void {
        patchState(store, { isActionLoading: true });

        adminService.approveDriver(driverId, assignedRouteId).pipe(
          tap({
            next: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Driver accredited & activated! 🚕', 'OK', {
                duration: 3500,
                panelClass: 'snack-success'
              });
              loadDrivers();
            },
            error: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Failed to approve driver.', 'Dismiss', { duration: 3500 });
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      rejectDriver(driverId: string, reason: string): void {
        patchState(store, { isActionLoading: true });

        adminService.rejectDriver(driverId, reason).pipe(
          tap({
            next: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Driver rejected.', 'Dismiss', { duration: 3500 });
              loadDrivers();
            },
            error: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Failed to reject driver.', 'Dismiss', { duration: 3500 });
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      updateDriverStatus(driverId: string, status: string, note?: string): void {
        adminService.updateDriverStatus(driverId, status, note).pipe(
          tap({
            next: () => {
              snackBar.open(`Driver status updated to ${status}.`, 'Dismiss', { duration: 3500 });
              loadDrivers();
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      assignRoute(driverId: string, routeId: string): void {
        adminService.assignDriverRoute(driverId, routeId).pipe(
          tap({
            next: () => {
              snackBar.open('Route assigned successfully.', 'OK', { duration: 3500, panelClass: 'snack-success' });
              loadDrivers();
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      handleDriverStatusUpdated(statusUpdate: DriverStatusUpdateDto): void {
        const updated = store.drivers().map(d =>
          d.id === statusUpdate.driverId ? { ...d, status: statusUpdate.status } : d
        );
        patchState(store, { drivers: updated });
      }
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      liveSync.driverStatusUpdated$.subscribe(status => {
        store.handleDriverStatusUpdated(status);
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadDrivers();
      });
    }
  })
);
