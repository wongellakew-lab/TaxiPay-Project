export interface AppNotificationDto {
  id: string;
  userId?: string | null;
  role?: string | null;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error' | string;
  targetUrl?: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface UnreadCountResponse {
  unreadCount: number;
}
