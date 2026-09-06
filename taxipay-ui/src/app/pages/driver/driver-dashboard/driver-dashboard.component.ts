import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { LiveSyncService } from '../../../core/services/live-sync.service';
import { DriverManifestStore } from '../../../core/stores/driver-manifest.store';
import { ActivePassengerDto } from '../../../core/models/driver.models';

@Component({
  selector: 'app-driver-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './driver-dashboard.component.html',
  styleUrl: './driver-dashboard.component.scss'
})
export class DriverDashboardComponent implements OnInit {
  public auth = inject(AuthService);
  public manifestStore = inject(DriverManifestStore);
  public liveSync = inject(LiveSyncService);
  private snackBar = inject(MatSnackBar);

  public currentDriverId = 'AB1234';
  public selectedDirection = signal<string>('');

  constructor() {
    // Automatically pre-select first available direction when directions are loaded
    effect(() => {
      const dirs = this.manifestStore.availableDirections();
      if (dirs.length > 0 && !this.selectedDirection()) {
        this.selectedDirection.set(dirs[0].directionKey);
      }
    });
  }

  ngOnInit(): void {
    const user = this.auth.currentUser();
    this.currentDriverId = user?.driverCode || user?.driverData?.driverId || 'AB1234';

    // Ensure SignalR connection is joined to driver group
    const did = user?.driverCode || user?.driverData?.driverId || user?.id;
    if (did) {
      this.liveSync.joinDriverGroup(did);
    }
    if (user?.id) {
      this.liveSync.joinDriverGroup(user.id);
    }

    this.manifestStore.loadDashboard();
  }

  selectedDirectionLabel = computed(() => {
    const key = this.selectedDirection();
    const dirs = this.manifestStore.availableDirections();
    const match = dirs.find(d => d.directionKey === key);
    return match ? match.displayLabel : (key || 'Corridor Forward');
  });

  startTrip(): void {
    if (this.manifestStore.isTripActive()) {
      this.snackBar.open('⚠️ An active trip is already in progress. End current trip first!', 'OK', {
        duration: 4000,
        panelClass: 'snack-warning'
      });
      return;
    }

    const shiftId = this.manifestStore.activeShiftId();
    if (!shiftId) {
      this.snackBar.open('Please verify you are on an active shift.', 'OK', { duration: 3500 });
      return;
    }

    let dir = this.selectedDirection();
    if (!dir && this.manifestStore.availableDirections().length > 0) {
      dir = this.manifestStore.availableDirections()[0].directionKey;
    }

    this.manifestStore.startTrip(shiftId, dir);
  }

  completeTrip(): void {
    if (!this.manifestStore.isTripActive()) return;

    const shiftId = this.manifestStore.activeShiftId();
    if (!shiftId) return;

    this.manifestStore.completeTrip(shiftId);
  }

  requestAlight(p: ActivePassengerDto): void {
    this.manifestStore.requestAlight(p);
  }

  getBadgeClass(method: string): string {
    const m = (method || '').toLowerCase();
    if (m.includes('telebirr')) return 'badge-cyan';
    if (m.includes('wallet')) return 'badge-amber';
    if (m.includes('cash')) return 'badge-slate';
    return 'badge-emerald';
  }
}
