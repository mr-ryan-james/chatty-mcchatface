import { inject } from '@angular/core';
import { AuthService } from './shared/services/auth.service';
import { Observable, of } from 'rxjs';

// Factory function for APP_INITIALIZER
export function initializeAppFactory(): () => Observable<boolean> {
  const authService = inject(AuthService); // Use inject() for dependency injection

  return () => {
    // Check if a token exists in localStorage
    const token = authService.getToken(); // Assuming getToken() exists and is synchronous

    if (token) {
      // If token exists, attempt to validate and load user
      // The validateAndLoadUser method should handle success/failure internally
      // and return an Observable that completes (e.g., using catchError + of(true))
      return authService.validateAndLoadUser(); // We will implement this method next
    } else {
      // No token, initialization is complete immediately
      return of(true); // Return an observable that completes immediately
    }
  };
}
