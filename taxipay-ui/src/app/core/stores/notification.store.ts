import { computed, effect, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState, withHooks } from '@ngrx/signals';
import { AppNotificationDto } from '../models/notification.models';
import { NotificationService } from '../services/notification.service';
import { LiveSyncService } from '../services/live-sync.service';
import { AuthService } from '../services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { tap, catchError, of } from 'rxjs';

export interface NotificationState {
  notifications: AppNotificationDto[];
  isLoading: boolean;
  error: string | null;
}

const initialNotificationState: NotificationState = {
  notifications: [],
  isLoading: false,
  error: null
};

export const NotificationStore = signalStore(
  { providedIn: 'root' },
  withState(initialNotificationState),
  withComputed((store) => ({
    unreadNotifications: computed(() => store.notifications().filter(n => !n.isRead)),
    readNotifications: computed(() => store.notifications().filter(n => n.isRead)),
    unreadCount: computed(() => store.notifications().filter(n => !n.isRead).length),
    hasUnread: computed(() => store.notifications().some(n => !n.isRead)),
    totalCount: computed(() => store.notifications().length)
  })),
  withMethods((
    store,
    notificationService = inject(NotificationService),
    authService = inject(AuthService),
    snackBar = inject(MatSnackBar)
  ) => {
    const loadNotifications = (): void => {
      const user = authService.currentUser();
      if (!user || !user.token) {
        patchState(store, { notifications: [], isLoading: false, error: null });
        return;
      }

      patchState(store, { isLoading: true, error: null });

      notificationService.getNotifications().pipe(
        tap({
          next: list => {
            const notifications = list || [];
            patchState(store, {
              notifications,
              isLoading: false,
              error: null
            });
          },
          error: err => {
            patchState(store, { isLoading: false, error: err?.message || 'Failed to load notifications' });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const markAsRead = (id: string): void => {
      const current = store.notifications();
      const target = current.find(n => n.id === id);
      if (!target || target.isRead) return;

      // Optimistic update — computed unreadCount updates automatically
      const updated = current.map(n => n.id === id ? { ...n, isRead: true } : n);
      patchState(store, { notifications: updated });

      notificationService.markAsRead(id).pipe(
        tap({
          error: () => {
            // Roll back to previous state if backend/database operation fails
            patchState(store, { notifications: current });
            snackBar.open('Failed to mark notification as read on server.', 'Dismiss', { duration: 3000 });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const markAllAsRead = (): void => {
      const current = store.notifications();
      const hasUnreadItems = current.some(n => !n.isRead);
      if (!hasUnreadItems) return;

      // Optimistic update
      const updated = current.map(n => ({ ...n, isRead: true }));
      patchState(store, { notifications: updated });

      notificationService.markAllAsRead().pipe(
        tap({
          next: () => {
            snackBar.open('All notifications marked as read.', 'OK', { duration: 2500 });
          },
          error: () => {
            // Roll back if backend operation fails
            patchState(store, { notifications: current });
            snackBar.open('Failed to mark notifications as read on server.', 'Dismiss', { duration: 3000 });
          }
        }),
        catchError(() => of(null))
      ).subscribe();
    };

    const handleNotificationReceived = (notification: AppNotificationDto): void => {
      const current = store.notifications();
      const exists = current.some(n => n.id === notification.id);
      if (!exists) {
        // Prepend new unread notification to reactive store
        const newNotification: AppNotificationDto = {
          ...notification,
          isRead: false
        };
        patchState(store, {
          notifications: [newNotification, ...current]
        });

        // Show floating toast for high-priority live updates
        snackBar.open(`🔔 ${notification.title}: ${notification.message}`, 'View', {
          duration: 5000,
          panelClass: notification.type === 'success' ? 'snack-success' : (notification.type === 'warning' ? 'snack-warning' : (notification.type === 'error' ? 'snack-error' : 'snack-info'))
        });
      }
    };

    const clear = (): void => {
      patchState(store, { notifications: [], isLoading: false, error: null });
    };

    return {
      loadNotifications,
      markAsRead,
      markAllAsRead,
      handleNotificationReceived,
      clear
    };
  }),
  withHooks({
    onInit(store, liveSync = inject(LiveSyncService), authService = inject(AuthService)) {
      // Automatically load notifications on login/session restoration and clear on logout
      effect(() => {
        const user = authService.currentUser();
        if (user && user.token) {
          store.loadNotifications();
        } else {
          store.clear();
        }
      });

      // Real-time notification broadcast listener via SignalR
      liveSync.notificationCreated$.subscribe(notif => {
        store.handleNotificationReceived(notif);
      });

      // Refresh on SignalR reconnection to ensure consistency
      liveSync.reconnected$.subscribe(() => {
        const user = authService.currentUser();
        if (user && user.token) {
          store.loadNotifications();
        }
      });
    }
  })
);
