import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Subscription } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { DriverService } from '../../../core/services/driver.service';
import { LiveSyncService } from '../../../core/services/live-sync.service';
import { DriverManifestStore } from '../../../core/stores/driver-manifest.store';
import { ShiftSummaryResponse } from '../../../core/models/driver.models';

@Component({
  selector: 'app-shift-summary',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressBarModule
  ],
  templateUrl: './shift-summary.component.html',
  styleUrl: './shift-summary.component.scss'
})
export class ShiftSummaryComponent implements OnInit, OnDestroy {
  public auth = inject(AuthService);
  public driverService = inject(DriverService);
  public manifestStore = inject(DriverManifestStore);
  public liveSync = inject(LiveSyncService);

  summary = signal<ShiftSummaryResponse | null>(null);
  private subs = new Subscription();

  get digitalAmount(): number {
    return this.summary()?.paymentBreakdown?.digital ?? this.manifestStore.digitalRevenue();
  }

  get cashAmount(): number {
    return this.summary()?.paymentBreakdown?.cash ?? this.manifestStore.cashRevenue();
  }

  get digitalPercent(): number {
    const d = this.digitalAmount;
    const c = this.cashAmount;
    const tot = d + c;
    return tot > 0 ? Math.round((d / tot) * 100) : (this.manifestStore.digitalPercent() || 0);
  }

  get cashPercent(): number {
    return this.digitalPercent > 0 ? 100 - this.digitalPercent : (this.manifestStore.cashPercent() || 0);
  }

  ngOnInit(): void {
    this.loadData();

    this.subs.add(
      this.liveSync.passengerAddedToManifest$.subscribe(() => {
        this.loadData();
      })
    );

    this.subs.add(
      this.liveSync.tripStatusUpdated$.subscribe(() => {
        this.loadData();
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  public loadData(): void {
    const user = this.auth.currentUser();
    const did = user?.id || user?.driverCode || user?.driverData?.driverId;
    if (did) {
      this.manifestStore.loadDashboard(did);

      this.driverService.getDashboard(did).subscribe({
        next: dash => {
          if (dash.activeShiftId) {
            this.driverService.getShiftSummary(dash.activeShiftId).subscribe({
              next: s => this.summary.set(s),
              error: () => {}
            });
          }
        },
        error: () => {}
      });
    }
  }
}

