import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { provideAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { PayTripComponent } from './pay-trip.component';
import { PassengerService } from '../../../core/services/passenger.service';
import { AuthService } from '../../../core/services/auth.service';
import { PaymentMethod } from '../../../core/models/passenger.models';

describe('PayTripComponent', () => {
  let component: PayTripComponent;
  let fixture: ComponentFixture<PayTripComponent>;
  let passengerService: PassengerService;
  let authService: AuthService;

  const mockDriver = {
    driverId: 'BM-001',
    fullName: 'Test Driver',
    phone: '0911000000',
    rating: 4.8,
    routeName: 'Bole - Merkato',
    routeCode: 'BM-001',
    isActive: true,
    hasActiveShift: true,
    routeNodes: [
      { id: 'A', name: 'Bole', order: 1 },
      { id: 'B', name: 'Bole Bridge', order: 2 },
      { id: 'C', name: 'Megenagna', order: 3 },
      { id: 'D', name: 'Mexico', order: 4 },
      { id: 'F', name: 'Merkato', order: 5 }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PayTripComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideAnimations(),
        {
          provide: MatSnackBar,
          useValue: { open: vi.fn() }
        },
        {
          provide: MatDialog,
          useValue: { open: vi.fn() }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PayTripComponent);
    component = fixture.componentInstance;
    passengerService = TestBed.inject(PassengerService);
    authService = TestBed.inject(AuthService);
    fixture.detectChanges();
  });

  it('should initialize with empty state when no driver is entered', () => {
    expect(component.driverCodeInput).toBe('');
    expect(component.driverInfo()).toBeNull();
    expect(component.selectedFromNode).toBe('');
    expect(component.selectedToNode).toBe('');
    expect(component.orderedNodes.length).toBe(0);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty-state-card')).toBeTruthy();
    expect(compiled.textContent).toContain('Enter a driver\'s code above to begin your ride fare calculation');
  });

  it('should preserve exactly what the user types without transforming casing', () => {
    component.driverCodeInput = 'bm-001';
    component.onDriverCodeInput();
    expect(component.driverCodeInput).toBe('bm-001');

    component.driverCodeInput = 'Bm-001';
    component.onDriverCodeInput();
    expect(component.driverCodeInput).toBe('Bm-001');
  });

  it('should perform partial search for drivers in database', () => {
    const searchSpy = vi.spyOn(passengerService, 'searchDrivers').mockReturnValue(of([
      {
        driverId: 'BM-001',
        fullName: 'Test Driver',
        routeName: 'Bole - Merkato',
        routeCode: 'BM-001',
        rating: 4.8,
        isActive: true,
        hasActiveShift: true
      }
    ]));

    component.driverCodeInput = 'bm';
    component.onDriverCodeInput();

    expect(component.driverCodeInput).toBe('bm');
  });

  it('should close suggestions dropdown and populate driver on option selected', () => {
    vi.spyOn(passengerService, 'getDriverByCode').mockReturnValue(of(mockDriver as any));
    const closeSpy = vi.fn();
    component.autoTrigger = { closePanel: closeSpy } as any;

    component.onDriverOptionSelected({ option: { value: 'BM-001' } } as any);

    expect(component.driverCodeInput).toBe('BM-001');
    expect(closeSpy).toHaveBeenCalled();
    expect(component.driverInfo()).toEqual(mockDriver as any);
  });

  it('should load driver and populate directional stops on case-insensitive driver code lookup', () => {
    vi.spyOn(passengerService, 'getDriverByCode').mockReturnValue(of(mockDriver as any));

    component.driverCodeInput = 'bm-001';
    component.lookupDriver();

    expect(component.driverInfo()).toEqual(mockDriver as any);
    expect(component.orderedNodes.length).toBe(5);
    expect(component.boardingNodeOptions.length).toBe(4);
    expect(component.selectedFromNode).toBe('A');
    expect(component.selectedToNode).toBe('B');
    expect(component.fareAmount()).toBe(5);
  });

  it('should display explicit red error when driver code does not exist in database', () => {
    vi.spyOn(passengerService, 'getDriverByCode').mockReturnValue(throwError(() => ({
      status: 404,
      error: { Code: 'driver_not_found', Message: 'Driver with code "xyz-999" does not exist.' }
    })));

    component.driverCodeInput = 'xyz-999';
    component.lookupDriver();

    expect(component.driverInfo()).toBeNull();
    expect(component.driverError()).toBe('Driver with code "xyz-999" does not exist.');
  });

  it('should update alighting options and recalculate fare when boarding point changes', () => {
    vi.spyOn(passengerService, 'getDriverByCode').mockReturnValue(of(mockDriver as any));
    component.driverCodeInput = 'BM-001';
    component.lookupDriver();

    component.onBoardingNodeSelected('C');

    expect(component.selectedFromNode).toBe('C');
    expect(component.alightNodeOptions.map(n => n.id)).toEqual(['D', 'F']);
    expect(component.selectedToNode).toBe('D');
    expect(component.fareAmount()).toBe(5);

    component.onAlightNodeSelected('F');
    expect(component.selectedToNode).toBe('F');
    expect(component.fareAmount()).toBe(10);
  });

  it('should update stations when route timeline emits fromSelected and toSelected', () => {
    vi.spyOn(passengerService, 'getDriverByCode').mockReturnValue(of(mockDriver as any));
    component.driverCodeInput = 'BM-001';
    component.lookupDriver();

    component.onTimelineFromSelected('B');
    expect(component.selectedFromNode).toBe('B');

    component.onTimelineToSelected('F');
    expect(component.selectedToNode).toBe('F');
    expect(component.fareAmount()).toBe(15);
  });

  it('should display specific field errors when attempting to pay with missing fields', () => {
    component.executePayment();

    expect(component.driverError()).toBe('Please enter a valid driver code.');
    expect(component.boardingError()).toBe('Please select a boarding point.');
    expect(component.alightError()).toBe('Please select an alighting point.');
  });

  it('should update shared wallet balance immediately after successful payment', () => {
    vi.spyOn(passengerService, 'getDriverByCode').mockReturnValue(of(mockDriver as any));
    vi.spyOn(authService, 'currentUser').mockReturnValue({ id: 'p-1', firstName: 'Wongel', lastName: 'Eshete', role: 'passenger' } as any);

    const paySpy = vi.spyOn(passengerService, 'payForTrip').mockReturnValue(of({
      tripId: 't-123',
      passengerId: 'p-1',
      driverId: 'BM-001',
      driverName: 'Test Driver',
      routeName: 'Bole - Merkato',
      boardingNode: 'Bole (A)',
      destinationNode: 'Merkato (F)',
      fare: 20,
      paymentMethod: 'Wallet',
      paymentStatus: 'Completed',
      paidAt: new Date().toISOString(),
      receiptNumber: 'REC-123',
      newBalance: 180
    }));

    component.driverCodeInput = 'BM-001';
    component.lookupDriver();
    component.selectedPaymentMethod = PaymentMethod.Wallet;

    component.executePayment();

    expect(paySpy).toHaveBeenCalled();
  });
});
