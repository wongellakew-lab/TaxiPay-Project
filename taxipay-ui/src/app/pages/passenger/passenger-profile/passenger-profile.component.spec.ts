import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { PassengerProfileComponent } from './passenger-profile.component';
import { PassengerService } from '../../../core/services/passenger.service';
import { AuthService } from '../../../core/services/auth.service';

describe('PassengerProfileComponent', () => {
  let component: PassengerProfileComponent;
  let fixture: ComponentFixture<PassengerProfileComponent>;
  let passengerService: PassengerService;
  let authService: AuthService;

  const mockProfile = {
    id: 'p-1',
    firstName: 'Wongel',
    lastName: 'Eshete',
    fullName: 'Wongel Eshete',
    phone: '0911223344',
    walletBalance: 250,
    totalTrips: 14,
    totalSpent: 320,
    rating: 0,
    isVerified: true,
    createdAt: '2026-01-01T00:00:00Z'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PassengerProfileComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: MatSnackBar,
          useValue: { open: vi.fn() }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PassengerProfileComponent);
    component = fixture.componentInstance;
    passengerService = TestBed.inject(PassengerService);
    authService = TestBed.inject(AuthService);

    vi.spyOn(authService, 'currentUser').mockReturnValue({
      id: 'p-1',
      firstName: 'Wongel',
      lastName: 'Eshete',
      role: 'passenger',
      phone: '0911223344'
    } as any);

    vi.spyOn(passengerService, 'getProfile').mockReturnValue(of(mockProfile));
    fixture.detectChanges();
  });

  it('should create and load profile on init', () => {
    expect(component).toBeTruthy();
    expect(component.profile()).toEqual(mockProfile);
    expect(component.isLoading()).toBe(false);
  });

  it('should render rating as 0.0 when database rating is 0 (no fallback to 5)', () => {
    expect(component.profile()?.rating).toBe(0);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('0.0');
  });

  it('should toggle edit mode when startEditing is called', () => {
    component.startEditing();
    expect(component.isEditing()).toBe(true);
    expect(component.editFirstName).toBe('Wongel');
    expect(component.editLastName).toBe('Eshete');
    expect(component.editPhone).toBe('0911223344');
  });

  it('should cancel edit mode when cancelEditing is called', () => {
    component.startEditing();
    component.cancelEditing();
    expect(component.isEditing()).toBe(false);
  });

  it('should update profile and save to database on saveProfile()', () => {
    const updatedProfile = { ...mockProfile, firstName: 'Wongel', lastName: 'Updated' };
    const updateSpy = vi.spyOn(passengerService, 'updateProfile').mockReturnValue(of(updatedProfile));

    component.startEditing();
    component.editLastName = 'Updated';
    component.saveProfile();

    expect(updateSpy).toHaveBeenCalledWith('p-1', {
      firstName: 'Wongel',
      lastName: 'Updated',
      phone: '0911223344'
    });
    expect(component.isEditing()).toBe(false);
    expect(component.profile()?.lastName).toBe('Updated');
  });

  it('should top up wallet and update balance on submitTopUp()', () => {
    const topUpSpy = vi.spyOn(passengerService, 'topUpWallet').mockReturnValue(of({
      passengerId: 'p-1',
      amount: 100,
      newBalance: 350,
      transactionRef: 'TOP-123',
      topUpTime: new Date().toISOString()
    }));

    component.topUpAmount = 100;
    component.submitTopUp();

    expect(topUpSpy).toHaveBeenCalledWith('p-1', {
      amount: 100,
      paymentMethod: 'Telebirr'
    });
  });
});
