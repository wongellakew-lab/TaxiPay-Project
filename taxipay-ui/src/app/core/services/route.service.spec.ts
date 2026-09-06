import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RouteService } from './route.service';
import { environment } from '../../../environments/environment';

describe('RouteService', () => {
  let service: RouteService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        RouteService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(RouteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch active routes from backend and populate signal', () => {
    service.getRoutes().subscribe(routes => {
      expect(routes.length).toBe(1);
      expect(routes[0].routeCode).toBe('BM-001');
      expect(service.routes().length).toBe(1);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/routes`);
    expect(req.request.method).toBe('GET');
    req.flush([
      {
        id: 'r-01',
        name: 'Bole ↔ Merkato',
        routeCode: 'BM-001',
        nodes: [{ id: 'A', name: 'Bole', latitude: 9.0, longitude: 38.7 }],
        isActive: true,
        activeDriversCount: 5,
        createdAt: new Date().toISOString()
      }
    ]);
  });

  it('should calculate fare between nodes via backend API', () => {
    service.calculateFare('A', 'F', 'r-01').subscribe(fare => {
      expect(fare).toBe(25);
    });

    const req = httpMock.expectOne(req => req.url.includes('/passengers/fares'));
    expect(req.request.method).toBe('GET');
    req.flush({ fare: 25 });
  });
});
