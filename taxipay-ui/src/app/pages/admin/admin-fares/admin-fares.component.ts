import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';
import { FareMatrixItem, RouteDetail } from '../../../core/models/admin.models';

@Component({
  selector: 'app-admin-fares',
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
    MatSlideToggleModule
  ],
  templateUrl: './admin-fares.component.html',
  styleUrl: './admin-fares.component.scss'
})
export class AdminFaresComponent implements OnInit {
  private adminService = inject(AdminService);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  routes = signal<RouteDetail[]>([]);
  fareMatrices = signal<FareMatrixItem[]>([]);

  // Sorting
  sortColumn: string = 'from';
  sortDirection: 'asc' | 'desc' = 'asc';

  selectedRouteId = '';
  currentRoute: RouteDetail | null = null;

  showBatchModal = false;
  batchHopRate = 5;

  sortedFareMatrices(): FareMatrixItem[] {
    const list = [...this.fareMatrices()];
    list.sort((a, b) => {
      let comparison = 0;
      switch (this.sortColumn) {
        case 'from':
          comparison = this.getNodeName(a.fromNode).localeCompare(this.getNodeName(b.fromNode));
          break;
        case 'to':
          comparison = this.getNodeName(a.toNode).localeCompare(this.getNodeName(b.toNode));
          break;
        case 'fare':
          comparison = a.fare - b.fare;
          break;
        case 'status':
          comparison = (a.isActive === b.isActive) ? 0 : a.isActive ? -1 : 1;
          break;
        default:
          comparison = 0;
          break;
      }
      return this.sortDirection === 'asc' ? comparison : -comparison;
    });
    return list;
  }

  toggleSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  getSortIcon(column: string): string {
    if (this.sortColumn !== column) return 'unfold_more';
    return this.sortDirection === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  ngOnInit(): void {
    this.adminService.getRoutes().subscribe(r => {
      this.routes.set(r);
      const queryRouteId = this.route.snapshot.queryParams['routeId'];
      if (queryRouteId && r.some(x => x.id === queryRouteId)) {
        this.onRouteChange(queryRouteId);
      } else if (r.length > 0) {
        this.onRouteChange(r[0].id);
      }
    });
  }

  onRouteChange(routeId: string): void {
    this.selectedRouteId = routeId;
    this.currentRoute = this.routes().find(r => r.id === routeId) || null;
    this.loadFareMatrices();
  }

  loadFareMatrices(): void {
    if (!this.selectedRouteId) return;
    this.adminService.getFareMatricesByRoute(this.selectedRouteId).subscribe(f => {
      this.fareMatrices.set(f);
    });
  }

  getActiveCount(): number {
    return this.fareMatrices().filter(f => f.isActive).length;
  }

  getNodeName(id: string): string {
    const n = this.currentRoute?.nodes.find(node => node.id === id);
    return n ? n.name : `Stop ${id}`;
  }

  getFareAmount(fromNode: string, toNode: string): number {
    const item = this.fareMatrices().find(f => f.fromNode === fromNode && f.toNode === toNode);
    return item ? item.fare : 0;
  }

  onQuickFareChange(fromNode: string, toNode: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const newFare = parseFloat(target.value) || 0;

    const existing = this.fareMatrices().find(f => f.fromNode === fromNode && f.toNode === toNode);
    if (existing) {
      this.adminService.updateFareMatrix(existing.id, { fare: newFare, isActive: true }).subscribe(() => {
        existing.fare = newFare;
        this.snackBar.open(`Fare ${fromNode}→${toNode} updated to ${newFare} ETB`, 'OK', { duration: 2500 });
      });
    } else {
      this.adminService.createFareMatrix({
        routeId: this.selectedRouteId,
        fromNode,
        toNode,
        fare: newFare,
        isActive: true
      }).subscribe(() => {
        this.snackBar.open(`Fare ${fromNode}→${toNode} created: ${newFare} ETB`, 'OK', { duration: 2500 });
        this.loadFareMatrices();
      });
    }
  }

  toggleFareStatus(item: FareMatrixItem): void {
    this.adminService.updateFareMatrix(item.id, { fare: item.fare, isActive: !item.isActive }).subscribe(() => {
      item.isActive = !item.isActive;
      this.snackBar.open(`Rule ${item.fromNode}→${item.toNode} is now ${item.isActive ? 'Active' : 'Disabled'}.`, 'OK', { duration: 2500 });
    });
  }

  openEditFareModal(item: FareMatrixItem): void {
    const newRate = prompt(`Enter new fare for ${item.fromNode} (${this.getNodeName(item.fromNode)}) → ${item.toNode} (${this.getNodeName(item.toNode)}):`, item.fare.toString());
    if (newRate !== null) {
      const parsed = parseFloat(newRate);
      if (!isNaN(parsed) && parsed > 0) {
        this.adminService.updateFareMatrix(item.id, { fare: parsed, isActive: item.isActive }).subscribe(() => {
          item.fare = parsed;
          this.snackBar.open(`Fare updated to ${parsed} ETB`, 'OK', { duration: 2500 });
        });
      }
    }
  }

  openAddPairModal(): void {
    if (!this.currentRoute || this.currentRoute.nodes.length < 2) return;
    const from = this.currentRoute.nodes[0].id;
    const to = this.currentRoute.nodes[this.currentRoute.nodes.length - 1].id;
    const fareStr = prompt(`Enter fare for ${from} → ${to}:`, '25');
    if (fareStr) {
      const fare = parseFloat(fareStr) || 25;
      this.adminService.createFareMatrix({
        routeId: this.selectedRouteId,
        fromNode: from,
        toNode: to,
        fare
      }).subscribe(() => {
        this.snackBar.open('Fare rule created.', 'OK', { duration: 2500 });
        this.loadFareMatrices();
      });
    }
  }

  openBatchGenerateModal(): void {
    this.showBatchModal = true;
  }

  confirmGenerateSymmetric(): void {
    if (!this.selectedRouteId || this.batchHopRate <= 0) return;

    this.adminService.generateSymmetricFares(this.selectedRouteId, this.batchHopRate).subscribe(() => {
      this.snackBar.open(`Tariff matrix successfully generated at ${this.batchHopRate} ETB per hop! 💰`, 'OK', {
        duration: 3500,
        panelClass: 'snack-success'
      });
      this.showBatchModal = false;
      this.loadFareMatrices();
    });
  }
}
