import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { TripHistoryItemDto } from '../models/passenger.models';
import { PassengerService } from '../services/passenger.service';
import { AuthService } from '../services/auth.service';
import { LiveSyncService } from '../services/live-sync.service';
import { tap, catchError, of } from 'rxjs';

export interface RideHistoryState {
  trips: TripHistoryItemDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  searchTerm: string;
  filterStatus: string;
  filterDirection: string;
  sortColumn: string;
  sortDirection: 'asc' | 'desc';
  isLoading: boolean;
  error: string | null;
}

const initialRideHistoryState: RideHistoryState = {
  trips: [],
  totalCount: 0,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
  searchTerm: '',
  filterStatus: 'ALL',
  filterDirection: 'ALL',
  sortColumn: 'tripStart',
  sortDirection: 'desc',
  isLoading: false,
  error: null
};

export const RideHistoryStore = signalStore(
  { providedIn: 'root' },
  withState(initialRideHistoryState),
  withComputed((store) => ({
    filteredTrips: computed(() => {
      let list = [...store.trips()];
      const search = store.searchTerm().trim().toLowerCase();
      const status = store.filterStatus();
      const direction = store.filterDirection();

      if (search) {
        list = list.filter(t =>
          (t.routeName || '').toLowerCase().includes(search) ||
          (t.driverName || '').toLowerCase().includes(search) ||
          (t.driverId || '').toLowerCase().includes(search) ||
          (t.boardingNode || '').toLowerCase().includes(search) ||
          (t.destinationNode || '').toLowerCase().includes(search) ||
          (t.boardingPointName || '').toLowerCase().includes(search) ||
          (t.destinationPointName || '').toLowerCase().includes(search)
        );
      }

      if (status !== 'ALL') {
        list = list.filter(t => (t.paymentMethod || t.status || '').toUpperCase() === status.toUpperCase());
      }

      if (direction !== 'ALL') {
        list = list.filter(t => (t.direction || '').toUpperCase() === direction.toUpperCase());
      }

      // Sort
      const col = store.sortColumn();
      const dir = store.sortDirection() === 'asc' ? 1 : -1;
      list.sort((a: any, b: any) => {
        const valA = a[col] ?? '';
        const valB = b[col] ?? '';
        if (valA < valB) return -1 * dir;
        if (valA > valB) return 1 * dir;
        return 0;
      });

      return list;
    }),
    totalSpentSum: computed(() =>
      store.trips().reduce((acc, t) => acc + (t.fare || 0), 0)
    )
  })),
  withMethods((
    store,
    passengerService = inject(PassengerService),
    authService = inject(AuthService)
  ) => {
    const loadTrips = (passengerId?: string, page = 1, size = 10): void => {
      const pid = passengerId || authService.currentUser()?.id;
      if (!pid) return;

      patchState(store, { isLoading: true, error: null, pageNumber: page, pageSize: size });

      passengerService.getTripHistory(pid, page, size).pipe(
        tap({
          next: res => {
            patchState(store, {
              trips: res.trips || [],
              totalCount: res.totalCount || 0,
              pageNumber: res.pageNumber || page,
              pageSize: res.pageSize || size,
              totalPages: res.totalPages || 1,
              hasNextPage: res.hasNextPage || false,
              hasPreviousPage: res.hasPreviousPage || false,
              isLoading: false
            });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load trip history';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    return {
      loadTrips,

      setSearchTerm(term: string): void {
        patchState(store, { searchTerm: term });
      },

      setFilterStatus(status: string): void {
        patchState(store, { filterStatus: status });
      },

      setFilterDirection(direction: string): void {
        patchState(store, { filterDirection: direction });
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

      setPage(pageIndex: number, pageSize: number): void {
        loadTrips(undefined, pageIndex + 1, pageSize);
      },

      refreshHistory(passengerId?: string): void {
        loadTrips(passengerId, store.pageNumber(), store.pageSize());
      }
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      liveSync.alightingConfirmed$.subscribe(() => {
        store.refreshHistory();
      });

      liveSync.reconnected$.subscribe(() => {
        store.refreshHistory();
      });
    }
  })
);
