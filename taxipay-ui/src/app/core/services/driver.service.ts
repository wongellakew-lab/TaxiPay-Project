import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActivePassengerDto,
  CompleteTripRequest,
  DriverDashboardResponse,
  DriverResponse,
  RequestRouteChangeRequest,
  RouteChangeResponse,
  ShiftSummaryResponse,
  TripResponse,
  UpdateProfileRequest
} from '../models/driver.models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class DriverService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  // State Management Signals
  profile = signal<DriverResponse | null>(null);
  dashboard = signal<DriverDashboardResponse | null>(null);
  activeTrip = signal<TripResponse | null>(null);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  getDashboard(driverId?: string): Observable<DriverDashboardResponse> {
    const did = driverId || this.auth.currentUser()?.id;
    this.isLoading.set(true);
    return this.http.get<DriverDashboardResponse>(`${environment.apiUrl}/drivers/${did}/dashboard`).pipe(
      tap({
        next: dash => {
          this.dashboard.set(dash);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  getProfile(driverId?: string): Observable<DriverResponse> {
    const did = driverId || this.auth.currentUser()?.id;
    this.isLoading.set(true);
    return this.http.get<DriverResponse>(`${environment.apiUrl}/drivers/${did}`).pipe(
      tap({
        next: p => {
          this.profile.set(p);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  updateProfile(driverId: string, req: UpdateProfileRequest): Observable<DriverResponse> {
    this.isLoading.set(true);
    return this.http.put<DriverResponse>(`${environment.apiUrl}/drivers/${driverId}`, req).pipe(
      tap({
        next: updated => {
          this.profile.set(updated);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  getShiftSummary(shiftId: string): Observable<ShiftSummaryResponse> {
    this.isLoading.set(true);
    return this.http.get<ShiftSummaryResponse>(`${environment.apiUrl}/drivers/shifts/${shiftId}/summary`).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  startTrip(shiftId: string, direction: string): Observable<TripResponse> {
    this.isLoading.set(true);
    return this.http.post<TripResponse>(
      `${environment.apiUrl}/drivers/shifts/${shiftId}/trips/start`,
      { direction }
    ).pipe(
      tap({
        next: trip => {
          this.activeTrip.set(trip);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  completeTrip(shiftId: string, req: CompleteTripRequest): Observable<TripResponse> {
    this.isLoading.set(true);
    return this.http.post<TripResponse>(`${environment.apiUrl}/drivers/shifts/${shiftId}/trips/complete`, req).pipe(
      tap({
        next: () => {
          this.activeTrip.set(null);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  requestRouteChange(driverId: string, req: RequestRouteChangeRequest): Observable<RouteChangeResponse> {
    this.isLoading.set(true);
    const targetRouteId = req.newRouteId || req.requestedRouteId || '';
    const payload = {
      newRouteId: targetRouteId,
      requestedRouteId: targetRouteId,
      reason: req.reason
    };
    return this.http.post<RouteChangeResponse>(`${environment.apiUrl}/drivers/${driverId}/route-change/request`, payload).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  getCurrentRouteChange(driverId: string): Observable<RouteChangeResponse | null> {
    return this.http.get<RouteChangeResponse | null>(`${environment.apiUrl}/drivers/${driverId}/route-change/current`);
  }

  requestPassengerAlighting(journeyId: string): Observable<ActivePassengerDto> {
    return this.http.post<ActivePassengerDto>(`${environment.apiUrl}/drivers/journeys/${journeyId}/request-alight`, {});
  }

  confirmPassengerAlighting(journeyId: string): Observable<ActivePassengerDto> {
    return this.http.post<ActivePassengerDto>(`${environment.apiUrl}/drivers/journeys/${journeyId}/confirm-alight`, {});
  }
}
