import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DisputeStore } from '../../../core/stores/dispute.store';
import { AdminDispute } from '../../../core/models/admin.models';

@Component({
  selector: 'app-admin-disputes',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatTooltipModule
  ],
  templateUrl: './admin-disputes.component.html',
  styleUrl: './admin-disputes.component.scss'
})
export class AdminDisputesComponent implements OnInit {
  public store = inject(DisputeStore);

  // Close Dispute Modal State
  activeDispute: AdminDispute | null = null;
  resolutionNotes = '';
  issueRefund = true;

  ngOnInit(): void {
    this.store.loadDisputes();
  }

  setStatusFilter(status: string): void {
    this.store.setStatusFilter(status);
  }

  onSearchChange(query: string): void {
    this.store.setSearchQuery(query);
  }

  toggleSort(column: string): void {
    this.store.setSort(column);
  }

  getSortIcon(column: string): string {
    if (this.store.sortColumn() !== column) return 'unfold_more';
    return this.store.sortDirection() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  openCloseModal(disp: AdminDispute): void {
    this.activeDispute = disp;
    this.resolutionNotes = 'Arbitrated trip records. Dispute closed with administrative settlement.';
    this.issueRefund = true;
  }

  reopenDispute(disp: AdminDispute): void {
    this.store.openDispute(disp.id, 'Dispute reopened by administrator for arbitration.');
  }

  submitCloseDispute(): void {
    if (!this.activeDispute) return;

    this.store.closeDispute(
      this.activeDispute.id,
      this.resolutionNotes,
      this.issueRefund
    );
    this.activeDispute = null;
  }
}
