import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminAnalyticsOverview,
  AdminDispute,
  AdminDriverListItem,
  AdminProfile,
  AdminRouteChangeRequest,
  CreateFareMatrixRequest,
  CreateRouteRequest,
  FareMatrixItem,
  RouteDetail,
  UpdateFareMatrixRequest,
  UpdateRouteRequest
} from '../models/admin.models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admins`;

  // Driver Management
  getDrivers(status?: string): Observable<AdminDriverListItem[]> {
    let params = new HttpParams();
    if (status && status !== 'all') {
      params = params.set('status', status);
    }
    return this.http.get<AdminDriverListItem[]>(`${this.apiUrl}/drivers`, { params });
  }

  getDriverById(id: string): Observable<AdminDriverListItem> {
    return this.http.get<AdminDriverListItem>(`${this.apiUrl}/drivers/${id}`);
  }

  updateDriverStatus(id: string, status: string, note?: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/drivers/${id}/status`, { status, note });
  }

  approveDriver(id: string, assignedRouteId?: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/drivers/${id}/approve`, { assignedRouteId });
  }

  rejectDriver(id: string, rejectionReason: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/drivers/${id}/reject`, { rejectionReason });
  }

  assignDriverRoute(driverId: string, routeId: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/drivers/${driverId}/assign-route`, { routeId });
  }

  // Route Management
  getRoutes(): Observable<RouteDetail[]> {
    return this.http.get<RouteDetail[]>(`${this.apiUrl}/routes`);
  }

  createRoute(request: CreateRouteRequest): Observable<RouteDetail> {
    return this.http.post<RouteDetail>(`${this.apiUrl}/routes`, request);
  }

  updateRoute(id: string, request: UpdateRouteRequest): Observable<RouteDetail> {
    return this.http.put<RouteDetail>(`${this.apiUrl}/routes/${id}`, request);
  }

  toggleRouteStatus(id: string): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.apiUrl}/routes/${id}/status`, {});
  }

  deleteRoute(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/routes/${id}`);
  }

  // Fare Management
  getFareMatricesByRoute(routeId: string): Observable<FareMatrixItem[]> {
    return this.http.get<FareMatrixItem[]>(`${this.apiUrl}/fare-matrices/route/${routeId}`);
  }

  createFareMatrix(request: CreateFareMatrixRequest): Observable<FareMatrixItem> {
    return this.http.post<FareMatrixItem>(`${this.apiUrl}/fare-matrices`, request);
  }

  updateFareMatrix(id: string, request: UpdateFareMatrixRequest): Observable<FareMatrixItem> {
    return this.http.put<FareMatrixItem>(`${this.apiUrl}/fare-matrices/${id}`, request);
  }

  generateSymmetricFares(routeId: string, baseFarePerHop: number): Observable<FareMatrixItem[]> {
    return this.http.post<FareMatrixItem[]>(`${this.apiUrl}/fare-matrices/generate-symmetric`, { routeId, baseFarePerHop });
  }

  // Route Change Requests
  getRouteChangeRequests(status?: string): Observable<AdminRouteChangeRequest[]> {
    let params = new HttpParams();
    if (status && status !== 'all') {
      params = params.set('status', status);
    }
    return this.http.get<AdminRouteChangeRequest[]>(`${this.apiUrl}/route-change-requests`, { params });
  }

  reviewRouteChangeRequest(requestId: string, approved: boolean, note?: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/route-change-requests/${requestId}/review`, { approved, note });
  }

  // Disputes
  getDisputes(status?: string): Observable<AdminDispute[]> {
    let params = new HttpParams();
    if (status && status !== 'all') {
      params = params.set('status', status);
    }
    return this.http.get<AdminDispute[]>(`${this.apiUrl}/disputes`, { params });
  }

  openDispute(disputeId: string, notes?: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/disputes/${disputeId}/open`, { notes });
  }

  closeDispute(disputeId: string, resolutionNotes: string, issueRefund = false): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/disputes/${disputeId}/close`, { resolutionNotes, issueRefund });
  }

  resolveDispute(disputeId: string, status: string, resolutionNotes: string, issueRefund = false): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/disputes/${disputeId}/resolve`, { status, resolutionNotes, issueRefund });
  }

  // Analytics
  getAnalyticsOverview(): Observable<AdminAnalyticsOverview> {
    return this.http.get<AdminAnalyticsOverview>(`${this.apiUrl}/analytics/overview`);
  }

  // Profile
  getProfile(): Observable<AdminProfile> {
    return this.http.get<AdminProfile>(`${this.apiUrl}/me`);
  }
}
