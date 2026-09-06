import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { authGuard, guestGuard, roleGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('AuthGuards', () => {
  let authService: AuthService;
  let router: Router;

  const mockRoute = {} as ActivatedRouteSnapshot;
  const mockState = { url: '/passenger/dashboard' } as RouterStateSnapshot;
  const mockDriverState = { url: '/driver/dashboard' } as RouterStateSnapshot;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: MatSnackBar,
          useValue: { open: vi.fn() }
        }
      ]
    });

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('authGuard', () => {
    it('should redirect unauthenticated users to /login with returnUrl', () => {
      authService.saveUser(null);
      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

      expect(result instanceof UrlTree).toBe(true);
      if (result instanceof UrlTree) {
        expect(result.toString()).toContain('/login?returnUrl=%2Fpassenger%2Fdashboard');
      }
    });

    it('should allow access when user is authenticated with a valid token', () => {
      authService.saveUser({
        id: 'test-id',
        firstName: 'Alem',
        lastName: 'Tesfaye',
        phone: '0955566778',
        role: 'passenger',
        token: 'demo-passenger-jwt-token'
      });

      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
      expect(result).toBe(true);
    });
  });

  describe('roleGuard', () => {
    it('should allow passenger to access passenger portal', () => {
      authService.saveUser({
        id: 'p-01',
        firstName: 'Alem',
        lastName: 'Tesfaye',
        phone: '0955566778',
        role: 'passenger',
        token: 'demo-passenger-jwt-token'
      });

      const guard = roleGuard(['passenger']);
      const result = TestBed.runInInjectionContext(() => guard(mockRoute, mockState));
      expect(result).toBe(true);
    });

    it('should deny passenger access to driver portal and redirect to passenger dashboard', () => {
      authService.saveUser({
        id: 'p-01',
        firstName: 'Alem',
        lastName: 'Tesfaye',
        phone: '0955566778',
        role: 'passenger',
        token: 'demo-passenger-jwt-token'
      });

      const guard = roleGuard(['driver']);
      const result = TestBed.runInInjectionContext(() => guard(mockRoute, mockDriverState));

      expect(result instanceof UrlTree).toBe(true);
      if (result instanceof UrlTree) {
        expect(result.toString()).toBe('/passenger/dashboard');
      }
    });

    it('should allow driver to access driver portal', () => {
      authService.saveUser({
        id: 'd-01',
        firstName: 'Abebe',
        lastName: 'Kebede',
        phone: '0911122334',
        role: 'driver',
        token: 'demo-driver-jwt-token'
      });

      const guard = roleGuard(['driver']);
      const result = TestBed.runInInjectionContext(() => guard(mockRoute, mockDriverState));
      expect(result).toBe(true);
    });

    it('should deny driver access to passenger portal and redirect to driver dashboard', () => {
      authService.saveUser({
        id: 'd-01',
        firstName: 'Abebe',
        lastName: 'Kebede',
        phone: '0911122334',
        role: 'driver',
        token: 'demo-driver-jwt-token'
      });

      const guard = roleGuard(['passenger']);
      const result = TestBed.runInInjectionContext(() => guard(mockRoute, mockState));

      expect(result instanceof UrlTree).toBe(true);
      if (result instanceof UrlTree) {
        expect(result.toString()).toBe('/driver/dashboard');
      }
    });

    it('should redirect unauthenticated users to /login when evaluating roleGuard', () => {
      authService.saveUser(null);

      const guard = roleGuard(['passenger']);
      const result = TestBed.runInInjectionContext(() => guard(mockRoute, mockState));

      expect(result instanceof UrlTree).toBe(true);
      if (result instanceof UrlTree) {
        expect(result.toString()).toContain('/login?returnUrl=%2Fpassenger%2Fdashboard');
      }
    });
  });

  describe('guestGuard', () => {
    it('should allow guests to access login/register', () => {
      authService.saveUser(null);
      const result = TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
      expect(result).toBe(true);
    });

    it('should redirect logged in passenger to passenger dashboard', () => {
      authService.saveUser({
        id: 'p-01',
        firstName: 'Alem',
        lastName: 'Tesfaye',
        phone: '0955566778',
        role: 'passenger',
        token: 'demo-passenger-jwt-token'
      });

      const result = TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
      expect(result instanceof UrlTree).toBe(true);
      if (result instanceof UrlTree) {
        expect(result.toString()).toBe('/passenger/dashboard');
      }
    });

    it('should redirect logged in driver to driver dashboard', () => {
      authService.saveUser({
        id: 'd-01',
        firstName: 'Abebe',
        lastName: 'Kebede',
        phone: '0911122334',
        role: 'driver',
        token: 'demo-driver-jwt-token'
      });

      const result = TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockDriverState));
      expect(result instanceof UrlTree).toBe(true);
      if (result instanceof UrlTree) {
        expect(result.toString()).toBe('/driver/dashboard');
      }
    });
  });
});
