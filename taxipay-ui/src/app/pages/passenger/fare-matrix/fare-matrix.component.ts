import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouteService } from '../../../core/services/route.service';
import { RouteDetail } from '../../../core/models/admin.models';
import { RouteTimelineComponent } from '../../../shared/components/route-timeline/route-timeline.component';

@Component({
  selector: 'app-fare-matrix',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTableModule,
    MatProgressBarModule,
    RouteTimelineComponent
  ],
  templateUrl: './fare-matrix.component.html',
  styleUrl: './fare-matrix.component.scss'
})
export class FareMatrixComponent implements OnInit {
  routeService = inject(RouteService);

  routes = signal<RouteDetail[]>(this.routeService.routes());
  activeRoute: RouteDetail | null = this.routeService.routes()[0] || null;
  activeRouteCode = this.routeService.routes()[0]?.routeCode || 'BM-001';

  selectedFrom = 'A';
  selectedTo = 'F';
  computedFare = signal(25);

  ngOnInit(): void {
    this.routeService.getRoutes().subscribe(routes => {
      this.routes.set(routes);
      if (routes.length > 0) {
        this.activeRoute = routes[0];
        this.activeRouteCode = routes[0].routeCode;
        this.selectedFrom = routes[0].nodes[0]?.id || 'A';
        this.selectedTo = routes[0].nodes[routes[0].nodes.length - 1]?.id || 'F';
        this.calculateCurrentFare();
      }
    });
  }

  changeRoute(code: string): void {
    this.activeRouteCode = code;
    this.activeRoute = this.routes().find(r => r.routeCode === code) || this.routes()[0];
    if (this.activeRoute && this.activeRoute.nodes.length > 0) {
      this.selectedFrom = this.activeRoute.nodes[0].id;
      this.selectedTo = this.activeRoute.nodes[this.activeRoute.nodes.length - 1].id;
      this.calculateCurrentFare();
    }
  }

  onBoardingChange(fromId: string): void {
    this.selectedFrom = fromId;
    this.calculateCurrentFare();
  }

  onDestinationChange(toId: string): void {
    this.selectedTo = toId;
    this.calculateCurrentFare();
  }

  onTimelineFromSelected(fromId: string): void {
    this.selectedFrom = fromId;
    this.calculateCurrentFare();
  }

  onTimelineToSelected(toId: string): void {
    this.selectedTo = toId;
    this.calculateCurrentFare();
  }

  calculateCurrentFare(): void {
    const localFare = this.getLocalFare(this.selectedFrom, this.selectedTo);
    this.computedFare.set(localFare);

    if (this.activeRoute) {
      this.routeService.calculateFare(this.selectedFrom, this.selectedTo, this.activeRoute.id).subscribe({
        next: fare => {
          if (fare > 0) {
            this.computedFare.set(fare);
          }
        },
        error: () => {}
      });
    }
  }

  getLocalFare(from: string, to: string): number {
    if (this.activeRoute && this.activeRoute.nodes.length > 0) {
      const idxFrom = this.activeRoute.nodes.findIndex(n => n.id === from);
      const idxTo = this.activeRoute.nodes.findIndex(n => n.id === to);
      if (idxFrom >= 0 && idxTo >= 0 && idxFrom !== idxTo) {
        const hops = Math.abs(idxTo - idxFrom);
        return Math.max(5, hops * 5);
      }
    }
    const defaultNodes = ['A', 'B', 'C', 'D', 'E', 'F'];
    const idxA = defaultNodes.indexOf(from);
    const idxB = defaultNodes.indexOf(to);
    if (idxA >= 0 && idxB >= 0 && idxA !== idxB) {
      return Math.max(5, Math.abs(idxB - idxA) * 5);
    }
    return 25;
  }

  getNodeName(id: string): string {
    const node = this.activeRoute?.nodes.find(n => n.id === id);
    return node ? node.name : `Stop ${id}`;
  }
}
