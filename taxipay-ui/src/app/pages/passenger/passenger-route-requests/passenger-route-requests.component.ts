import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { RouteRequestStore } from '../../../core/stores/route-request.store';
import { PassengerRouteRequestDto } from '../../../core/models/passenger.models';

@Component({
  selector: 'app-passenger-route-requests',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './passenger-route-requests.component.html',
  styleUrl: './passenger-route-requests.component.scss'
})
export class PassengerRouteRequestsComponent implements OnInit {
  private fb = inject(FormBuilder);
  public auth = inject(AuthService);
  public routeRequestStore = inject(RouteRequestStore);

  selectedStatus = signal<string>('all');
  searchQuery = signal<string>('');

  // Sorting
  sortColumn = signal<string>('date');
  sortDirection = signal<'asc' | 'desc'>('desc');

  filteredRequests = computed(() => {
    let list = [...this.routeRequestStore.requests()];
    const status = this.selectedStatus();
    const query = this.searchQuery().trim().toLowerCase();

    if (status !== 'all') {
      list = list.filter(r => r.status.toLowerCase() === status.toLowerCase());
    }

    if (query) {
      list = list.filter(
        r =>
          r.fromLocation.toLowerCase().includes(query) ||
          r.toLocation.toLowerCase().includes(query) ||
          (r.description && r.description.toLowerCase().includes(query))
      );
    }

    // Apply Sorting
    const col = this.sortColumn();
    const dir = this.sortDirection() === 'asc' ? 1 : -1;

    list.sort((a, b) => {
      let comparison = 0;
      switch (col) {
        case 'path':
          comparison = `${a.fromLocation} ${a.toLocation}`.localeCompare(`${b.fromLocation} ${b.toLocation}`);
          break;
        case 'details':
          comparison = (a.description || '').localeCompare(b.description || '');
          break;
        case 'status':
          comparison = a.status.localeCompare(b.status);
          break;
        case 'date':
        default:
          comparison = new Date(a.requestedAt).getTime() - new Date(b.requestedAt).getTime();
          break;
      }
      return dir === 1 ? comparison : -comparison;
    });

    return list;
  });

  showSubmitModal = false;
  requestForm!: FormGroup;

  ngOnInit(): void {
    this.requestForm = this.fb.group({
      fromLocation: ['', Validators.required],
      toLocation: ['', Validators.required],
      description: ['']
    });

    const user = this.auth.currentUser();
    if (user?.id) {
      this.routeRequestStore.loadPassengerRequests(user.id);
    }
  }

  get formControls() {
    return this.requestForm.controls;
  }

  setStatusFilter(status: string): void {
    this.selectedStatus.set(status);
  }

  toggleSort(column: string): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
  }

  getSortIcon(column: string): string {
    if (this.sortColumn() !== column) return 'unfold_more';
    return this.sortDirection() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  getCount(status: string): number {
    return this.routeRequestStore.requests().filter(r => r.status.toLowerCase() === status.toLowerCase()).length;
  }

  openSubmitModal(): void {
    this.requestForm.reset({ fromLocation: '', toLocation: '', description: '' });
    this.showSubmitModal = true;
  }

  submitRequest(): void {
    if (this.requestForm.invalid) {
      this.requestForm.markAllAsTouched();
      return;
    }

    const user = this.auth.currentUser();
    if (!user) return;

    const { fromLocation, toLocation, description } = this.requestForm.value;
    this.routeRequestStore.submitRequest(user.id, { fromLocation, toLocation, description });
    this.showSubmitModal = false;
  }
}
