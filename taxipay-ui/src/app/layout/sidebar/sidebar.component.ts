import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { WalletStore } from '../../core/stores/wallet.store';
import { DriverManifestStore } from '../../core/stores/driver-manifest.store';
import { AdminStatisticsStore } from '../../core/stores/admin-statistics.store';
import { DriverManagementStore } from '../../core/stores/driver-management.store';
import { AdminRouteRequestStore } from '../../core/stores/admin-route-request.store';
import { DisputeStore } from '../../core/stores/dispute.store';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule, MatDividerModule, MatTooltipModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit {
  public auth = inject(AuthService);
  public walletStore = inject(WalletStore);
  public driverStore = inject(DriverManifestStore);
  public adminStatsStore = inject(AdminStatisticsStore);
  public driverMgmtStore = inject(DriverManagementStore);
  public routeRequestStore = inject(AdminRouteRequestStore);
  public disputeStore = inject(DisputeStore);

  ngOnInit(): void {
    if (this.auth.isAdmin()) {
      this.adminStatsStore.loadOverview();
      this.driverMgmtStore.loadDrivers();
      this.routeRequestStore.loadRequests();
      this.disputeStore.loadDisputes();
    } else if (this.auth.isPassenger()) {
      this.walletStore.loadBalance();
    } else if (this.auth.isDriver()) {
      this.driverStore.loadDashboard();
    }
  }
}

