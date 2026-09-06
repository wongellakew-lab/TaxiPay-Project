import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.currentUser()?.token;

  let authReq = req;
  if (token) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 401) {
          // Do not auto-logout on failed login/register attempts
          const isAuthEndpoint =
            req.url.includes('/login') ||
            req.url.includes('/register') ||
            (req.url.endsWith('/drivers') && req.method === 'POST');

          if (!isAuthEndpoint && authService.isLoggedIn()) {
            authService.logout(true);
          }
        }

        // Standardize friendly user-facing messages for operational errors
        let userMessage = err.error?.detail || err.error?.message || '';
        if (err.status === 403) {
          userMessage = userMessage || 'Access denied: You do not have permission to perform this action.';
        } else if (err.status === 429) {
          userMessage = 'Too many requests. Please try again shortly.';
        } else if (err.status === 503) {
          userMessage = 'TaxiPay is temporarily unavailable. Please try again.';
        } else if (err.status === 500) {
          const traceId = err.error?.traceId;
          userMessage = traceId
            ? `An unexpected error occurred. Trace ID: ${traceId}`
            : 'An unexpected server error occurred. Please try again later.';
        } else if (err.status === 0) {
          userMessage = 'Unable to connect to TaxiPay server. Please check your network connection.';
        }

        if (userMessage) {
          const customError = new HttpErrorResponse({
            error: typeof err.error === 'object' && err.error !== null
              ? { ...err.error, message: userMessage, detail: userMessage }
              : { message: userMessage, detail: userMessage },
            headers: err.headers,
            status: err.status,
            statusText: err.statusText,
            url: err.url ?? undefined
          });
          return throwError(() => customError);
        }
      }

      return throwError(() => err);
    })
  );
};
