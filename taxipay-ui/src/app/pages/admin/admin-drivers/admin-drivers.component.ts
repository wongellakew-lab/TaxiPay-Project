import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DriverManagementStore } from '../../../core/stores/driver-management.store';
import { AdminDriverListItem } from '../../../core/models/admin.models';

@Component({
  selector: 'app-admin-drivers',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTabsModule,
    MatDialogModule
  ],
  templateUrl: './admin-drivers.component.html',
  styleUrl: './admin-drivers.component.scss'
})
export class AdminDriversComponent implements OnInit {
  public driverStore = inject(DriverManagementStore);
  private snackBar = inject(MatSnackBar);

  // Modals state
  selectedDriverForRoute: AdminDriverListItem | null = null;
  targetRouteId = '';

  selectedDriverForApprove: AdminDriverListItem | null = null;
  approveAssignedRouteId = '';

  selectedDriverForReject: AdminDriverListItem | null = null;
  rejectionReasonText = '';

  ngOnInit(): void {
    this.driverStore.loadDrivers();
    this.driverStore.loadRoutes();
  }

  setStatusFilter(status: string): void {
    this.driverStore.setStatusFilter(status);
  }

  onSearchChange(query: string): void {
    this.driverStore.setSearchQuery(query);
  }

  toggleSort(column: string): void {
    this.driverStore.setSort(column);
  }

  getSortIcon(column: string): string {
    if (this.driverStore.sortColumn() !== column) return 'unfold_more';
    return this.driverStore.sortDirection() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  getCount(status: string): number {
    return this.driverStore.drivers().filter(d => d.status.toLowerCase() === status.toLowerCase()).length;
  }

  openApproveModal(driver: AdminDriverListItem): void {
    this.selectedDriverForApprove = driver;
    this.approveAssignedRouteId = driver.preferredRouteId || driver.currentRouteId || '';
  }

  confirmDriverApproval(): void {
    if (!this.selectedDriverForApprove) return;

    const driver = this.selectedDriverForApprove;
    const assignedRouteId = this.approveAssignedRouteId || undefined;

    this.driverStore.approveDriver(driver.id, assignedRouteId);
    this.selectedDriverForApprove = null;
  }

  openRejectModal(driver: AdminDriverListItem): void {
    this.selectedDriverForReject = driver;
    this.rejectionReasonText = '';
  }

  confirmDriverRejection(): void {
    if (!this.selectedDriverForReject || !this.rejectionReasonText.trim()) return;

    const driver = this.selectedDriverForReject;
    const reason = this.rejectionReasonText.trim();

    this.driverStore.rejectDriver(driver.id, reason);
    this.selectedDriverForReject = null;
  }

  suspendDriver(driver: AdminDriverListItem): void {
    this.driverStore.updateDriverStatus(driver.id, 'Suspended', 'Account suspended by admin');
  }

  openAssignModal(driver: AdminDriverListItem): void {
    this.selectedDriverForRoute = driver;
    this.targetRouteId = driver.currentRouteId || '';
  }

  confirmRouteAssignment(): void {
    if (!this.selectedDriverForRoute || !this.targetRouteId) return;

    this.driverStore.assignRoute(this.selectedDriverForRoute.id, this.targetRouteId);
    this.selectedDriverForRoute = null;
  }
}
