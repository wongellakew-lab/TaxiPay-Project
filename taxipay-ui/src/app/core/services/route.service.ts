import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, tap } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { FareMatrixItem, RouteDetail } from '../models/admin.models';

@Injectable({
  providedIn: 'root'
})
export class RouteService {
  private http = inject(HttpClient);

  // Reactive State Signals
  routes = signal<RouteDetail[]>([]);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  private hasFetched = false;

  getRoutes(forceRefresh = false): Observable<RouteDetail[]> {
    if (!forceRefresh && this.hasFetched && this.routes().length > 0) {
      return of(this.routes());
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    return this.http.get<RouteDetail[]>(`${environment.apiUrl}/routes`).pipe(
      tap({
        next: data => {
          this.hasFetched = true;
          this.routes.set(data);
          this.isLoading.set(false);
        },
        error: err => {
          this.isLoading.set(false);
          this.errorMessage.set('Failed to load routes from database.');
        }
      })
    );
  }

  getRouteById(routeId: string): Observable<RouteDetail> {
    const cached = this.routes().find(r => r.id === routeId);
    if (cached) return of(cached);

    return this.http.get<RouteDetail>(`${environment.apiUrl}/routes/${routeId}`);
  }

  getFaresForRoute(routeId: string): Observable<FareMatrixItem[]> {
    return this.http.get<FareMatrixItem[]>(`${environment.apiUrl}/routes/${routeId}/fares`);
  }

  calculateFare(fromNode: string, toNode: string, routeId?: string): Observable<number> {
    const params = new HttpParams()
      .set('routeId', routeId || '')
      .set('fromNode', fromNode)
      .set('toNode', toNode);

    return this.http.get<{ fare: number }>(`${environment.apiUrl}/passengers/fares`, { params }).pipe(
      map(res => res.fare)
    );
  }
}
