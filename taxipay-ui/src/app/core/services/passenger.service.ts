import { Injectable, inject, signal, effect } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActivePassengerDto } from '../models/driver.models';
import {
  CurrentJourneyDto,
  DriverByCodeResponse,
  DriverSearchResultDto,
  FareResponse,
  PassengerDashboardResponse,
  PassengerProfileResponse,
  PassengerResponse,
  PayTripRequest,
  PayTripResponse,
  TopUpWalletRequest,
  TopUpWalletResponse,
  TripHistoryResponse,
  UpdatePassengerProfileRequest,
  PassengerRouteRequestDto,
  CreatePassengerRouteRequestDto
} from '../models/passenger.models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class PassengerService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  // State Management Signals - Single Source of Truth
  profile = signal<PassengerResponse | null>(null);
  dashboard = signal<PassengerDashboardResponse | null>(null);
  walletBalance = signal<number>(0);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  constructor() {
    // Reactively synchronize the authoritative database wallet balance whenever
    // the current user session changes (login, switch account, page reload)
    // and clear the wallet when logged out.
    effect(() => {
      const user = this.auth.currentUser();
      if (user && user.role === 'passenger' && user.id) {
        this.refreshWallet(user.id);
      } else if (!user) {
        this.walletBalance.set(0);
        this.profile.set(null);
        this.dashboard.set(null);
      }
    });
  }

  /**
   * Fetches the authoritative passenger profile from the database and updates the shared wallet balance.
   */
  refreshWallet(passengerId?: string): void {
    const pid = passengerId || this.auth.currentUser()?.id;
    if (!pid) return;
    this.getProfile(pid).subscribe();
  }

  getProfile(passengerId?: string): Observable<PassengerProfileResponse> {
    const pid = passengerId || this.auth.currentUser()?.id;
    this.isLoading.set(true);
    return this.http.get<PassengerProfileResponse>(`${environment.apiUrl}/passengers/${pid}/profile`).pipe(
      tap({
        next: p => {
          this.profile.set(p);
          if (p.walletBalance !== undefined && p.walletBalance !== null) {
            this.walletBalance.set(Number(p.walletBalance));
          }
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  updateProfile(passengerId: string, req: UpdatePassengerProfileRequest): Observable<PassengerProfileResponse> {
    this.isLoading.set(true);
    return this.http.put<PassengerProfileResponse>(`${environment.apiUrl}/passengers/${passengerId}/profile`, req).pipe(
      tap({
        next: p => {
          this.profile.set(p);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  topUpWallet(passengerId: string, req: TopUpWalletRequest): Observable<TopUpWalletResponse> {
    this.isLoading.set(true);
    return this.http.post<TopUpWalletResponse>(`${environment.apiUrl}/passengers/${passengerId}/topup`, req).pipe(
      tap({
        next: res => {
          this.walletBalance.set(res.newBalance);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  getDashboard(passengerId?: string): Observable<PassengerDashboardResponse> {
    const pid = passengerId || this.auth.currentUser()?.id;
    this.isLoading.set(true);
    return this.http.get<PassengerDashboardResponse>(`${environment.apiUrl}/passengers/${pid}/dashboard`).pipe(
      tap({
        next: dash => {
          this.dashboard.set(dash);
          if (dash.summary?.walletBalance !== undefined) {
            this.walletBalance.set(dash.summary.walletBalance);
          }
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  getTripHistory(passengerId?: string, pageNumber = 1, pageSize = 10): Observable<TripHistoryResponse> {
    const pid = passengerId || this.auth.currentUser()?.id;
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    this.isLoading.set(true);
    return this.http.get<TripHistoryResponse>(`${environment.apiUrl}/passengers/${pid}/trips`, { params }).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  getDriverByCode(driverCode: string): Observable<DriverByCodeResponse & { currentDirection?: string; directionKey?: 'FORWARD' | 'REVERSE' }> {
    return this.http.get<DriverByCodeResponse>(`${environment.apiUrl}/passengers/drivers/${driverCode}`);
  }

  searchDrivers(query: string, limit = 10): Observable<DriverSearchResultDto[]> {
    const params = new HttpParams()
      .set('query', query)
      .set('limit', limit.toString());

    return this.http.get<DriverSearchResultDto[]>(`${environment.apiUrl}/passengers/drivers/search`, { params });
  }

  getFare(routeId: string, fromNode: string, toNode: string): Observable<FareResponse> {
    const params = new HttpParams()
      .set('routeId', routeId)
      .set('fromNode', fromNode)
      .set('toNode', toNode);

    return this.http.get<FareResponse>(`${environment.apiUrl}/passengers/fares`, { params });
  }

  payForTrip(passengerId: string, req: PayTripRequest): Observable<PayTripResponse> {
    this.isLoading.set(true);
    return this.http.post<PayTripResponse>(`${environment.apiUrl}/passengers/${passengerId}/pay`, req).pipe(
      tap({
        next: res => {
          this.isLoading.set(false);
          if (res.newBalance !== undefined) {
            this.walletBalance.set(res.newBalance);
          } else {
            // Refetch profile to keep wallet in sync with database
            this.getProfile(passengerId).subscribe();
          }
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  getRouteRequests(passengerId?: string): Observable<PassengerRouteRequestDto[]> {
    const pid = passengerId || this.auth.currentUser()?.id;
    return this.http.get<PassengerRouteRequestDto[]>(`${environment.apiUrl}/passengers/${pid}/route-requests`);
  }

  submitRouteRequest(passengerId: string, req: CreatePassengerRouteRequestDto): Observable<PassengerRouteRequestDto> {
    return this.http.post<PassengerRouteRequestDto>(`${environment.apiUrl}/passengers/${passengerId}/route-requests`, req);
  }

  confirmAlighting(passengerId: string, journeyId: string): Observable<ActivePassengerDto> {
    this.isLoading.set(true);
    return this.http.post<ActivePassengerDto>(`${environment.apiUrl}/passengers/${passengerId}/journeys/${journeyId}/confirm-alight`, {}).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }
}
