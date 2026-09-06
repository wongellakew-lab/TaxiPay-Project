import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { DriverService } from '../../../core/services/driver.service';
import { DriverResponse } from '../../../core/models/driver.models';

@Component({
  selector: 'app-driver-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './driver-profile.component.html',
  styleUrl: './driver-profile.component.scss'
})
export class DriverProfileComponent implements OnInit {
  auth = inject(AuthService);
  driverService = inject(DriverService);
  snackBar = inject(MatSnackBar);

  profile = signal<DriverResponse | null>(null);
  firstName = '';
  lastName = '';
  phone = '';
  isSaving = signal(false);

  ngOnInit(): void {
    const driverId = this.auth.currentUser()?.id;
    if (driverId) {
      this.driverService.getProfile(driverId).subscribe(p => {
        this.profile.set(p);
        this.firstName = p.firstName;
        this.lastName = p.lastName;
        this.phone = p.phone;
      });
    }
  }

  saveProfile(): void {
    const driverId = this.auth.currentUser()?.id;
    if (!driverId) return;
    this.isSaving.set(true);

    this.driverService.updateProfile(driverId, {
      firstName: this.firstName,
      lastName: this.lastName,
      phone: this.phone
    }).subscribe({
      next: updated => {
        this.isSaving.set(false);
        this.profile.set(updated);
        this.snackBar.open('✅ Profile details updated successfully!', 'OK', {
          duration: 3500,
          panelClass: 'snack-success'
        });
      },
      error: () => this.isSaving.set(false)
    });
  }
}
