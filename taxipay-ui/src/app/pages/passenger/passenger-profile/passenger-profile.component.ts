import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { PassengerService } from '../../../core/services/passenger.service';
import { WalletStore } from '../../../core/stores/wallet.store';
import { PassengerProfileResponse, TopUpWalletRequest, UpdatePassengerProfileRequest } from '../../../core/models/passenger.models';

@Component({
  selector: 'app-passenger-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './passenger-profile.component.html',
  styleUrl: './passenger-profile.component.scss'
})
export class PassengerProfileComponent implements OnInit {
  auth = inject(AuthService);
  passengerService = inject(PassengerService);
  walletStore = inject(WalletStore);
  snackBar = inject(MatSnackBar);

  isLoading = signal(true);
  isEditing = signal(false);
  isSavingProfile = signal(false);
  profile = signal<PassengerProfileResponse | null>(null);

  editFirstName = '';
  editLastName = '';
  editPhone = '';

  profileSaveError: string | null = null;
  firstNameError: string | null = null;
  lastNameError: string | null = null;
  phoneError: string | null = null;

  topUpAmount = 100;
  presetAmounts = [50, 100, 200, 500];

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    const user = this.auth.currentUser();
    const passengerId = user?.id;
    if (!passengerId) {
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.passengerService.getProfile(passengerId).subscribe({
      next: p => {
        this.profile.set(p);
        if (p.walletBalance !== undefined) {
          this.walletStore.setBalance(p.walletBalance);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Could not load profile from database.', 'Dismiss', { duration: 4000, panelClass: 'snack-error' });
      }
    });
  }

  startEditing(): void {
    const p = this.profile();
    if (!p) return;

    this.editFirstName = p.firstName;
    this.editLastName = p.lastName;
    this.editPhone = p.phone;

    this.profileSaveError = null;
    this.firstNameError = null;
    this.lastNameError = null;
    this.phoneError = null;
    this.isEditing.set(true);
  }

  cancelEditing(): void {
    this.isEditing.set(false);
    this.profileSaveError = null;
    this.firstNameError = null;
    this.lastNameError = null;
    this.phoneError = null;
  }

  isEditFormValid(): boolean {
    return !!(this.editFirstName.trim() && this.editLastName.trim() && this.editPhone.trim());
  }

  /** Validates phone format: must be 10 digits starting with 09 or 07, or start with +251 */
  private isValidPhone(phone: string): boolean {
    const t = phone.trim();
    return /^(09|07)\d{8}$/.test(t) || /^\+2519\d{8}$/.test(t) || /^\+2517\d{8}$/.test(t);
  }

  /** Runs client-side validation and populates per-field error strings. Returns true if valid. */
  private validateEditForm(): boolean {
    let valid = true;
    this.firstNameError = null;
    this.lastNameError = null;
    this.phoneError = null;
    this.profileSaveError = null;

    if (!this.editFirstName.trim()) {
      this.firstNameError = 'First name is required.';
      valid = false;
    } else if (this.editFirstName.trim().length < 2) {
      this.firstNameError = 'First name must be at least 2 characters.';
      valid = false;
    }

    if (!this.editLastName.trim()) {
      this.lastNameError = 'Last name is required.';
      valid = false;
    } else if (this.editLastName.trim().length < 2) {
      this.lastNameError = 'Last name must be at least 2 characters.';
      valid = false;
    }

    if (!this.editPhone.trim()) {
      this.phoneError = 'Phone number is required.';
      valid = false;
    } else if (!this.isValidPhone(this.editPhone)) {
      this.phoneError = 'Enter a valid Ethiopian phone number (e.g. 0911000000).';
      valid = false;
    }

    return valid;
  }

  saveProfile(): void {
    if (!this.validateEditForm()) return;

    const user = this.auth.currentUser();
    const passengerId = user?.id;
    if (!passengerId) {
      this.profileSaveError = 'You must be logged in to update your profile.';
      return;
    }

    this.isSavingProfile.set(true);
    this.profileSaveError = null;
    this.firstNameError = null;
    this.lastNameError = null;
    this.phoneError = null;

    const req: UpdatePassengerProfileRequest = {
      firstName: this.editFirstName.trim(),
      lastName: this.editLastName.trim(),
      phone: this.editPhone.trim()
    };

    this.passengerService.updateProfile(passengerId, req).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.isSavingProfile.set(false);
        this.isEditing.set(false);

        // Keep localStorage session user display name in sync so the navbar
        // shows the new name immediately without requiring logout/login.
        if (user) {
          const updatedUser = {
            ...user,
            firstName: updated.firstName,
            lastName: updated.lastName,
            phone: updated.phone
          };
          this.auth.saveUser(updatedUser);
        }

        this.snackBar.open('✓ Profile updated and saved to database!', 'OK', { duration: 3500, panelClass: 'snack-success' });
      },
      error: (err) => {
        this.isSavingProfile.set(false);
        const serverMessage = this.extractErrorMessage(err);
        if (serverMessage.toLowerCase().includes('phone')) {
          this.phoneError = serverMessage;
        } else if (serverMessage.toLowerCase().includes('first name') || serverMessage.toLowerCase().includes('firstname')) {
          this.firstNameError = serverMessage;
        } else if (serverMessage.toLowerCase().includes('last name') || serverMessage.toLowerCase().includes('lastname')) {
          this.lastNameError = serverMessage;
        } else {
          this.profileSaveError = serverMessage;
        }
      }
    });
  }

  /** Parse HttpErrorResponse into a human-readable message */
  private extractErrorMessage(err: any): string {
    if (!err) return 'An unknown error occurred.';
    const e = err.error;
    if (e && typeof e === 'object') {
      if (e.message) return e.message;
      if (e.detail) return e.detail;
      if (e.title) return e.title;
      if (e.errors) {
        const firstKey = Object.keys(e.errors)[0];
        if (firstKey && e.errors[firstKey]?.[0]) return e.errors[firstKey][0];
      }
    }
    if (err.status === 409) return 'This phone number is already registered. Please use a different phone number.';
    if (err.status === 400) return 'Invalid data submitted. Please check your input.';
    if (err.status === 401) return 'Your session has expired. Please log in again.';
    if (err.status === 404) return 'Passenger profile not found.';
    if (err.status === 0) return 'Cannot reach the server. Check your network connection.';
    return 'Failed to save profile changes. Please try again.';
  }

  getFullName(): string {
    const p = this.profile();
    if (p) {
      if (p.fullName && p.fullName.trim()) return p.fullName.trim();
      const combined = `${p.firstName || ''} ${p.lastName || ''}`.trim();
      if (combined) return combined;
    }
    const u = this.auth.currentUser();
    if (u) {
      const combined = `${u.firstName || ''} ${u.lastName || ''}`.trim();
      if (combined) return combined;
    }
    return 'Commuter';
  }

  getInitials(): string {
    const p = this.profile();
    if (p) {
      const first = p.firstName ? p.firstName.trim()[0] : '';
      const last = p.lastName ? p.lastName.trim()[0] : '';
      const initials = (first + last).toUpperCase();
      if (initials) return initials;
    }
    const u = this.auth.currentUser();
    if (u) {
      const first = u.firstName ? u.firstName.trim()[0] : '';
      const last = u.lastName ? u.lastName.trim()[0] : '';
      return (first + last).toUpperCase() || 'P';
    }
    return 'P';
  }

  selectPreset(amount: number): void {
    this.topUpAmount = amount;
  }

  isValidTopUpAmount(): boolean {
    return this.topUpAmount >= 10 && this.topUpAmount <= 10000;
  }

  submitTopUp(): void {
    if (!this.isValidTopUpAmount()) return;

    const user = this.auth.currentUser();
    const passengerId = user?.id;
    if (!passengerId) return;

    this.walletStore.topUp(this.topUpAmount, 'Telebirr', passengerId);
  }
}
