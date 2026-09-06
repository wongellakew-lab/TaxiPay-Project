import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/services/auth.service';

/**
 * Validates that input is either a valid email format or a valid Ethiopian phone number.
 */
export function emailOrPhoneValidator(): ValidatorFn {
  const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
  const phoneRegex = /^(09|07)\d{8}$/;

  return (control: AbstractControl): ValidationErrors | null => {
    const val = (control.value || '').trim();
    if (!val) return null;

    if (val.includes('@') || /[a-zA-Z]/.test(val)) {
      if (!emailRegex.test(val)) {
        return { invalidEmail: true };
      }
    } else {
      if (!phoneRegex.test(val)) {
        return { invalidPhone: true };
      }
    }
    return null;
  };
}

/**
 * Validates password requirements:
 * - Minimum 8 characters
 * - At least one uppercase letter
 * - At least one lowercase letter
 * - At least one number
 */
export function strongPasswordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const val = control.value || '';
    if (!val) return null;

    const hasMinLength = val.length >= 8;
    const hasUpperCase = /[A-Z]/.test(val);
    const hasLowerCase = /[a-z]/.test(val);
    const hasNumber = /\d/.test(val);

    const valid = hasMinLength && hasUpperCase && hasLowerCase && hasNumber;
    if (!valid) {
      return {
        weakPassword: {
          hasMinLength,
          hasUpperCase,
          hasLowerCase,
          hasNumber
        }
      };
    }
    return null;
  };
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  public auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  selectedRole = signal<'passenger' | 'driver'>('passenger');
  hidePassword = true;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  showForgotHelp = signal(false);

  private returnUrl: string | null = null;

  passengerForm!: FormGroup;
  driverForm!: FormGroup;

  get selectedTabIndex(): number {
    return this.selectedRole() === 'driver' ? 1 : 0;
  }
  set selectedTabIndex(index: number) {
    this.setRole(index === 1 ? 'driver' : 'passenger');
  }

  get pControls() {
    return this.passengerForm?.controls;
  }

  get dControls() {
    return this.driverForm?.controls;
  }

  ngOnInit(): void {
    this.initForms();

    this.route.queryParams.subscribe(params => {
      if (params['role'] === 'driver') {
        this.selectedRole.set('driver');
      } else if (params['role'] === 'passenger') {
        this.selectedRole.set('passenger');
      }

      if (params['returnUrl']) {
        this.returnUrl = params['returnUrl'];
      }
    });
  }

  private initForms(): void {
    this.passengerForm = this.fb.group({
      identifier: ['', [Validators.required, emailOrPhoneValidator()]],
      password: ['', [Validators.required, strongPasswordValidator()]],
      rememberMe: [false]
    });

    this.driverForm = this.fb.group({
      identifier: ['', [Validators.required, emailOrPhoneValidator()]],
      password: ['', [Validators.required, strongPasswordValidator()]],
      rememberMe: [false]
    });
  }

  setRole(role: 'passenger' | 'driver'): void {
    if (this.selectedRole() !== role) {
      this.selectedRole.set(role);
      this.errorMessage.set(null);
      this.showForgotHelp.set(false);
      
      // Clean form reset when switching between roles
      this.passengerForm.reset({ identifier: '', password: '', rememberMe: false });
      this.driverForm.reset({ identifier: '', password: '', rememberMe: false });
    }
  }

  onTabChange(index: number): void {
    this.setRole(index === 1 ? 'driver' : 'passenger');
  }

  toggleForgotHelp(): void {
    this.showForgotHelp.set(!this.showForgotHelp());
  }

  loginPassenger(): void {
    this.errorMessage.set(null);

    if (this.passengerForm.invalid) {
      this.passengerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const { identifier, password } = this.passengerForm.value;
    const cleanId = (identifier || '').trim();

    this.auth.loginPassenger({ phone: cleanId, password }, this.returnUrl || undefined).subscribe({
      next: () => {
        this.isLoading.set(false);
      },
      error: (err: Error) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.message || 'Passenger sign in failed. Please verify credentials.');
      }
    });
  }

  loginDriver(): void {
    this.errorMessage.set(null);

    if (this.driverForm.invalid) {
      this.driverForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const { identifier, password } = this.driverForm.value;
    const cleanId = (identifier || '').trim();

    this.auth.loginDriver({ phone: cleanId, password }, this.returnUrl || undefined).subscribe({
      next: () => {
        this.isLoading.set(false);
      },
      error: (err: Error) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.message || 'Driver sign in failed. Please verify driver phone and password.');
      }
    });
  }
}
