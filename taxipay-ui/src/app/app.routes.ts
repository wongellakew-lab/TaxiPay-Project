import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';
import { authGuard, guestGuard, roleGuard } from './core/guards/auth.guard';
import { AuthService } from './core/services/auth.service';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      // Public Landing / Login Page
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
      },

      // Direct Auth Shorthands
      {
        path: 'login',
        canActivate: [guestGuard],
        loadComponent: () => import('./pages/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        canActivate: [guestGuard],
        loadComponent: () => import('./pages/auth/register/register.component').then(m => m.RegisterComponent)
      },

      // Dedicated Admin Login
      {
        path: 'admin/login',
        canActivate: [guestGuard],
        loadComponent: () => import('./pages/admin/admin-login/admin-login.component').then(m => m.AdminLoginComponent)
      },

      // Auth Routes
      {
        path: 'auth',
        children: [
          {
            path: 'login',
            canActivate: [guestGuard],
            loadComponent: () => import('./pages/auth/login/login.component').then(m => m.LoginComponent)
          },
          {
            path: 'register',
            canActivate: [guestGuard],
            loadComponent: () => import('./pages/auth/register/register.component').then(m => m.RegisterComponent)
          },
          {
            path: '',
            redirectTo: 'login',
            pathMatch: 'full'
          }
        ]
      },

      // Unified /dashboard alias - redirects to appropriate role dashboard
      {
        path: 'dashboard',
        redirectTo: () => {
          const auth = inject(AuthService);
          if (auth.isAdmin()) return 'admin/dashboard';
          if (auth.isDriver()) return 'driver/dashboard';
          return 'passenger/dashboard';
        }
      },

      // Direct Role Dashboard Aliases
      {
        path: 'admin-dashboard',
        redirectTo: 'admin/dashboard'
      },
      {
        path: 'passenger-dashboard',
        redirectTo: 'passenger/dashboard'
      },
      {
        path: 'driver-dashboard',
        redirectTo: 'driver/dashboard'
      },

      // Admin Protected Routes
      {
        path: 'admin',
        canActivate: [authGuard, roleGuard(['admin'])],
        children: [
          {
            path: 'dashboard',
            loadComponent: () => import('./pages/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
          },
          {
            path: 'drivers',
            loadComponent: () => import('./pages/admin/admin-drivers/admin-drivers.component').then(m => m.AdminDriversComponent)
          },
          {
            path: 'routes',
            loadComponent: () => import('./pages/admin/admin-routes/admin-routes.component').then(m => m.AdminRoutesComponent)
          },
          {
            path: 'fares',
            loadComponent: () => import('./pages/admin/admin-fares/admin-fares.component').then(m => m.AdminFaresComponent)
          },
          {
            path: 'route-requests',
            loadComponent: () => import('./pages/admin/admin-route-requests/admin-route-requests.component').then(m => m.AdminRouteRequestsComponent)
          },
          {
            path: 'disputes',
            loadComponent: () => import('./pages/admin/admin-disputes/admin-disputes.component').then(m => m.AdminDisputesComponent)
          },
          {
            path: 'analytics',
            loadComponent: () => import('./pages/admin/admin-analytics/admin-analytics.component').then(m => m.AdminAnalyticsComponent)
          },
          {
            path: 'profile',
            loadComponent: () => import('./pages/admin/admin-profile/admin-profile.component').then(m => m.AdminProfileComponent)
          },
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          }
        ]
      },

      // Passenger Protected Routes
      {
        path: 'passenger',
        canActivate: [authGuard, roleGuard(['passenger'])],
        children: [
          {
            path: 'dashboard',
            loadComponent: () => import('./pages/passenger/passenger-dashboard/passenger-dashboard.component').then(m => m.PassengerDashboardComponent)
          },
          {
            path: 'pay',
            loadComponent: () => import('./pages/passenger/pay-trip/pay-trip.component').then(m => m.PayTripComponent)
          },
          {
            path: 'trips',
            loadComponent: () => import('./pages/passenger/trip-history/trip-history.component').then(m => m.TripHistoryComponent)
          },
          {
            path: 'fare-matrix',
            loadComponent: () => import('./pages/passenger/fare-matrix/fare-matrix.component').then(m => m.FareMatrixComponent)
          },
          {
            path: 'route-requests',
            loadComponent: () => import('./pages/passenger/passenger-route-requests/passenger-route-requests.component').then(m => m.PassengerRouteRequestsComponent)
          },
          {
            path: 'profile',
            loadComponent: () => import('./pages/passenger/passenger-profile/passenger-profile.component').then(m => m.PassengerProfileComponent)
          },
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          }
        ]
      },

      // Driver Protected Routes
      {
        path: 'driver',
        canActivate: [authGuard, roleGuard(['driver'])],
        children: [
          {
            path: 'dashboard',
            loadComponent: () => import('./pages/driver/driver-dashboard/driver-dashboard.component').then(m => m.DriverDashboardComponent)
          },
          {
            path: 'shift-summary',
            loadComponent: () => import('./pages/driver/shift-summary/shift-summary.component').then(m => m.ShiftSummaryComponent)
          },
          {
            path: 'route-change',
            loadComponent: () => import('./pages/driver/route-change/route-change.component').then(m => m.RouteChangeComponent)
          },
          {
            path: 'profile',
            loadComponent: () => import('./pages/driver/driver-profile/driver-profile.component').then(m => m.DriverProfileComponent)
          },
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          }
        ]
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
