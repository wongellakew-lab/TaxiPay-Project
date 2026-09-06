import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminService } from '../../../core/services/admin.service';
import { AdminNode, CreateRouteRequest, RouteDetail } from '../../../core/models/admin.models';

@Component({
  selector: 'app-admin-routes',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatTooltipModule
  ],
  templateUrl: './admin-routes.component.html',
  styleUrl: './admin-routes.component.scss'
})
export class AdminRoutesComponent implements OnInit {
  private adminService = inject(AdminService);
  private snackBar = inject(MatSnackBar);

  routes = signal<RouteDetail[]>([]);

  showModal = false;
  editingRouteId: string | null = null;

  formRouteCode = '';
  formRouteName = '';
  formNodes: AdminNode[] = [];

  ngOnInit(): void {
    this.loadRoutes();
  }

  loadRoutes(): void {
    this.adminService.getRoutes().subscribe(r => this.routes.set(r));
  }

  toggleStatus(route: RouteDetail): void {
    this.adminService.toggleRouteStatus(route.id).subscribe(() => {
      route.isActive = !route.isActive;
      this.snackBar.open(`Route ${route.routeCode} is now ${route.isActive ? 'Active' : 'Disabled'}.`, 'OK', { duration: 3000 });
    });
  }

  deleteRoute(route: RouteDetail): void {
    if (confirm(`Are you sure you want to delete route ${route.routeCode} (${route.name})?`)) {
      this.adminService.deleteRoute(route.id).subscribe(() => {
        this.snackBar.open(`Route ${route.routeCode} deleted.`, 'OK', { duration: 3000 });
        this.loadRoutes();
      });
    }
  }

  openCreateModal(): void {
    this.editingRouteId = null;
    this.formRouteCode = '';
    this.formRouteName = '';
    this.formNodes = [
      { id: 'A', name: 'Start Terminal', latitude: 9.0145, longitude: 38.7468 },
      { id: 'B', name: 'Mid Junction', latitude: 9.0225, longitude: 38.7556 },
      { id: 'C', name: 'End Terminal', latitude: 9.0345, longitude: 38.7688 }
    ];
    this.showModal = true;
  }

  openEditModal(route: RouteDetail): void {
    this.editingRouteId = route.id;
    this.formRouteCode = route.routeCode;
    this.formRouteName = route.name;
    this.formNodes = JSON.parse(JSON.stringify(route.nodes));
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  addNode(): void {
    const letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const nextLetter = letters[this.formNodes.length] || `N${this.formNodes.length + 1}`;
    this.formNodes.push({
      id: nextLetter,
      name: `Station ${nextLetter}`,
      latitude: 9.0200 + (this.formNodes.length * 0.005),
      longitude: 38.7500 + (this.formNodes.length * 0.005)
    });
  }

  removeNode(index: number): void {
    this.formNodes.splice(index, 1);
    const letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    this.formNodes.forEach((n, i) => {
      n.id = letters[i] || `N${i + 1}`;
    });
  }

  isFormValid(): boolean {
    return (
      this.formRouteCode.trim().length > 0 &&
      this.formRouteName.trim().length > 0 &&
      this.formNodes.length >= 2 &&
      this.formNodes.every(n => n.name.trim().length > 0)
    );
  }

  saveRoute(): void {
    if (!this.isFormValid()) return;

    if (this.editingRouteId) {
      this.adminService.updateRoute(this.editingRouteId, {
        name: this.formRouteName,
        routeCode: this.formRouteCode,
        nodes: this.formNodes,
        isActive: true
      }).subscribe(() => {
        this.snackBar.open(`Route ${this.formRouteCode} updated successfully.`, 'OK', { duration: 3500, panelClass: 'snack-success' });
        this.closeModal();
        this.loadRoutes();
      });
    } else {
      this.adminService.createRoute({
        name: this.formRouteName,
        routeCode: this.formRouteCode,
        nodes: this.formNodes,
        isActive: true
      }).subscribe(() => {
        this.snackBar.open(`Route ${this.formRouteCode} created successfully! 🛣️`, 'OK', { duration: 3500, panelClass: 'snack-success' });
        this.closeModal();
        this.loadRoutes();
      });
    }
  }
}
