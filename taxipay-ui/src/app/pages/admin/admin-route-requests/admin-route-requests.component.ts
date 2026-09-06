import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AdminRouteRequestStore } from '../../../core/stores/admin-route-request.store';
import { AdminRouteChangeRequest } from '../../../core/models/admin.models';

@Component({
  selector: 'app-admin-route-requests',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './admin-route-requests.component.html',
  styleUrl: './admin-route-requests.component.scss'
})
export class AdminRouteRequestsComponent implements OnInit {
  public store = inject(AdminRouteRequestStore);

  activeModalRequest: AdminRouteChangeRequest | null = null;
  modalAction: 'approve' | 'reject' = 'approve';
  reviewNote = '';

  ngOnInit(): void {
    this.store.loadRequests();
  }

  setStatusFilter(status: string): void {
    this.store.setStatusFilter(status);
  }

  onSearchChange(query: string): void {
    this.store.setSearchQuery(query);
  }

  openApproveModal(req: AdminRouteChangeRequest): void {
    this.activeModalRequest = req;
    this.modalAction = 'approve';
    this.reviewNote = 'Transfer approved after route capacity check.';
  }

  openRejectModal(req: AdminRouteChangeRequest): void {
    this.activeModalRequest = req;
    this.modalAction = 'reject';
    this.reviewNote = 'Route is at capacity or transfer window closed.';
  }

  submitReview(): void {
    if (!this.activeModalRequest) return;

    if (this.modalAction === 'approve') {
      this.store.approveRequest(this.activeModalRequest.id, this.reviewNote);
    } else {
      this.store.rejectRequest(this.activeModalRequest.id, this.reviewNote);
    }

    this.activeModalRequest = null;
  }
}
