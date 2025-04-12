import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { delay, tap, map, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { User } from './user.service';
import { environment } from '../../../environments/environment';

export interface UserLoginDto {
  email: string;
  password: string;
}

export interface UserRegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  password: string;
}

export interface AuthResponseDto {
  token: string;
  email: string; // Add
  firstName: string; // Add
  lastName: string; // Add
  userId: number; // Add (use number to match backend)
  // Remove user: User;
  // Remove expiration: Date;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private currentUserSubject: BehaviorSubject<User | null> =
    new BehaviorSubject<User | null>(null);
  public currentUser$: Observable<User | null> =
    this.currentUserSubject.asObservable();
  private isAuthenticatedSubject: BehaviorSubject<boolean> =
    new BehaviorSubject<boolean>(false);
  public isAuthenticated$: Observable<boolean> =
    this.isAuthenticatedSubject.asObservable();

  // Mock user for development
  private mockUser: User = {
    id: '1',
    firstName: 'John',
    lastName: 'Doe',
    email: 'john@example.com',
    username: 'johndoe',
  };

  constructor(private http: HttpClient, private router: Router) {}

  login(loginDto: UserLoginDto): Observable<AuthResponseDto> {
    console.log(`Attempting login with email: ${loginDto.email}`);

    return this.http
      .post<AuthResponseDto>(`${environment.apiUrl}/auth/login`, loginDto)
      .pipe(
        tap((response) => this.handleAuthentication(response)),
        catchError(this.handleError)
      );
  }

  register(registerDto: UserRegisterDto): Observable<AuthResponseDto> {
    return this.http
      .post<AuthResponseDto>(`${environment.apiUrl}/auth/register`, registerDto)
      .pipe(
        tap((response) => this.handleAuthentication(response)),
        catchError(this.handleError)
      );
  }

  logout(): void {
    // Remove token from localStorage
    localStorage.removeItem('token');

    // Reset authentication state
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);

    // Navigate to login page
    this.router.navigate(['/user/login']);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    return !!token; // Basic check, in a real app would also check expiration
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getUserInfo(): User | null {
    return this.currentUserSubject.value;
  }

  /**
   * Validate token (if present) and load user from /auth/me endpoint.
   * Used for app initialization to restore user state from token.
   */
  validateAndLoadUser(): Observable<boolean> {
    const token = this.getToken();
    if (!token) {
      // No token, ensure logged out state and complete initialization
      this.logout(); // Call logout to ensure state is reset
      return of(true);
    }

    // Token exists, call the /api/auth/me endpoint
    // Use getHttpOptions() to include the Authorization header
    return this.http
      .get<User>(`${environment.apiUrl}/auth/me`, this.getHttpOptions())
      .pipe(
        tap((user: User) => {
          // Success: Update subjects with real user data
          if (user) {
            // Convert ID to string if necessary (assuming backend returns number)
            const frontendUser: User = { ...user, id: user.id.toString() };
            this.currentUserSubject.next(frontendUser);
            this.isAuthenticatedSubject.next(true);
            console.log(
              '[AuthService] User loaded via token validation:',
              frontendUser
            );
          } else {
            // Should not happen if API returns valid user on success, but handle defensively
            console.error(
              '[AuthService] /auth/me returned success but no user data.'
            );
            this.logout(); // Treat as failure
          }
        }),
        map(() => true), // Map successful response to true for APP_INITIALIZER
        catchError((error) => {
          // Failure (e.g., 401 Unauthorized, network error)
          console.error('[AuthService] Token validation failed:', error);
          this.logout(); // Clear invalid token and reset state
          return of(true); // IMPORTANT: Return true even on error so app initialization completes
        })
      );
  }

  private handleAuthentication(response: AuthResponseDto): void {
    // Destructure directly from the response
    const { token, userId, email, firstName, lastName } = response;
    console.log(
      '[AuthService] handleAuthentication called. Response:',
      response
    );

    localStorage.setItem('token', token);

    // Create the User object expected by the frontend
    const user: User = {
      id: userId.toString(), // Convert number to string for User interface
      firstName: firstName,
      lastName: lastName,
      email: email,
      // username is missing from backend response, set to email or similar?
      username: email, // Or construct from first/last name if preferred
    };

    this.isAuthenticatedSubject.next(true);
    console.log('[AuthService] isAuthenticatedSubject updated to true');
    this.currentUserSubject.next(user); // Pass the constructed User object
    console.log('[AuthService] currentUserSubject updated. User:', user);
  }

  // Helper methods for HTTP requests
  getHttpOptions() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: `Bearer ${this.getToken()}`,
      }),
    };
  }

  private handleError(error: any) {
    console.error('API error:', error);
    let errorMessage = 'An unknown error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else if (error.status) {
      // Server-side error
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }

    return throwError(() => new Error(errorMessage));
  }
}
