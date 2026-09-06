import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { AdminDispute } from '../models/admin.models';
import { AdminService } from '../services/admin.service';
import { LiveSyncService } from '../services/live-sync.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface DisputeState {
  disputes: AdminDispute[];
  selectedStatus: 'all' | 'open' | 'closed' | string;
  searchQuery: string;
  sortColumn: string;
  sortDirection: 'asc' | 'desc';
  isLoading: boolean;
  isActionLoading: boolean;
  error: string | null;
}

const initialDisputeState: DisputeState = {
  disputes: [],
  selectedStatus: 'all',
  searchQuery: '',
  sortColumn: 'date',
  sortDirection: 'desc',
  isLoading: false,
  isActionLoading: false,
  error: null
};

export const DisputeStore = signalStore(
  { providedIn: 'root' },
  withState(initialDisputeState),
  withComputed((store) => ({
    totalCount: computed(() => store.disputes().length),
    openCount: computed(() =>
      store.disputes().filter(d => {
        const s = (d.status || '').toLowerCase();
        return s === 'open' || s === 'reviewing';
      }).length
    ),
    closedCount: computed(() =>
      store.disputes().filter(d => {
        const s = (d.status || '').toLowerCase();
        return s === 'closed' || s === 'resolved' || s === 'rejected';
      }).length
    ),
    filteredDisputes: computed(() => {
      let list = [...store.disputes()];
      const status = store.selectedStatus().toLowerCase();
      const query = store.searchQuery().trim().toLowerCase();

      if (status === 'open') {
        list = list.filter(d => {
          const s = (d.status || '').toLowerCase();
          return s === 'open' || s === 'reviewing';
        });
      } else if (status === 'closed') {
        list = list.filter(d => {
          const s = (d.status || '').toLowerCase();
          return s === 'closed' || s === 'resolved' || s === 'rejected';
        });
      }

      if (query) {
        list = list.filter(d =>
          d.tripNumber.toLowerCase().includes(query) ||
          d.passengerName.toLowerCase().includes(query) ||
          d.passengerPhone.includes(query) ||
          d.driverName.toLowerCase().includes(query) ||
          d.driverPhone.includes(query) ||
          d.routeCode.toLowerCase().includes(query) ||
          d.disputeType.toLowerCase().includes(query) ||
          d.description.toLowerCase().includes(query)
        );
      }

      const col = store.sortColumn();
      const dir = store.sortDirection() === 'asc' ? 1 : -1;

      list.sort((a, b) => {
        let comparison = 0;
        switch (col) {
          case 'passenger':
            comparison = a.passengerName.localeCompare(b.passengerName);
            break;
          case 'driver':
            comparison = a.driverName.localeCompare(b.driverName);
            break;
          case 'route':
            comparison = a.routeCode.localeCompare(b.routeCode);
            break;
          case 'type':
            comparison = a.disputeType.localeCompare(b.disputeType);
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
    const loadDisputes = (): void => {
      patchState(store, { isLoading: true, error: null });

      adminService.getDisputes().pipe(
        tap({
          next: disputes => {
            patchState(store, { disputes: disputes || [], isLoading: false });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load disputes';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    return {
      loadDisputes,

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

      openDispute(disputeId: string, notes?: string): void {
        patchState(store, { isActionLoading: true });

        adminService.openDispute(disputeId, notes).pipe(
          tap({
            next: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Dispute case reopened for arbitration.', 'OK', {
                duration: 3500,
                panelClass: 'snack-info'
              });
              loadDisputes();
            },
            error: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Failed to reopen dispute.', 'Dismiss', { duration: 3500 });
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      closeDispute(disputeId: string, resolutionNotes: string, issueRefund = false): void {
        patchState(store, { isActionLoading: true });

        adminService.closeDispute(disputeId, resolutionNotes, issueRefund).pipe(
          tap({
            next: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Dispute case closed and resolved! ⚖️', 'OK', {
                duration: 3500,
                panelClass: 'snack-success'
              });
              loadDisputes();
            },
            error: () => {
              patchState(store, { isActionLoading: false });
              snackBar.open('Failed to close dispute.', 'Dismiss', { duration: 3500 });
            }
          }),
          catchError(() => of(null))
        ).subscribe();
      },

      handleDisputeUpdated(dispute: AdminDispute): void {
        const current = store.disputes();
        const index = current.findIndex(d => d.id === dispute.id);
        if (index >= 0) {
          const updated = [...current];
          updated[index] = { ...current[index], ...dispute };
          patchState(store, { disputes: updated });
        } else {
          patchState(store, { disputes: [dispute, ...current] });
        }
      }
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      liveSync.disputeUpdated$.subscribe(dispute => {
        store.handleDisputeUpdated(dispute);
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadDisputes();
      });
    }
  })
);
