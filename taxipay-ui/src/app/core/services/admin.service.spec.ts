import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminService } from './admin.service';
import { environment } from '../../../environments/environment';

describe('AdminService', () => {
  let service: AdminService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch drivers via HTTP from database API', () => {
    service.getDrivers().subscribe(drivers => {
      expect(drivers).toBeDefined();
      expect(drivers.length).toBe(1);
      expect(drivers[0].driverId).toBe('AB1234');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/admins/drivers`);
    expect(req.request.method).toBe('GET');
    req.flush([
      {
        id: 'd-01',
        driverId: 'AB1234',
        firstName: 'Abebe',
        lastName: 'Kebede',
        phone: '0911122334',
        licenseNumber: 'DL-001',
        status: 'Active',
        isVerified: true,
        rating: 4.8,
        totalTrips: 150,
        totalRevenue: 1500,
        currentRouteId: 'r-01',
        currentRouteCode: 'BM-001',
        currentRouteName: 'Bole ↔ Merkato',
        createdAt: '2026-08-15T19:21:22Z'
      }
    ]);
  });

  it('should fetch routes via HTTP from database API', () => {
    service.getRoutes().subscribe(routes => {
      expect(routes).toBeDefined();
      expect(routes.length).toBe(1);
      expect(routes[0].routeCode).toBe('BM-001');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/admins/routes`);
    expect(req.request.method).toBe('GET');
    req.flush([
      {
        id: 'r-01',
        name: 'Bole ↔ Merkato',
        routeCode: 'BM-001',
        nodes: [{ id: 'A', name: 'Bole Medhanealem', latitude: 9.0145, longitude: 38.7468 }],
        isActive: true,
        activeDriversCount: 5,
        createdAt: '2026-08-15T19:21:22Z'
      }
    ]);
  });

  it('should fetch analytics overview via HTTP', () => {
    service.getAnalyticsOverview().subscribe(overview => {
      expect(overview).toBeDefined();
      expect(overview.totalRevenue).toBe(148520);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/admins/analytics/overview`);
    expect(req.request.method).toBe('GET');
    req.flush({
      totalRevenue: 148520,
      todayRevenue: 8450,
      totalTrips: 4120,
      activeTrips: 18,
      totalDrivers: 48,
      activeDriversOnShift: 26,
      pendingDriverApprovals: 3,
      totalPassengers: 1250,
      openDisputes: 2,
      pendingRouteRequests: 1,
      dailyRevenueHistory: [],
      recentActivities: []
    });
  });
});
