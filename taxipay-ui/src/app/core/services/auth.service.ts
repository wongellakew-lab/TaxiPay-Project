import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OverlayContainer } from '@angular/cdk/overlay';
import { catchError, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JwtClaims, UserProfile, UserRole } from '../models/auth.models';
import { DriverLoginRequest, DriverLoginResponse, CreateDriverRequest, DriverResponse } from '../models/driver.models';
import { PassengerLoginRequest, PassengerLoginResponse, RegisterRequest, PassengerResponse } from '../models/passenger.models';
import { AdminLoginRequest, AdminLoginResponse } from '../models/admin.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private overlayContainer = inject(OverlayContainer, { optional: true });

  private readonly STORAGE_KEY = 'taxipay_auth_user';
  private readonly THEME_KEY = 'taxipay_theme';

  // Signals
  public currentUser = signal<UserProfile | null>(this.loadStoredUser());
  public theme = signal<'dark' | 'light'>(this.loadStoredTheme());

  public isLoggedIn = computed(() => {
    const user = this.currentUser();
    if (!user || !user.token) return false;
    return !this.isTokenExpired(user.token);
  });

  public userRole = computed<UserRole>(() => {
    if (!this.isLoggedIn()) return 'guest';
    return this.currentUser()?.role || 'guest';
  });

  public isDriver = computed(() => this.isLoggedIn() && this.userRole() === 'driver');
  public isPassenger = computed(() => this.isLoggedIn() && this.userRole() === 'passenger');
  public isAdmin = computed(() => this.isLoggedIn() && this.userRole() === 'admin');

  constructor() {
    this.applyTheme(this.theme());
    this.validateSessionOnStartup();
  }

  private validateSessionOnStartup(): void {
    const user = this.currentUser();
    if (user && user.token && this.isTokenExpired(user.token)) {
      this.saveUser(null);
    }
  }

  public decodeToken(token: string): JwtClaims | null {
    if (!token) return null;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = parts[1];
      const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
      const jsonString = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonString);
    } catch {
      return null;
    }
  }

  public isTokenExpired(token: string): boolean {
    if (!token) return true;

    const claims = this.decodeToken(token);
    if (!claims || !claims.exp) return false;

    const now = Math.floor(Date.now() / 1000);
    return claims.exp < now;
  }

  public extractRoleFromToken(token: string, fallback: UserRole = 'guest'): UserRole {
    if (!token) return fallback;

    const claims = this.decodeToken(token);
    if (!claims) return fallback;

    const role = (claims.role || claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'])?.toLowerCase();
    if (role === 'admin' || role === 'superadmin') return 'admin';
    if (role === 'driver') return 'driver';
    if (role === 'passenger') return 'passenger';

    return fallback;
  }

  public toggleTheme(): void {
    const next = this.theme() === 'dark' ? 'light' : 'dark';
    this.setTheme(next);
  }

  public setTheme(theme: 'dark' | 'light'): void {
    this.theme.set(theme);
    localStorage.setItem(this.THEME_KEY, theme);
    this.applyTheme(theme);
  }

  private applyTheme(theme: 'dark' | 'light'): void {
    const root = document.documentElement;
    const body = document.body;

    root.setAttribute('data-theme', theme);
    if (body) {
      body.setAttribute('data-theme', theme);
    }

    if (theme === 'dark') {
      root.classList.add('dark', 'dark-theme');
      root.classList.remove('light', 'light-theme');
      if (body) {
        body.classList.add('dark', 'dark-theme');
        body.classList.remove('light', 'light-theme');
      }
    } else {
      root.classList.add('light', 'light-theme');
      root.classList.remove('dark', 'dark-theme');
      if (body) {
        body.classList.add('light', 'light-theme');
        body.classList.remove('dark', 'dark-theme');
      }
    }

    // Synchronize Angular Material CDK Overlay Container
    const overlayElem = this.overlayContainer?.getContainerElement();
    if (overlayElem) {
      overlayElem.setAttribute('data-theme', theme);
      if (theme === 'dark') {
        overlayElem.classList.add('dark', 'dark-theme');
        overlayElem.classList.remove('light', 'light-theme');
      } else {
        overlayElem.classList.add('light', 'light-theme');
        overlayElem.classList.remove('dark', 'dark-theme');
      }
    }

    // Also update any existing overlay containers in the DOM
    const overlays = document.querySelectorAll('.cdk-overlay-container');
    overlays.forEach(el => {
      el.setAttribute('data-theme', theme);
      if (theme === 'dark') {
        el.classList.add('dark', 'dark-theme');
        el.classList.remove('light', 'light-theme');
      } else {
        el.classList.add('light', 'light-theme');
        el.classList.remove('dark', 'dark-theme');
      }
    });
  }

  private loadStoredTheme(): 'dark' | 'light' {
    const saved = localStorage.getItem(this.THEME_KEY) as 'dark' | 'light' | null;
    return saved === 'light' ? 'light' : 'dark';
  }

  private loadStoredUser(): UserProfile | null {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY);
      if (!json) return null;
      const parsed: UserProfile = JSON.parse(json);
      if (parsed && parsed.token && this.isTokenExpired(parsed.token)) {
        localStorage.removeItem(this.STORAGE_KEY);
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }

  public saveUser(user: UserProfile | null): void {
    if (user) {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(user));
    } else {
      localStorage.removeItem(this.STORAGE_KEY);
    }
    this.currentUser.set(user);
  }

  public getDashboardRoute(role?: UserRole): string {
    const targetRole = role || this.userRole();
    if (targetRole === 'admin') return '/admin/dashboard';
    if (targetRole === 'driver') return '/driver/dashboard';
    if (targetRole === 'passenger') return '/passenger/dashboard';
    return '/login';
  }

  public loginAdmin(req: AdminLoginRequest, returnUrl?: string) {
    return this.http.post<AdminLoginResponse>(`${environment.apiUrl}/admins/login`, req).pipe(
      tap(res => {
        const verifiedRole = this.extractRoleFromToken(res.accessToken, 'admin');
        const user: UserProfile = {
          id: res.admin.id,
          firstName: res.admin.firstName,
          lastName: res.admin.lastName,
          phone: res.admin.phone,
          email: res.admin.email,
          role: verifiedRole,
          token: res.accessToken,
          tokenExpiresAt: res.tokenExpiresAt,
          adminData: res.admin
        };
        this.saveUser(user);
        this.snackBar.open(`Welcome to Executive Console, ${user.firstName}! 🛡️`, 'OK', { duration: 3500, panelClass: 'snack-success' });

        if (returnUrl && returnUrl.startsWith('/admin') && !returnUrl.includes('login')) {
          this.router.navigateByUrl(returnUrl);
        } else {
          this.router.navigate(['/admin/dashboard']);
        }
      }),
      catchError((err: HttpErrorResponse) => {
        const errorMsg = this.formatErrorMessage(err, 'Admin authentication failed. Verify email and password.');
        this.snackBar.open(errorMsg, 'Dismiss', { duration: 4500, panelClass: 'snack-error' });
        return throwError(() => new Error(errorMsg));
      })
    );
  }

  public loginDriver(req: DriverLoginRequest, returnUrl?: string) {
    return this.http.post<DriverLoginResponse>(`${environment.apiUrl}/drivers/login`, req).pipe(
      tap(res => {
        const verifiedRole = this.extractRoleFromToken(res.accessToken, 'driver');
        const user: UserProfile = {
          id: res.driver.id,
          driverCode: res.driver.driverId,
          firstName: res.driver.fullName.split(' ')[0] || 'Driver',
          lastName: res.driver.fullName.split(' ')[1] || '',
          phone: res.driver.phone,
          role: verifiedRole,
          token: res.accessToken,
          tokenExpiresAt: res.tokenExpiresAt,
          driverData: res.driver
        };
        this.saveUser(user);
        this.snackBar.open(`Welcome back, Captain ${user.firstName}! 🚕`, 'OK', { duration: 3500, panelClass: 'snack-success' });
        
        if (returnUrl && returnUrl.startsWith('/driver')) {
          this.router.navigateByUrl(returnUrl);
        } else {
          this.router.navigate(['/driver/dashboard']);
        }
      }),
      catchError((err: HttpErrorResponse) => {
        const errorMsg = this.formatErrorMessage(err, 'Driver login failed. Please check phone and password.');
        this.snackBar.open(errorMsg, 'Dismiss', { duration: 4500, panelClass: 'snack-error' });
        return throwError(() => new Error(errorMsg));
      })
    );
  }

  public loginPassenger(req: PassengerLoginRequest, returnUrl?: string) {
    return this.http.post<PassengerLoginResponse>(`${environment.apiUrl}/passengers/login`, req).pipe(
      tap(res => {
        const verifiedRole = this.extractRoleFromToken(res.accessToken, 'passenger');
        const user: UserProfile = {
          id: res.passenger.id,
          firstName: res.passenger.firstName,
          lastName: res.passenger.lastName,
          phone: res.passenger.phone,
          role: verifiedRole,
          token: res.accessToken,
          tokenExpiresAt: res.tokenExpiresAt,
          passengerData: res.passenger
        };
        this.saveUser(user);
        this.snackBar.open(`Welcome back, ${user.firstName}! 🚀`, 'OK', { duration: 3500, panelClass: 'snack-success' });
        
        if (returnUrl && returnUrl.startsWith('/passenger')) {
          this.router.navigateByUrl(returnUrl);
        } else {
          this.router.navigate(['/passenger/dashboard']);
        }
      }),
      catchError((err: HttpErrorResponse) => {
        const errorMsg = this.formatErrorMessage(err, 'Passenger login failed. Please check credentials.');
        this.snackBar.open(errorMsg, 'Dismiss', { duration: 4500, panelClass: 'snack-error' });
        return throwError(() => new Error(errorMsg));
      })
    );
  }

  public registerPassenger(req: RegisterRequest) {
    return this.http.post<PassengerResponse>(`${environment.apiUrl}/passengers/register`, req).pipe(
      tap(() => {
        this.snackBar.open('Registration successful! Please sign in with your credentials.', 'OK', { duration: 4000, panelClass: 'snack-success' });
      }),
      catchError((err: HttpErrorResponse) => {
        const errorMsg = this.formatErrorMessage(err, 'Registration failed. Phone number may already be registered.');
        this.snackBar.open(errorMsg, 'Dismiss', { duration: 4500, panelClass: 'snack-error' });
        return throwError(() => new Error(errorMsg));
      })
    );
  }

  public registerDriver(req: CreateDriverRequest) {
    return this.http.post<DriverResponse>(`${environment.apiUrl}/drivers`, req).pipe(
      tap(res => {
        this.snackBar.open(`Driver registration submitted! Driver Code: ${res.driverId || 'Pending'}`, 'OK', { duration: 5000, panelClass: 'snack-success' });
      }),
      catchError((err: HttpErrorResponse) => {
        const errorMsg = this.formatErrorMessage(err, 'Driver registration failed. Phone or license number may already exist.');
        this.snackBar.open(errorMsg, 'Dismiss', { duration: 4500, panelClass: 'snack-error' });
        return throwError(() => new Error(errorMsg));
      })
    );
  }

  public getCurrentUser() {
    return this.http.get<any>(`${environment.apiUrl}/auth/me`);
  }

  public logout(sessionExpired = false): void {
    const wasAdmin = this.isAdmin();
    this.saveUser(null);

    // Clear any persistent tokens or cached form data to ensure form starts clean
    localStorage.removeItem('taxipay_remember_identifier');
    localStorage.removeItem('taxipay_remember_passenger');
    localStorage.removeItem('taxipay_remember_driver');
    sessionStorage.clear();

    if (sessionExpired) {
      this.snackBar.open('Your session has expired. Please sign in again.', 'OK', { duration: 4000, panelClass: 'snack-warning' });
    } else {
      this.snackBar.open('Logged out successfully.', 'OK', { duration: 2500, panelClass: 'snack-info' });
    }
    this.router.navigate([wasAdmin ? '/admin/login' : '/login'], { replaceUrl: true });
  }

  private formatErrorMessage(err: HttpErrorResponse, fallback: string): string {
    if (err.status === 0) {
      return 'Cannot connect to TaxiPay server. Please check your network connection.';
    }
    if (err.status === 401) {
      return 'Invalid credentials. Please verify your credentials and password.';
    }
    if (err.status === 403) {
      if (err.error?.detail) return err.error.detail;
      if (err.error?.message) return err.error.message;
      return 'Access forbidden: Your account is pending approval, rejected, or suspended.';
    }
    if (err.status === 423) {
      if (err.error?.detail) return err.error.detail;
      if (err.error?.message) return err.error.message;
      return 'Account temporarily locked due to multiple failed login attempts. Please try again later.';
    }
    if (err.status === 409) {
      return err.error?.detail || err.error?.message || 'Conflict: The phone number or license number is already in use.';
    }
    if (err.error && typeof err.error === 'object') {
      if (err.error.detail) return err.error.detail;
      if (err.error.message) return err.error.message;
      if (err.error.title) return err.error.title;
      if (err.error.errors) {
        const firstKey = Object.keys(err.error.errors)[0];
        if (firstKey && err.error.errors[firstKey].length > 0) {
          return err.error.errors[firstKey][0];
        }
      }
    }
    return fallback;
  }
}
