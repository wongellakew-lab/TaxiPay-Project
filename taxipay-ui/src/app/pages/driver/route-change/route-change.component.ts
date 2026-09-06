import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { AuthService } from '../../../core/services/auth.service';
import { DriverTransferRequestStore } from '../../../core/stores/driver-transfer-request.store';
import { RouteTimelineComponent } from '../../../shared/components/route-timeline/route-timeline.component';

@Component({
  selector: 'app-route-change',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    RouteTimelineComponent
  ],
  templateUrl: './route-change.component.html',
  styleUrl: './route-change.component.scss'
})
export class RouteChangeComponent implements OnInit {
  public auth = inject(AuthService);
  public store = inject(DriverTransferRequestStore);

  requestedRouteId = '';
  requestReason = '';

  ngOnInit(): void {
    const user = this.auth.currentUser();
    this.store.loadCurrentRequest(user?.id);
    this.store.loadAvailableRoutes();
  }

  submitRequest(): void {
    if (!this.requestedRouteId || !this.requestReason.trim()) return;

    this.store.submitTransferRequest(this.requestedRouteId, this.requestReason, () => {
      this.requestReason = '';
    });
  }
}
