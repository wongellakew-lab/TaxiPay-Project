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
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { RouteService } from '../../../core/services/route.service';
import { RouteDetail } from '../../../core/models/admin.models';

/**
 * Validates password match between two control fields.
 */
export function passwordMatchValidator(passwordKey: string, confirmPasswordKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordKey);
    const confirmPassword = group.get(confirmPasswordKey);

    if (!password || !confirmPassword) return null;

    if (confirmPassword.errors && !confirmPassword.errors['passwordMismatch']) {
      return null;
    }

    if (password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    } else {
      if (confirmPassword.hasError('passwordMismatch')) {
        confirmPassword.setErrors(null);
      }
      return null;
    }
  };
}

/**
 * Validates strong password rules:
 * - 8+ characters
 * - Uppercase letter
 * - Lowercase letter
 * - Number
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
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  public auth = inject(AuthService);
  private routeService = inject(RouteService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  selectedTabIndex = 0;
  hidePassword = true;
  hideConfirmPassword = true;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  routes = signal<RouteDetail[]>([]);

  pForm!: FormGroup;
  dForm!: FormGroup;

  ngOnInit(): void {
    const phoneRegex = /^(09|07)\d{8}$/;

    this.routeService.getRoutes().subscribe({
      next: data => this.routes.set(data.filter(r => r.isActive)),
      error: () => {}
    });

    this.pForm = this.fb.group(
      {
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        phone: ['', [Validators.required, Validators.pattern(phoneRegex)]],
        password: ['', [Validators.required, strongPasswordValidator()]],
        confirmPassword: ['', Validators.required]
      },
      { validators: passwordMatchValidator('password', 'confirmPassword') }
    );

    this.dForm = this.fb.group(
      {
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        phone: ['', [Validators.required, Validators.pattern(phoneRegex)]],
        licenseNumber: ['', Validators.required],
        preferredRouteId: [null],
        password: ['', [Validators.required, strongPasswordValidator()]],
        confirmPassword: ['', Validators.required]
      },
      { validators: passwordMatchValidator('password', 'confirmPassword') }
    );

    this.route.queryParams.subscribe(params => {
      if (params['role'] === 'driver') {
        this.selectedTabIndex = 1;
      } else if (params['role'] === 'passenger') {
        this.selectedTabIndex = 0;
      }
    });
  }

  get pControls() {
    return this.pForm.controls;
  }

  get dControls() {
    return this.dForm.controls;
  }

  onTabChange(index: number): void {
    this.selectedTabIndex = index;
    this.errorMessage.set(null);
  }

  registerPassenger(): void {
    this.errorMessage.set(null);

    if (this.pForm.invalid) {
      this.pForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const { firstName, lastName, phone, password } = this.pForm.value;

    this.auth.registerPassenger({ firstName, lastName, phone, password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/login'], { queryParams: { role: 'passenger' } });
      },
      error: (err: Error) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.message || 'Registration failed. Phone number may already be in use.');
      }
    });
  }

  registerDriver(): void {
    this.errorMessage.set(null);

    if (this.dForm.invalid) {
      this.dForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const { firstName, lastName, phone, licenseNumber, preferredRouteId, password } = this.dForm.value;

    this.auth.registerDriver({
      firstName,
      lastName,
      phone,
      licenseNumber,
      preferredRouteId: preferredRouteId || undefined,
      password
    }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/login'], { queryParams: { role: 'driver' } });
      },
      error: (err: Error) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.message || 'Driver registration failed. Phone or license may already exist.');
      }
    });
  }
}
