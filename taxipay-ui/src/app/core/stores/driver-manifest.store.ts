import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { ActivePassengerDto, ActiveTripDto, CompletedTripDto, DriverDashboardResponse, NodeDto, PaymentBreakdownDto, RouteDirectionDto } from '../models/driver.models';
import { DriverService } from '../services/driver.service';
import { AuthService } from '../services/auth.service';
import { LiveSyncService } from '../services/live-sync.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface DriverManifestState {
  dashboardData: DriverDashboardResponse | null;
  activePassengers: ActivePassengerDto[];
  maxSeats: number;
  currentTripId: string | null;
  activeTrip: ActiveTripDto | null;
  lastCompletedTrip: CompletedTripDto | null;
  availableDirections: RouteDirectionDto[];
  routeNodes: NodeDto[];
  routeName: string;
  routeCode: string;
  direction: string;
  hasActiveShift: boolean;
  activeShiftId: string | null;
  tripStatus: string;
  todayRevenue: number;
  todayTrips: number;
  todayPassengers: number;
  paymentBreakdown: PaymentBreakdownDto;
  currentTripNumber: number;
  isLoading: boolean;
  isActionLoading: boolean;
  error: string | null;
}

const initialManifestState: DriverManifestState = {
  dashboardData: null,
  activePassengers: [],
  maxSeats: 15,
  currentTripId: null,
  activeTrip: null,
  lastCompletedTrip: null,
  availableDirections: [],
  routeNodes: [],
  routeName: '',
  routeCode: '',
  direction: 'FORWARD',
  hasActiveShift: false,
  activeShiftId: null,
  tripStatus: 'None',
  todayRevenue: 0,
  todayTrips: 0,
  todayPassengers: 0,
  paymentBreakdown: { digital: 0, cash: 0, telebirr: 0, wallet: 0 },
  currentTripNumber: 1,
  isLoading: false,
  isActionLoading: false,
  error: null
};

export const DriverManifestStore = signalStore(
  { providedIn: 'root' },
  withState(initialManifestState),
  withComputed((store) => ({
    passengerCount: computed(() => store.activePassengers().length),
    availableSeats: computed(() => Math.max(0, store.maxSeats() - store.activePassengers().length)),
    onBoardCount: computed(() =>
      store.activePassengers().filter(p => p.alightingStatus !== 'AwaitingConfirmation').length
    ),
    awaitingConfirmationCount: computed(() =>
      store.activePassengers().filter(p => p.alightingStatus === 'AwaitingConfirmation').length
    ),
    isFull: computed(() => store.activePassengers().length >= store.maxSeats()),
    hasActiveTrip: computed(() => !!store.currentTripId() && store.tripStatus() === 'Active'),
    isTripActive: computed(() => !!store.currentTripId() && store.tripStatus() === 'Active'),
    digitalRevenue: computed(() => store.paymentBreakdown()?.digital ?? 0),
    cashRevenue: computed(() => store.paymentBreakdown()?.cash ?? 0),
    digitalPercent: computed(() => {
      const d = store.paymentBreakdown()?.digital ?? 0;
      const c = store.paymentBreakdown()?.cash ?? 0;
      const total = d + c;
      return total > 0 ? Math.round((d / total) * 100) : 0;
    }),
    cashPercent: computed(() => {
      const d = store.paymentBreakdown()?.digital ?? 0;
      const c = store.paymentBreakdown()?.cash ?? 0;
      const total = d + c;
      return total > 0 ? (100 - Math.round((d / total) * 100)) : 0;
    })
  })),
  withMethods((
    store,
    driverService = inject(DriverService),
    authService = inject(AuthService),
    liveSync = inject(LiveSyncService),
    snackBar = inject(MatSnackBar)
  ) => {
    const loadDashboard = (driverId?: string): void => {
      const user = authService.currentUser();
      const did = driverId || user?.id || user?.driverCode || user?.driverData?.driverId;
      if (!did) return;

      patchState(store, { isLoading: true, error: null });

      driverService.getDashboard(did).pipe(
        tap({
          next: (data: DriverDashboardResponse) => {
            const passengers = data.activePassengers || [];
            const revenue = data.todaySummary?.todayRevenue ?? data.totalRevenue ?? 0;
            const trips = data.todaySummary?.todayTrips ?? 0;
            const totalPass = data.todaySummary?.todayPassengers ?? data.totalPassengers ?? 0;
            const isTripActive = !!data.isTripActive || !!data.activeTripId;
            const pb = data.todaySummary?.paymentBreakdown || { digital: revenue, cash: 0, telebirr: revenue, wallet: 0 };

            patchState(store, {
              dashboardData: data,
              activePassengers: passengers,
              maxSeats: data.capacity || 15,
              currentTripId: data.activeTripId || (data.activeTrip ? data.activeTrip.tripId : null),
              activeTrip: data.activeTrip || null,
              lastCompletedTrip: data.lastCompletedTrip || null,
              availableDirections: data.availableDirections || [],
              routeNodes: data.routeNodes || [],
              routeName: data.routeName || '',
              routeCode: data.routeCode || '',
              hasActiveShift: !!data.activeShiftId,
              activeShiftId: data.activeShiftId || null,
              tripStatus: isTripActive ? 'Active' : 'None',
              todayRevenue: revenue,
              todayTrips: trips,
              todayPassengers: totalPass,
              paymentBreakdown: pb,
              currentTripNumber: data.currentTripNumber || 1,
              isLoading: false
            });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load driver dashboard';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const startTrip = (shiftId: string, direction: string, onSuccess?: () => void): void => {
      patchState(store, { isActionLoading: true, error: null });

      driverService.startTrip(shiftId, direction).pipe(
        tap({
          next: trip => {
            patchState(store, {
              isActionLoading: false,
              currentTripId: trip.tripId,
              tripStatus: 'Active',
              direction: trip.direction,
              activePassengers: [],
              currentTripNumber: trip.tripNumber,
              activeTrip: {
                tripId: trip.tripId,
                tripNumber: trip.tripNumber,
                routeName: trip.routeName || store.routeName(),
                routeCode: store.routeCode(),
                direction: trip.direction,
                directionDisplay: trip.direction,
                tripStart: trip.tripStart,
                status: 'Active',
                passengersCount: 0,
                tripRevenue: 0
              }
            });

            snackBar.open('🚀 Trip Started successfully! Minibus is now accepting commuters.', 'OK', {
              duration: 4000,
              panelClass: 'snack-success'
            });

            if (onSuccess) onSuccess();
            loadDashboard();
          },
          error: err => {
            patchState(store, { isActionLoading: false });
            const msg = err?.error?.detail || err?.error?.title || 'Could not start trip.';
            snackBar.open(msg, 'Dismiss', { duration: 4500, panelClass: 'snack-error' });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const completeTrip = (shiftId: string, onSuccess?: () => void): void => {
      patchState(store, { isActionLoading: true, error: null });

      driverService.completeTrip(shiftId, {
        latitude: 9.0107,
        longitude: 38.7612
      }).pipe(
        tap({
          next: trip => {
            const active = store.activeTrip();
            const completed: CompletedTripDto = {
              tripId: trip.tripId,
              tripNumber: trip.tripNumber,
              routeName: trip.routeName || store.routeName(),
              direction: trip.direction || store.direction(),
              tripStart: trip.tripStart || (active ? active.tripStart : new Date().toISOString()),
              tripEnd: trip.tripEnd || new Date().toISOString(),
              status: 'Completed',
              passengersCount: trip.passengersCount,
              tripRevenue: trip.tripRevenue
            };

            patchState(store, {
              isActionLoading: false,
              currentTripId: null,
              activeTrip: null,
              lastCompletedTrip: completed,
              tripStatus: 'Completed',
              activePassengers: []
            });

            snackBar.open('🏁 Trip completed! Vehicle ready for next run.', 'OK', {
              duration: 4500,
              panelClass: 'snack-success'
            });

            if (onSuccess) onSuccess();
            loadDashboard();
          },
          error: err => {
            patchState(store, { isActionLoading: false });
            const msg = err?.error?.detail || err?.error?.title || 'Cannot complete trip: Check active passengers.';
            snackBar.open(msg, 'Dismiss', { duration: 5500, panelClass: 'snack-error' });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const requestAlight = (p: ActivePassengerDto): void => {
      if (!p.journeyId) return;

      patchState(store, { isActionLoading: true });

      driverService.requestPassengerAlighting(p.journeyId).pipe(
        tap({
          next: () => {
            patchState(store, { isActionLoading: false });
            snackBar.open(
              `⏳ Alighting requested for ${p.passengerName}. Awaiting passenger confirmation.`,
              'OK',
              { duration: 3500 }
            );

            const currentPassengers = store.activePassengers();
            const updated = currentPassengers.map(pass =>
              pass.journeyId === p.journeyId ? { ...pass, alightingStatus: 'AwaitingConfirmation' } : pass
            );
            patchState(store, { activePassengers: updated });
          },
          error: err => {
            patchState(store, { isActionLoading: false });
            const msg = err?.error?.detail || err?.error?.message || 'Could not request alighting.';
            snackBar.open(msg, 'Dismiss', { duration: 3500 });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const handlePassengerAdded = (passenger: ActivePassengerDto): void => {
      const exists = store.activePassengers().some(p => p.journeyId === passenger.journeyId);
      if (exists) {
        const updated = store.activePassengers().map(p =>
          p.journeyId === passenger.journeyId ? passenger : p
        );
        patchState(store, { activePassengers: updated });
      } else {
        const newPassengers = [passenger, ...store.activePassengers()];
        const fare = passenger.fare || 0;
        const newRevenue = store.todayRevenue() + fare;

        const isCash = (passenger.paymentMethod || '').toLowerCase().includes('cash');
        const currentPb = store.paymentBreakdown() || { digital: 0, cash: 0, telebirr: 0, wallet: 0 };
        const updatedPb: PaymentBreakdownDto = {
          digital: isCash ? currentPb.digital : currentPb.digital + fare,
          cash: isCash ? currentPb.cash + fare : currentPb.cash,
          telebirr: (passenger.paymentMethod || '').toLowerCase().includes('telebirr') ? (currentPb.telebirr || 0) + fare : currentPb.telebirr,
          wallet: (passenger.paymentMethod || '').toLowerCase().includes('wallet') ? (currentPb.wallet || 0) + fare : currentPb.wallet
        };

        const currentActive = store.activeTrip();
        let updatedActiveTrip = currentActive;
        if (currentActive) {
          updatedActiveTrip = {
            ...currentActive,
            passengersCount: currentActive.passengersCount + 1,
            tripRevenue: currentActive.tripRevenue + fare
          };
        }

        patchState(store, {
          activePassengers: newPassengers,
          todayRevenue: newRevenue,
          paymentBreakdown: updatedPb,
          activeTrip: updatedActiveTrip
        });

        snackBar.open(`💰 Commuter ${passenger.passengerName} paid ${passenger.fare} ETB and boarded!`, 'OK', {
          duration: 4000,
          panelClass: 'snack-success'
        });
      }
    };

    const handleAlightingRequested = (passenger: ActivePassengerDto): void => {
      const updated = store.activePassengers().map(p =>
        p.journeyId === passenger.journeyId
          ? { ...p, alightingStatus: 'AwaitingConfirmation', destinationNode: passenger.destinationNode || p.destinationNode }
          : p
      );
      patchState(store, { activePassengers: updated });
    };

    const handleAlightingConfirmed = (passenger: ActivePassengerDto): void => {
      const filtered = store.activePassengers().filter(p => p.journeyId !== passenger.journeyId);
      patchState(store, { activePassengers: filtered });
      snackBar.open(
        `✓ Commuter ${passenger.passengerName} confirmed drop-off and alighted.`,
        'OK',
        { duration: 3500, panelClass: 'snack-success' }
      );
    };

    const setTripState = (tripId: string | null, routeName: string, status: string, direction = 'FORWARD'): void => {
      patchState(store, {
        currentTripId: tripId,
        routeName,
        tripStatus: status,
        direction,
        activePassengers: tripId ? store.activePassengers() : []
      });
    };

    const clearManifest = (): void => {
      patchState(store, {
        activePassengers: [],
        currentTripId: null,
        activeTrip: null,
        tripStatus: 'Completed'
      });
    };

    return {
      loadDashboard,
      loadManifest: loadDashboard,
      startTrip,
      completeTrip,
      requestAlight,
      handlePassengerAdded,
      handleAlightingRequested,
      handleAlightingConfirmed,
      setTripState,
      clearManifest
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService)) {
      liveSync.passengerAddedToManifest$.subscribe(p => {
        store.handlePassengerAdded(p);
      });

      liveSync.alightingRequested$.subscribe(p => {
        store.handleAlightingRequested(p);
      });

      liveSync.alightingConfirmed$.subscribe(p => {
        store.handleAlightingConfirmed(p);
      });

      liveSync.tripStatusUpdated$.subscribe(status => {
        if (status.status === 'Completed') {
          store.clearManifest();
        } else {
          store.setTripState(status.tripId, store.routeName(), status.status, status.direction);
        }
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadDashboard();
      });
    }
  })
);
