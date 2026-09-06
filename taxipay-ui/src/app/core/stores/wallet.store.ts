import { inject } from '@angular/core';
import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { PassengerService } from '../services/passenger.service';
import { AuthService } from '../services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface WalletState {
  balance: number;
  isLoading: boolean;
  isTopUpLoading: boolean;
  error: string | null;
  lastTopUpRef: string | null;
  lastUpdated: string | null;
}

const initialWalletState: WalletState = {
  balance: 0,
  isLoading: false,
  isTopUpLoading: false,
  error: null,
  lastTopUpRef: null,
  lastUpdated: null
};

export const WalletStore = signalStore(
  { providedIn: 'root' },
  withState(initialWalletState),
  withMethods((
    store,
    passengerService = inject(PassengerService),
    authService = inject(AuthService),
    snackBar = inject(MatSnackBar)
  ) => ({
    loadBalance(passengerId?: string): void {
      const pid = passengerId || authService.currentUser()?.id;
      if (!pid) return;

      patchState(store, { isLoading: true, error: null });

      passengerService.getProfile(pid).pipe(
        tap({
          next: profile => {
            patchState(store, {
              balance: profile.walletBalance ?? 0,
              isLoading: false,
              lastUpdated: new Date().toISOString()
            });
            // Keep passenger service legacy signal in sync
            passengerService.walletBalance.set(profile.walletBalance ?? 0);
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Failed to load wallet balance';
            patchState(store, { isLoading: false, error: msg });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    },

    setBalance(newBalance: number): void {
      patchState(store, {
        balance: newBalance,
        lastUpdated: new Date().toISOString()
      });
      passengerService.walletBalance.set(newBalance);
    },

    topUp(amount: number, paymentMethod = 'Telebirr', passengerId?: string): void {
      const pid = passengerId || authService.currentUser()?.id;
      if (!pid || amount <= 0) return;

      patchState(store, { isTopUpLoading: true, error: null });

      passengerService.topUpWallet(pid, { amount, paymentMethod }).pipe(
        tap({
          next: res => {
            patchState(store, {
              balance: res.newBalance,
              isTopUpLoading: false,
              lastTopUpRef: res.transactionRef,
              lastUpdated: res.topUpTime
            });
            passengerService.walletBalance.set(res.newBalance);
            snackBar.open(
              `💳 Wallet topped up with ${amount} ETB successfully! Reference: ${res.transactionRef}`,
              'OK',
              { duration: 4000, panelClass: 'snack-success' }
            );
          },
          error: err => {
            const msg = err?.error?.detail || err?.error?.message || 'Top-up transaction failed. Please try again.';
            patchState(store, { isTopUpLoading: false, error: msg });
            snackBar.open(msg, 'Dismiss', { duration: 4000, panelClass: 'snack-error' });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    },

    clearError(): void {
      patchState(store, { error: null });
    }
  }))
);
