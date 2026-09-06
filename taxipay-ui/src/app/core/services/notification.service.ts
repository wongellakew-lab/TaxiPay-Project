import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AppNotificationDto, UnreadCountResponse } from '../models/notification.models';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/notifications`;

  getNotifications(): Observable<AppNotificationDto[]> {
    return this.http.get<AppNotificationDto[]>(this.baseUrl);
  }

  getUnreadCount(): Observable<UnreadCountResponse> {
    return this.http.get<UnreadCountResponse>(`${this.baseUrl}/unread-count`);
  }

  markAsRead(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/read`, {});
  }

  markAllAsRead(): Observable<{ markedCount: number }> {
    return this.http.post<{ markedCount: number }>(`${this.baseUrl}/read-all`, {});
  }
}
