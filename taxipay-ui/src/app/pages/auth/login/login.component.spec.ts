import { TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  LoginComponent,
  emailOrPhoneValidator,
  strongPasswordValidator
} from './login.component';

describe('LoginComponent & Validation', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: MatSnackBar,
          useValue: { open: vi.fn() }
        }
      ]
    }).compileComponents();
  });

  describe('emailOrPhoneValidator', () => {
    const validator = emailOrPhoneValidator();

    it('should validate proper email format', () => {
      expect(validator(new FormControl('passenger@taxipay.et'))).toBeNull();
      expect(validator(new FormControl('driver.bole@gmail.com'))).toBeNull();
    });

    it('should reject malformed email format', () => {
      expect(validator(new FormControl('invalid-email@'))).toEqual({ invalidEmail: true });
      expect(validator(new FormControl('test@domain'))).toEqual({ invalidEmail: true });
    });

    it('should validate Ethiopian 10-digit phone format', () => {
      expect(validator(new FormControl('0911223344'))).toBeNull();
      expect(validator(new FormControl('0711223344'))).toBeNull();
    });

    it('should reject invalid phone format', () => {
      expect(validator(new FormControl('0811223344'))).toEqual({ invalidPhone: true });
      expect(validator(new FormControl('123456'))).toEqual({ invalidPhone: true });
    });
  });

  describe('strongPasswordValidator', () => {
    const validator = strongPasswordValidator();

    it('should accept passwords with min 8 chars, uppercase, lowercase, and number', () => {
      expect(validator(new FormControl('TaxiPay2026'))).toBeNull();
      expect(validator(new FormControl('Pass12345'))).toBeNull();
    });

    it('should reject passwords shorter than 8 characters', () => {
      const res = validator(new FormControl('Pass1'));
      expect(res).not.toBeNull();
      expect(res?.['weakPassword']?.hasMinLength).toBe(false);
    });

    it('should reject passwords missing uppercase', () => {
      const res = validator(new FormControl('password123'));
      expect(res).not.toBeNull();
      expect(res?.['weakPassword']?.hasUpperCase).toBe(false);
    });

    it('should reject passwords missing numbers', () => {
      const res = validator(new FormControl('PasswordOnly'));
      expect(res).not.toBeNull();
      expect(res?.['weakPassword']?.hasNumber).toBe(false);
    });
  });

  describe('Sign-in button click behavior', () => {
    it('should mark form as touched when sign-in is clicked with empty inputs', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      expect(component.passengerForm.valid).toBe(false);
      component.loginPassenger();

      expect(component.passengerForm.touched).toBe(true);
      expect(component.pControls['identifier'].touched).toBe(true);
      expect(component.pControls['password'].touched).toBe(true);
    });
  });

  describe('Login Page UI Elements & Dark Mode Visibility', () => {
    it('should render Welcome back, Sign in to your account, and role headings', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;

      expect(compiled.querySelector('.form-eyebrow')?.textContent).toContain('Welcome back');
      expect(compiled.querySelector('.form-title')?.textContent).toContain('Passenger Sign In');
      expect(compiled.querySelector('.form-desc')?.textContent).toContain('Sign in to your account');
    });

    it('should render Passenger and Driver login selector buttons with proper states', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;

      const passengerTab = compiled.querySelector('#tab-passenger');
      const driverTab = compiled.querySelector('#tab-driver');

      expect(passengerTab?.textContent).toContain('Passenger Login');
      expect(driverTab?.textContent).toContain('Driver Login');
      expect(passengerTab?.classList.contains('active')).toBe(true);
      expect(driverTab?.classList.contains('active')).toBe(false);

      // Switch to driver
      component.setRole('driver');
      fixture.detectChanges();

      expect(passengerTab?.classList.contains('active')).toBe(false);
      expect(driverTab?.classList.contains('active')).toBe(true);
      expect(compiled.querySelector('.form-title')?.textContent).toContain('Driver Sign In');
    });

    it('should render form fields with readable labels, placeholders, and visibility toggles', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;

      const emailLabel = compiled.querySelector('label[for="p-id"]');
      const emailInput = compiled.querySelector('#p-id') as HTMLInputElement;
      const pwLabel = compiled.querySelector('label[for="p-pw"]');
      const pwInput = compiled.querySelector('#p-pw') as HTMLInputElement;

      expect(emailLabel?.textContent).toContain('Email or Phone number');
      expect(emailInput?.placeholder).toContain('name@email.com');
      expect(pwLabel?.textContent).toContain('Password');
      expect(pwInput?.placeholder).toContain('Enter your password');

      // Check remember me & forgot password
      expect(compiled.querySelector('.checkbox-label')?.textContent).toContain('Remember me');
      expect(compiled.querySelector('.forgot-btn')?.textContent).toContain('Forgot password?');

      // Check submit button text
      expect(compiled.querySelector('.submit-btn')?.textContent).toContain('Sign In as Passenger');

      // Check footer links
      expect(compiled.querySelector('.form-footer')?.textContent).toContain("Don't have an account?");
      expect(compiled.querySelector('.form-footer a')?.textContent).toContain('Sign up');
    });

    it('should render left-side hero text, transit badge, and all feature chips', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;

      expect(compiled.querySelector('.hero-live-badge')?.textContent).toContain('ETHIOPIAN SMART TRANSIT');
      expect(compiled.querySelector('.hero-headline')?.textContent).toContain('Ethiopia');
      expect(compiled.querySelector('.hero-sub')?.textContent).toContain('Smart rides. Safe journeys.');

      const chips = compiled.querySelectorAll('.hero-chip');
      expect(chips.length).toBe(3);
      expect(chips[0].textContent).toContain('Safe & Verified');
      expect(chips[1].textContent).toContain('Tap & Pay');
      expect(chips[2].textContent).toContain('All Corridors');
    });

    it('should toggle theme when theme button is clicked', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;

      const themeBtn = compiled.querySelector('.theme-btn') as HTMLButtonElement;
      expect(themeBtn).toBeTruthy();

      const initialTheme = component.auth.theme();
      themeBtn.click();
      fixture.detectChanges();

      expect(component.auth.theme()).not.toBe(initialTheme);
    });

    it('should preserve entered input values, role, and validation state across rapid theme toggling', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;

      // Set input values and validation
      component.setRole('passenger');
      component.passengerForm.patchValue({
        identifier: 'passenger@example.com',
        password: 'Password123!',
        rememberMe: true
      });
      fixture.detectChanges();

      const themeBtn = compiled.querySelector('.theme-btn') as HTMLButtonElement;

      component.auth.setTheme('dark');
      fixture.detectChanges();

      // Rapidly toggle theme 4 times: dark -> light -> dark -> light -> dark
      themeBtn.click(); // 1
      fixture.detectChanges();
      expect(component.auth.theme()).toBe('light');
      expect(document.documentElement.getAttribute('data-theme')).toBe('light');

      themeBtn.click(); // 2
      fixture.detectChanges();
      expect(component.auth.theme()).toBe('dark');
      expect(document.documentElement.getAttribute('data-theme')).toBe('dark');

      themeBtn.click(); // 3
      fixture.detectChanges();
      expect(component.auth.theme()).toBe('light');

      themeBtn.click(); // 4
      fixture.detectChanges();
      expect(component.auth.theme()).toBe('dark');

      // Verify form data and role state remained completely intact
      expect(component.selectedRole()).toBe('passenger');
      expect(component.passengerForm.value.identifier).toBe('passenger@example.com');
      expect(component.passengerForm.value.password).toBe('Password123!');
      expect(component.passengerForm.value.rememberMe).toBe(true);
      expect(component.passengerForm.valid).toBe(true);

      // Verify Driver role also preserved across theme toggles
      component.setRole('driver');
      component.driverForm.patchValue({
        identifier: '0911223344',
        password: 'DriverPassword1!',
        rememberMe: false
      });
      fixture.detectChanges();

      themeBtn.click(); // -> light
      fixture.detectChanges();
      expect(component.selectedRole()).toBe('driver');
      expect(component.driverForm.value.identifier).toBe('0911223344');
      expect(component.driverForm.value.password).toBe('DriverPassword1!');

      themeBtn.click(); // -> dark
      fixture.detectChanges();
      expect(component.selectedRole()).toBe('driver');
      expect(component.driverForm.value.identifier).toBe('0911223344');
    });
  });
});
