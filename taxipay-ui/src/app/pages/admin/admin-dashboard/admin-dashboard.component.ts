import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/services/auth.service';
import { AdminStatisticsStore } from '../../../core/stores/admin-statistics.store';
import { DriverManagementStore } from '../../../core/stores/driver-management.store';
import { DisputeStore } from '../../../core/stores/dispute.store';
import { AdminRouteRequestStore } from '../../../core/stores/admin-route-request.store';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDividerModule,
    MatProgressBarModule,
    MatTooltipModule
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  public auth = inject(AuthService);
  public adminStatsStore = inject(AdminStatisticsStore);
  public driverStore = inject(DriverManagementStore);
  public disputeStore = inject(DisputeStore);
  public routeRequestStore = inject(AdminRouteRequestStore);

  totalPendingActions = computed(() =>
    this.driverStore.pendingApprovalCount() +
    this.disputeStore.openCount() +
    this.routeRequestStore.pendingCount()
  );

  ngOnInit(): void {
    this.adminStatsStore.loadOverview();
    this.driverStore.loadDrivers();
    this.disputeStore.loadDisputes();
    this.routeRequestStore.loadRequests();
  }

  getBarHeight(amount: number): number {
    const max = 30000;
    return Math.min(100, Math.max(15, (amount / max) * 100));
  }

  getActTypeClass(type: string): string {
    switch (type) {
      case 'Trip': return 'bg-cyan-subtle';
      case 'DriverRegistration': return 'bg-blue-subtle';
      case 'RouteChange': return 'bg-gold-subtle';
      case 'Dispute': return 'bg-purple-subtle';
      default: return 'bg-blue-subtle';
    }
  }

  getActIcon(type: string): string {
    switch (type) {
      case 'Trip': return 'directions_bus';
      case 'DriverRegistration': return 'person_add';
      case 'RouteChange': return 'alt_route';
      case 'Dispute': return 'gavel';
      default: return 'info';
    }
  }
}
