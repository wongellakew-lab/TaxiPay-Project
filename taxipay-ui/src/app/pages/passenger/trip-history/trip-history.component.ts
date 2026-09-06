import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../../core/services/auth.service';
import { RideHistoryStore } from '../../../core/stores/ride-history.store';
import { PayTripResponse, TripHistoryItemDto } from '../../../core/models/passenger.models';
import { ReceiptDialogComponent } from '../../../shared/components/receipt-dialog/receipt-dialog.component';

@Component({
  selector: 'app-trip-history',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule
  ],
  templateUrl: './trip-history.component.html',
  styleUrl: './trip-history.component.scss'
})
export class TripHistoryComponent implements OnInit {
  auth = inject(AuthService);
  historyStore = inject(RideHistoryStore);
  dialog = inject(MatDialog);

  displayedColumns: string[] = ['date', 'journey', 'driver', 'fare', 'method', 'status', 'actions'];

  ngOnInit(): void {
    const passengerId = this.auth.currentUser()?.id;
    this.historyStore.loadTrips(passengerId);
  }

  toggleSort(column: string): void {
    this.historyStore.setSort(column);
  }

  getSortIcon(column: string): string {
    if (this.historyStore.sortColumn() !== column) return 'unfold_more';
    return this.historyStore.sortDirection() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  getBoardingName(item: TripHistoryItemDto): string {
    return item.boardingPointName || item.boardingNode || 'N/A';
  }

  getDestinationName(item: TripHistoryItemDto): string {
    return item.destinationPointName || item.destinationNode || 'N/A';
  }

  onSearchChange(query: string): void {
    this.historyStore.setSearchTerm(query);
  }

  onMethodFilterChange(method: string): void {
    this.historyStore.setFilterStatus(method);
  }

  resetFilters(): void {
    this.historyStore.setSearchTerm('');
    this.historyStore.setFilterStatus('ALL');
  }

  onPageChanged(event: PageEvent): void {
    this.historyStore.setPage(event.pageIndex, event.pageSize);
  }

  getBadgeClass(method: string): string {
    const m = (method || '').toLowerCase();
    if (m.includes('telebirr')) return 'badge-cyan';
    if (m.includes('wallet')) return 'badge-amber';
    if (m.includes('cash')) return 'badge-slate';
    return 'badge-emerald';
  }

  getStatusBadgeClass(status: string): string {
    const s = (status || '').toLowerCase();
    if (s.includes('completed') || s.includes('paid')) return 'badge-emerald';
    if (s.includes('pending')) return 'badge-amber';
    return 'badge-slate';
  }

  openTicket(item: TripHistoryItemDto): void {
    const receiptData: PayTripResponse = {
      tripId: item.tripId,
      passengerId: this.auth.currentUser()?.id || '',
      driverId: item.driverId,
      driverName: item.driverName,
      routeName: item.routeName,
      boardingNode: this.getBoardingName(item),
      destinationNode: this.getDestinationName(item),
      fare: item.fare,
      paymentMethod: item.paymentMethod,
      paymentStatus: item.paymentStatus,
      paidAt: item.tripStart,
      receiptNumber: item.transactionRef || ('TXP-' + item.tripId.slice(0, 8).toUpperCase())
    };

    this.dialog.open(ReceiptDialogComponent, {
      data: receiptData,
      width: '440px',
      panelClass: 'glass-dialog'
    });
  }

  exportHistory(): void {
    const rows = this.historyStore.filteredTrips().map(t =>
      `"${t.tripStart}","${t.routeName}","${this.getBoardingName(t)}","${this.getDestinationName(t)}","${t.driverName}","${t.driverId}","${t.paymentMethod}",${t.fare}`
    );
    const csvContent = 'data:text/csv;charset=utf-8,Date,Route,Boarding,Destination,Driver,DriverID,Method,Fare\n' + rows.join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', 'taxipay_ride_history.csv');
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
