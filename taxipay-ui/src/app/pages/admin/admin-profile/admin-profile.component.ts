import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminProfile } from '../../../core/models/admin.models';

@Component({
  selector: 'app-admin-profile',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatProgressSpinnerModule, MatDividerModule],
  templateUrl: './admin-profile.component.html',
  styleUrl: './admin-profile.component.scss'
})
export class AdminProfileComponent implements OnInit {
  private adminService = inject(AdminService);
  public auth = inject(AuthService);

  profile = signal<AdminProfile | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.adminService.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load admin profile.');
        this.loading.set(false);
      }
    });
  }
}
