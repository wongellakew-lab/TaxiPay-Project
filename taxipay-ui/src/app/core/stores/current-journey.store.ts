import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { CurrentJourneyDto } from '../models/passenger.models';
import { ActivePassengerDto } from '../models/driver.models';
import { PassengerService } from '../services/passenger.service';
import { AuthService } from '../services/auth.service';
import { LiveSyncService } from '../services/live-sync.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface CurrentJourneyState {
  currentJourney: CurrentJourneyDto | null;
  isLoading: boolean;
  isConfirmingAlight: boolean;
  error: string | null;
}

const initialCurrentJourneyState: CurrentJourneyState = {
  currentJourney: null,
  isLoading: false,
  isConfirmingAlight: false,
  error: null
};

export const CurrentJourneyStore = signalStore(
  { providedIn: 'root' },
  withState(initialCurrentJourneyState),
  withComputed((store) => ({
    hasActiveJourney: computed(() => !!store.currentJourney()),
    isAwaitingConfirmation: computed(() => store.currentJourney()?.alightingStatus === 'AwaitingConfirmation'),
    alightingPoint: computed(() => store.currentJourney()?.alightingPoint || ''),
    boardingPoint: computed(() => store.currentJourney()?.boardingPoint || ''),
    driverName: computed(() => store.currentJourney()?.driverName || 'Minibus Driver'),
    driverCode: computed(() => store.currentJourney()?.driverCode || ''),
    fare: computed(() => store.currentJourney()?.fare ?? 0),
    routeName: computed(() => store.currentJourney()?.routeName || 'Transit Route')
  })),
  withMethods((
    store,
    passengerService = inject(PassengerService),
    authService = inject(AuthService),
    liveSync = inject(LiveSyncService),
    snackBar = inject(MatSnackBar)
  ) => ({
    loadCurrentJourney(passengerId?: string): void {
      const pid = passengerId || authService.currentUser()?.id;
      if (!pid) return;

      patchState(store, { isLoading: true, error: null });

      passengerService.getDashboard(pid).pipe(
        tap({
          next: data => {
            patchState(store, {
              currentJourney: data.currentJourney || null,
              isLoading: false
            });
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load active journey';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    },

    setJourney(journey: CurrentJourneyDto | null): void {
      patchState(store, { currentJourney: journey });
    },

    confirmAlighting(passengerId?: string): void {
      const pid = passengerId || authService.currentUser()?.id;
      const journey = store.currentJourney();
      if (!pid || !journey) return;

      patchState(store, { isConfirmingAlight: true, error: null });

      passengerService.confirmAlighting(pid, journey.journeyId).pipe(
        tap({
          next: res => {
            patchState(store, {
              currentJourney: null,
              isConfirmingAlight: false
            });
            snackBar.open(
              `🎉 Journey completed! Alighting confirmed at ${res.destinationNode || journey.alightingPoint}.`,
              'OK',
              { duration: 4500, panelClass: 'snack-success' }
            );
          },
          error: err => {
            patchState(store, { isConfirmingAlight: false });
            const msg = err?.error?.detail || err?.error?.message || 'Could not confirm alighting. Please try again.';
            patchState(store, { error: msg });
            snackBar.open(msg, 'Dismiss', { duration: 4000, panelClass: 'snack-error' });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    },

    handleAlightingRequested(p: ActivePassengerDto): void {
      const current = store.currentJourney();
      if (current && current.journeyId === p.journeyId) {
        const updated: CurrentJourneyDto = {
          ...current,
          alightingStatus: 'AwaitingConfirmation',
          alightingPoint: p.destinationNode || current.alightingPoint
        };
        patchState(store, { currentJourney: updated });
        snackBar.open(
          `🔔 Your driver indicated you have arrived at ${p.destinationNode}. Please confirm your alighting.`,
          'OK',
          { duration: 7000, panelClass: 'snack-warning' }
        );
      }
    },

    handleAlightingConfirmed(p: ActivePassengerDto): void {
      const current = store.currentJourney();
      if (current && current.journeyId === p.journeyId) {
        patchState(store, { currentJourney: null });
        snackBar.open(
          `✅ Alighting confirmed at ${p.destinationNode}. Thank you for riding with TaxiPay!`,
          'OK',
          { duration: 5000, panelClass: 'snack-success' }
        );
      }
    },

    clearJourney(): void {
      patchState(store, { currentJourney: null });
    }
  })),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService), auth = inject(AuthService)) {
      liveSync.alightingRequested$.subscribe(p => {
        store.handleAlightingRequested(p);
      });

      liveSync.alightingConfirmed$.subscribe(p => {
        store.handleAlightingConfirmed(p);
      });

      liveSync.passengerAddedToManifest$.subscribe(p => {
        if (p.passengerId === auth.currentUser()?.id) {
          store.loadCurrentJourney();
        }
      });

      liveSync.reconnected$.subscribe(() => {
        store.loadCurrentJourney();
      });
    }
  })
);
