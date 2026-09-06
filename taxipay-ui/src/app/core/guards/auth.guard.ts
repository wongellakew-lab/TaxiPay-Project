import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/auth.models';

/**
 * Ensures the user has a valid, unexpired authentication session.
 * If not authenticated, redirects to /login (or /admin/login for admin URLs) with returnUrl query parameter.
 */
export const authGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);

  if (authService.isLoggedIn()) {
    return true;
  }

  snackBar.open('Please sign in to access this page.', 'Dismiss', {
    duration: 3500,
    panelClass: 'snack-warning'
  });

  const isAdminRoute = state.url.startsWith('/admin');
  return router.createUrlTree([isAdminRoute ? '/admin/login' : '/login'], {
    queryParams: { returnUrl: state.url }
  });
};

/**
 * Enforces role-based authorization for protected routes.
 * Ensures the authenticated user possesses one of the allowed roles.
 * If unauthorized, redirects to the user's appropriate dashboard.
 */
export const roleGuard = (allowedRoles: ('passenger' | 'driver' | 'admin')[]): CanActivateFn => {
  return (route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const snackBar = inject(MatSnackBar);

    // 1. Verify authentication
    if (!authService.isLoggedIn()) {
      snackBar.open('Please sign in to access this page.', 'Dismiss', {
        duration: 3500,
        panelClass: 'snack-warning'
      });
      const isAdminRoute = state.url.startsWith('/admin');
      return router.createUrlTree([isAdminRoute ? '/admin/login' : '/login'], {
        queryParams: { returnUrl: state.url }
      });
    }

    const currentRole = authService.userRole();

    // 2. Verify role authorization
    if (allowedRoles.includes(currentRole as 'passenger' | 'driver' | 'admin')) {
      return true;
    }

    // 3. Unauthorized role access handling
    let portalName = 'Requested';
    if (allowedRoles.includes('admin')) portalName = 'Admin Console';
    else if (allowedRoles.includes('driver')) portalName = 'Driver';
    else if (allowedRoles.includes('passenger')) portalName = 'Passenger';

    snackBar.open(`Access denied: You do not have permission to access the ${portalName} portal.`, 'Dismiss', {
      duration: 4000,
      panelClass: 'snack-error'
    });

    // Redirect to the user's own authorized dashboard
    if (currentRole === 'admin') {
      return router.createUrlTree(['/admin/dashboard']);
    } else if (currentRole === 'driver') {
      return router.createUrlTree(['/driver/dashboard']);
    } else if (currentRole === 'passenger') {
      return router.createUrlTree(['/passenger/dashboard']);
    }

    return router.createUrlTree(['/login']);
  };
};

/**
 * For public auth pages like /login, /register, and /admin/login.
 * If already signed in, redirects the user to their active dashboard.
 */
export const guestGuard: CanActivateFn = (): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    return router.createUrlTree([authService.getDashboardRoute()]);
  }

  return true;
};
