import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { AdminLoginComponent } from './admin-login.component';

describe('AdminLoginComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations()
      ]
    }).compileComponents();
  });

  it('should create and initialize login form with completely empty credentials', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.loginForm).toBeDefined();
    expect(component.loginForm.get('email')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
  });

  it('should invalidate empty email or password', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.loginForm.patchValue({ email: '', password: '' });
    expect(component.loginForm.valid).toBe(false);

    component.onSubmit();
    expect(component.loginForm.get('email')?.touched).toBe(true);
    expect(component.loginForm.get('password')?.touched).toBe(true);
  });

  it('should validate proper email format', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.loginForm.patchValue({ email: 'invalid-email', password: 'secret' });
    expect(component.loginForm.get('email')?.hasError('email')).toBe(true);

    component.loginForm.patchValue({ email: 'supervisor@taxipay.com', password: 'secret' });
    expect(component.loginForm.get('email')?.hasError('email')).toBe(false);
  });

  it('should not contain Back to Sign In link or link to /login', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.back-home-wrap')).toBeNull();
    expect(compiled.querySelector('a[routerLink="/login"]')).toBeNull();
    expect(compiled.textContent).not.toContain('Back to Sign In');
  });

  it('should render the theme toggle button and toggle theme immediately when clicked', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    const themeBtn = compiled.querySelector('.theme-btn') as HTMLButtonElement;
    expect(themeBtn).toBeTruthy();

    component.auth.setTheme('dark');
    fixture.detectChanges();
    expect(component.auth.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');

    themeBtn.click();
    fixture.detectChanges();

    expect(component.auth.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(compiled.querySelector('.theme-btn-text')?.textContent).toContain('Dark Mode');

    themeBtn.click();
    fixture.detectChanges();

    expect(component.auth.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(compiled.querySelector('.theme-btn-text')?.textContent).toContain('Light Mode');
  });

  it('should preserve entered credentials and form validation state across rapid theme toggles', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    component.loginForm.patchValue({
      email: 'supervisor@taxipay.com',
      password: 'AdminPassword123!'
    });
    fixture.detectChanges();

    const themeBtn = compiled.querySelector('.theme-btn') as HTMLButtonElement;
    component.auth.setTheme('dark');
    fixture.detectChanges();

    // Rapid toggle 4 times: dark -> light -> dark -> light -> dark
    themeBtn.click(); // 1 -> light
    fixture.detectChanges();
    expect(component.auth.theme()).toBe('light');

    themeBtn.click(); // 2 -> dark
    fixture.detectChanges();
    expect(component.auth.theme()).toBe('dark');

    themeBtn.click(); // 3 -> light
    fixture.detectChanges();
    expect(component.auth.theme()).toBe('light');

    themeBtn.click(); // 4 -> dark
    fixture.detectChanges();
    expect(component.auth.theme()).toBe('dark');

    // Verify form values and validity were completely preserved
    expect(component.loginForm.get('email')?.value).toBe('supervisor@taxipay.com');
    expect(component.loginForm.get('password')?.value).toBe('AdminPassword123!');
    expect(component.loginForm.valid).toBe(true);
  });
});
