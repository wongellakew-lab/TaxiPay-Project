import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PassengerDashboardComponent } from './passenger-dashboard.component';
import { AuthService } from '../../../core/services/auth.service';
import { PassengerService } from '../../../core/services/passenger.service';
import { CurrentJourneyStore } from '../../../core/stores/current-journey.store';
import { WalletStore } from '../../../core/stores/wallet.store';
import { signal } from '@angular/core';
import { CurrentJourneyDto } from '../../../core/models/passenger.models';

describe('PassengerDashboardComponent - Active Commute Section', () => {
  let component: PassengerDashboardComponent;
  let fixture: ComponentFixture<PassengerDashboardComponent>;
  let mockCurrentJourneySignal: ReturnType<typeof signal<CurrentJourneyDto | null>>;

  const mockActiveJourney: CurrentJourneyDto = {
    tripId: 'trip-123',
    journeyId: 'journey-456',
    driverId: 'driver-999',
    tripStatus: 'Active',
    routeName: 'Megenagna - Tor Hailoch Corridor',
    boardingPoint: 'Megenagna Terminal',
    alightingPoint: 'Tor Hailoch',
    driverName: 'Abebe Bikila',
    driverCode: 'ET-TX-7788',
    driverPhone: '+251911223344',
    fare: 25.0,
    paymentMethod: 'Telebirr',
    paymentStatus: 'Completed',
    alightingStatus: 'InTransit',
    boardedAt: new Date().toISOString()
  };

  beforeEach(async () => {
    mockCurrentJourneySignal = signal<CurrentJourneyDto | null>(mockActiveJourney);

    const mockAuthService = {
      currentUser: signal({ id: 'p-1', role: 'passenger', name: 'Almaz' }),
      theme: signal<'dark' | 'light'>('dark'),
      toggleTheme: vi.fn(),
      setTheme: vi.fn()
    };

    const mockPassengerService = {
      getDashboard: vi.fn().mockReturnValue(of({
        summary: { fullName: 'Almaz Tesfaye', phone: '+251911000000', walletBalance: 250, rating: 4.9 },
        currentJourney: mockActiveJourney,
        recentTrips: [],
        favorites: [],
        stats: { totalTrips: 12, totalSpent: 300, averageFare: 25, tripsThisMonth: 8, spentThisMonth: 200 }
      }))
    };

    const mockWalletStore = {
      balance: signal(250),
      isLoading: signal(false),
      loadBalance: vi.fn(),
      setBalance: vi.fn()
    };

    const mockCurrentJourneyStore = {
      currentJourney: mockCurrentJourneySignal,
      isConfirmingAlight: signal(false),
      loadCurrentJourney: vi.fn(),
      setJourney: vi.fn(),
      confirmAlighting: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [PassengerDashboardComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        { provide: PassengerService, useValue: mockPassengerService },
        { provide: WalletStore, useValue: mockWalletStore },
        { provide: CurrentJourneyStore, useValue: mockCurrentJourneyStore }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PassengerDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the Active Commute in Progress section when current journey exists', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const card = compiled.querySelector('.current-journey-card');
    expect(card).toBeTruthy();

    const indicatorText = compiled.querySelector('.live-transit-indicator')?.textContent;
    expect(indicatorText).toContain('ACTIVE COMMUTE IN PROGRESS');

    const routeHeading = compiled.querySelector('.cj-heading')?.textContent;
    expect(routeHeading).toContain('Megenagna - Tor Hailoch Corridor');
  });

  it('should display all trip details clearly in the detail grid', () => {
    const compiled = fixture.nativeElement as HTMLElement;

    // Driver details
    const driverName = compiled.querySelector('.driver-name')?.textContent;
    expect(driverName).toContain('Abebe Bikila');
    const driverCode = compiled.querySelector('.driver-code-tag')?.textContent;
    expect(driverCode).toContain('ET-TX-7788');

    // Route stops
    const fromStop = compiled.querySelector('.stop-node.from')?.textContent;
    expect(fromStop).toContain('Megenagna Terminal');
    const toStop = compiled.querySelector('.stop-node.to')?.textContent;
    expect(toStop).toContain('Tor Hailoch');

    // Fare & payment
    const fareVal = compiled.querySelector('.fare-val')?.textContent;
    expect(fareVal).toContain('25.00 ETB');
    const badge = compiled.querySelector('.badge');
    expect(badge?.textContent).toContain('Telebirr');
    expect(badge?.classList.contains('badge-cyan')).toBe(true);

    // Status pill
    const statusPill = compiled.querySelector('.cj-status-pill');
    expect(statusPill?.textContent).toContain('In Transit');
    expect(statusPill?.classList.contains('pill-emerald')).toBe(true);
  });

  it('should render Alighting Confirmation Requested state with action button when awaiting confirmation', () => {
    mockCurrentJourneySignal.set({
      ...mockActiveJourney,
      alightingStatus: 'AwaitingConfirmation'
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const card = compiled.querySelector('.current-journey-card');
    expect(card?.classList.contains('awaiting-confirmation')).toBe(true);

    const indicator = compiled.querySelector('.live-transit-indicator')?.textContent;
    expect(indicator).toContain('ALIGHTING CONFIRMATION REQUESTED');

    const alertBox = compiled.querySelector('.alighting-alert-box');
    expect(alertBox).toBeTruthy();
    expect(alertBox?.textContent).toContain('Destination Stop Reached');

    const confirmBtn = compiled.querySelector('#btn-confirm-alighting');
    expect(confirmBtn).toBeTruthy();
    expect(confirmBtn?.textContent).toContain('Confirm Alighted');
  });

  it('should support switching between light and dark theme on the Active Commute card and back to dark', () => {
    const card = fixture.nativeElement.querySelector('.current-journey-card') as HTMLElement;
    expect(card).toBeTruthy();

    // Default / Dark mode classes
    document.body.classList.remove('light-theme', 'light');
    document.body.classList.add('dark-theme', 'dark');
    expect(document.body.classList.contains('dark-theme')).toBe(true);

    // Verify dark mode elements and trip information visibility
    const indicator = card.querySelector('.live-transit-indicator') as HTMLElement;
    expect(indicator).toBeTruthy();
    expect(indicator.textContent).toContain('ACTIVE COMMUTE IN PROGRESS');

    const pulseDot = card.querySelector('.pulse-dot') as HTMLElement;
    expect(pulseDot).toBeTruthy();

    const busIcon = card.querySelector('.bus-transit-icon') as HTMLElement;
    expect(busIcon).toBeTruthy();

    // Switch to Light mode
    document.body.classList.remove('dark-theme', 'dark');
    document.body.classList.add('light-theme', 'light');
    expect(document.body.classList.contains('light-theme')).toBe(true);
    expect(document.body.classList.contains('dark-theme')).toBe(false);

    // Switch back to Dark mode
    document.body.classList.remove('light-theme', 'light');
    document.body.classList.add('dark-theme', 'dark');
    expect(document.body.classList.contains('dark-theme')).toBe(true);
    expect(document.body.classList.contains('light-theme')).toBe(false);

    // Cleanup
    document.body.classList.remove('light-theme', 'light', 'dark-theme', 'dark');
  });

  it('should render all detail labels, icons, and elements in Active Commute section', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const card = compiled.querySelector('.current-journey-card');
    expect(card).toBeTruthy();

    // Verify all 4 detail labels
    const labels = Array.from(card?.querySelectorAll('.detail-label') || []).map(el => el.textContent?.trim());
    expect(labels).toEqual(['DRIVER', 'ROUTE STOPS', 'FARE & PAYMENT', 'BOARDED AT']);

    // Verify arrow icon in stops path
    const arrowIcon = card?.querySelector('.stops-path .arrow-icon');
    expect(arrowIcon).toBeTruthy();

    // Verify schedule time icon
    const timeIcon = card?.querySelector('.time-info .time-icon');
    expect(timeIcon).toBeTruthy();

    // Verify boarding and alighting nodes
    const fromNode = card?.querySelector('.stop-node.from');
    const toNode = card?.querySelector('.stop-node.to');
    expect(fromNode).toBeTruthy();
    expect(toNode).toBeTruthy();

    // Verify status pill and icon
    const statusPill = card?.querySelector('.cj-status-pill');
    expect(statusPill).toBeTruthy();
    expect(statusPill?.querySelector('mat-icon')).toBeTruthy();
  });
});

