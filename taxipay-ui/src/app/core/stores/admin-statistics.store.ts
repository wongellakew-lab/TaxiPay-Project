import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { AdminAnalyticsOverview, DailyRevenueStat, RecentActivity, CorridorStat } from '../models/admin.models';
import { AdminService } from '../services/admin.service';
import { LiveSyncService } from '../services/live-sync.service';
import { AuthService } from '../services/auth.service';
import { tap, catchError, of } from 'rxjs';

export interface AdminStatisticsState {
  overview: AdminAnalyticsOverview | null;
  isLoading: boolean;
  error: string | null;
}

const initialAdminStatisticsState: AdminStatisticsState = {
  overview: null,
  isLoading: false,
  error: null
};

export const AdminStatisticsStore = signalStore(
  { providedIn: 'root' },
  withState(initialAdminStatisticsState),
  withComputed((store) => ({
    totalRevenue: computed(() => store.overview()?.totalRevenue ?? 0),
    todayRevenue: computed(() => store.overview()?.todayRevenue ?? 0),
    totalTrips: computed(() => store.overview()?.totalTrips ?? 0),
    activeTrips: computed(() => store.overview()?.activeTrips ?? 0),
    totalDrivers: computed(() => store.overview()?.totalDrivers ?? 0),
    activeDriversOnShift: computed(() => store.overview()?.activeDriversOnShift ?? 0),
    activeMinibuses: computed(() => store.overview()?.activeDriversOnShift ?? 0),
    pendingDriverApprovals: computed(() => store.overview()?.pendingDriverApprovals ?? 0),
    totalPassengers: computed(() => store.overview()?.totalPassengers ?? 0),
    openDisputes: computed(() => store.overview()?.openDisputes ?? 0),
    pendingRouteRequests: computed(() => store.overview()?.pendingRouteRequests ?? 0),
    dailyRevenueHistory: computed<DailyRevenueStat[]>(() => store.overview()?.dailyRevenueHistory ?? []),
    recentActivities: computed<RecentActivity[]>(() => store.overview()?.recentActivities ?? []),
    corridorStats: computed<CorridorStat[]>(() => store.overview()?.corridorStats ?? [])
  })),
  withMethods((
    store,
    adminService = inject(AdminService),
    authService = inject(AuthService)
  ) => {
    const loadOverview = (): void => {
      if (!authService.isAdmin()) return;

      patchState(store, { isLoading: true, error: null });

      adminService.getAnalyticsOverview().pipe(
        tap({
          next: (data: AdminAnalyticsOverview) => {
            patchState(store, { overview: data, isLoading: false, error: null });
          },
          error: (err: any) => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load platform analytics';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    return {
      loadOverview
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      // Whenever relevant real-time events happen, refresh admin overview
      liveSync.driverStatusUpdated$.subscribe(() => {
        store.loadOverview();
      });

      liveSync.driverRouteChangeSubmitted$.subscribe(() => {
        store.loadOverview();
      });

      liveSync.routeChangeReviewed$.subscribe(() => {
        store.loadOverview();
      });

      liveSync.disputeUpdated$.subscribe(() => {
        store.loadOverview();
      });

      liveSync.tripStatusUpdated$.subscribe(() => {
        store.loadOverview();
      });

      liveSync.passengerAddedToManifest$.subscribe(() => {
        store.loadOverview();
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadOverview();
      });
    }
  })
);
