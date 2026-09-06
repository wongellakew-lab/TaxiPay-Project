import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;

  // Helper to create a fake JWT with specific exp and role
  const createFakeJwt = (role: string, expSecondsFromNow: number): string => {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const exp = Math.floor(Date.now() / 1000) + expSecondsFromNow;
    const payload = btoa(
      JSON.stringify({
        role,
        exp,
        nameid: 'user-123',
        name: 'Test User'
      })
    );
    return `${header}.${payload}.signature`;
  };

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

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created and start with guest role when storage is empty', () => {
    expect(service).toBeTruthy();
    expect(service.isLoggedIn()).toBe(false);
    expect(service.userRole()).toBe('guest');
    expect(service.isDriver()).toBe(false);
    expect(service.isPassenger()).toBe(false);
  });

  describe('JWT token handling', () => {
    it('should correctly decode token claims', () => {
      const token = createFakeJwt('passenger', 3600);
      const claims = service.decodeToken(token);
      expect(claims).not.toBeNull();
      expect(claims?.role).toBe('passenger');
      expect(claims?.nameid).toBe('user-123');
    });

    it('should detect expired tokens', () => {
      const expiredToken = createFakeJwt('passenger', -3600);
      expect(service.isTokenExpired(expiredToken)).toBe(true);

      const validToken = createFakeJwt('passenger', 3600);
      expect(service.isTokenExpired(validToken)).toBe(false);
    });

    it('should extract authoritative role from token', () => {
      const driverToken = createFakeJwt('driver', 3600);
      expect(service.extractRoleFromToken(driverToken)).toBe('driver');

      const passengerToken = createFakeJwt('passenger', 3600);
      expect(service.extractRoleFromToken(passengerToken)).toBe('passenger');

      expect(service.extractRoleFromToken('invalid', 'guest')).toBe('guest');
    });
  });

  describe('Login flows', () => {
    it('should login passenger successfully and navigate to passenger dashboard', () => {
      const validToken = createFakeJwt('passenger', 3600);

      service.loginPassenger({ phone: '0955566778', password: 'password123' }).subscribe(res => {
        expect(res.passenger.firstName).toBe('Alem');
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/passengers/login`);
      expect(req.request.method).toBe('POST');
      req.flush({
        accessToken: validToken,
        passenger: {
          id: 'p-100',
          firstName: 'Alem',
          lastName: 'Tesfaye',
          phone: '0955566778'
        }
      });

      expect(service.isLoggedIn()).toBe(true);
      expect(service.isPassenger()).toBe(true);
      expect(service.currentUser()?.firstName).toBe('Alem');
      expect(router.navigate).toHaveBeenCalledWith(['/passenger/dashboard']);
    });

    it('should login driver successfully and navigate to driver dashboard', () => {
      const validToken = createFakeJwt('driver', 3600);

      service.loginDriver({ phone: '0911122334', password: 'password123' }).subscribe(res => {
        expect(res.driver.driverId).toBe('AB1234');
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/drivers/login`);
      expect(req.request.method).toBe('POST');
      req.flush({
        accessToken: validToken,
        driver: {
          id: 'd-100',
          driverId: 'AB1234',
          fullName: 'Abebe Kebede',
          phone: '0911122334'
        }
      });

      expect(service.isLoggedIn()).toBe(true);
      expect(service.isDriver()).toBe(true);
      expect(service.currentUser()?.firstName).toBe('Abebe');
      expect(router.navigate).toHaveBeenCalledWith(['/driver/dashboard']);
    });
  });

  describe('Logout', () => {
    it('should clear stored session and redirect to /login', () => {
      service.saveUser({
        id: 'p-01',
        firstName: 'Alem',
        lastName: 'Tesfaye',
        phone: '0955566778',
        role: 'passenger',
        token: 'demo-passenger-jwt-token'
      });

      expect(service.isLoggedIn()).toBe(true);

      service.logout();

      expect(service.isLoggedIn()).toBe(false);
      expect(service.currentUser()).toBeNull();
      expect(router.navigate).toHaveBeenCalledWith(['/login'], { replaceUrl: true });
    });
  });

  describe('Theme Management', () => {
    it('should initialize theme, update DOM classes and set data-theme attribute', () => {
      expect(service.theme()).toBe('dark');
      expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
      expect(document.documentElement.classList.contains('dark-theme')).toBe(true);
      expect(document.documentElement.classList.contains('dark')).toBe(true);
      expect(document.body.getAttribute('data-theme')).toBe('dark');
      expect(document.body.classList.contains('dark-theme')).toBe(true);
    });

    it('should immediately switch from dark to light mode on toggleTheme()', () => {
      service.setTheme('dark');
      expect(service.theme()).toBe('dark');

      service.toggleTheme();

      expect(service.theme()).toBe('light');
      expect(localStorage.getItem('taxipay_theme')).toBe('light');

      expect(document.documentElement.getAttribute('data-theme')).toBe('light');
      expect(document.documentElement.classList.contains('light-theme')).toBe(true);
      expect(document.documentElement.classList.contains('dark-theme')).toBe(false);

      expect(document.body.getAttribute('data-theme')).toBe('light');
      expect(document.body.classList.contains('light-theme')).toBe(true);
      expect(document.body.classList.contains('dark-theme')).toBe(false);
    });

    it('should switch back to dark mode instantly and maintain synchronization', () => {
      service.setTheme('light');
      expect(service.theme()).toBe('light');

      service.toggleTheme();

      expect(service.theme()).toBe('dark');
      expect(localStorage.getItem('taxipay_theme')).toBe('dark');

      expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
      expect(document.documentElement.classList.contains('dark-theme')).toBe(true);
      expect(document.documentElement.classList.contains('light-theme')).toBe(false);

      expect(document.body.getAttribute('data-theme')).toBe('dark');
      expect(document.body.classList.contains('dark-theme')).toBe(true);
      expect(document.body.classList.contains('light-theme')).toBe(false);
    });

    it('should handle rapid consecutive toggling without desynchronization', () => {
      service.setTheme('dark');

      service.toggleTheme(); // -> light
      expect(service.theme()).toBe('light');
      expect(document.body.classList.contains('light-theme')).toBe(true);

      service.toggleTheme(); // -> dark
      expect(service.theme()).toBe('dark');
      expect(document.body.classList.contains('dark-theme')).toBe(true);

      service.toggleTheme(); // -> light
      expect(service.theme()).toBe('light');
      expect(document.body.classList.contains('light-theme')).toBe(true);

      service.toggleTheme(); // -> dark
      expect(service.theme()).toBe('dark');
      expect(document.body.classList.contains('dark-theme')).toBe(true);
      expect(document.body.classList.contains('light-theme')).toBe(false);
    });
  });
});

