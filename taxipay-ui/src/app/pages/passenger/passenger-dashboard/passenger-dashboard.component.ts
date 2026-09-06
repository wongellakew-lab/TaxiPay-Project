import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { PassengerService } from '../../../core/services/passenger.service';
import { WalletStore } from '../../../core/stores/wallet.store';
import { CurrentJourneyStore } from '../../../core/stores/current-journey.store';
import { PassengerDashboardResponse } from '../../../core/models/passenger.models';

@Component({
  selector: 'app-passenger-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './passenger-dashboard.component.html',
  styleUrl: './passenger-dashboard.component.scss'
})
export class PassengerDashboardComponent implements OnInit {
  auth = inject(AuthService);
  passengerService = inject(PassengerService);
  walletStore = inject(WalletStore);
  currentJourneyStore = inject(CurrentJourneyStore);

  dashboard = signal<PassengerDashboardResponse | null>(null);
  isLoading = signal<boolean>(false);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    const pid = user?.id;

    this.walletStore.loadBalance(pid);
    this.currentJourneyStore.loadCurrentJourney(pid);
    this.loadDashboard();
  }

  loadDashboard(showLoading = true): void {
    const user = this.auth.currentUser();
    const pid = user?.id;
    if (!pid) return;

    if (showLoading) {
      this.isLoading.set(true);
    }

    this.passengerService.getDashboard(pid).subscribe({
      next: data => {
        this.dashboard.set(data);
        if (data.currentJourney) {
          this.currentJourneyStore.setJourney(data.currentJourney);
        }
        if (data.summary?.walletBalance !== undefined) {
          this.walletStore.setBalance(data.summary.walletBalance);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  confirmAlighting(): void {
    this.currentJourneyStore.confirmAlighting();
  }

  getBadgeClass(method: string): string {
    const m = (method || '').toLowerCase();
    if (m.includes('telebirr')) return 'badge-cyan';
    if (m.includes('wallet')) return 'badge-amber';
    if (m.includes('cash')) return 'badge-slate';
    return 'badge-emerald';
  }
}
