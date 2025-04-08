import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { delay, tap, map, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { User } from './user.service';
import { environment } from '../../../environments/environment';

export interface UserLoginDto {
  username: string;
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
  user: User;
  expiration: Date;
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

  constructor(private http: HttpClient, private router: Router) {
    // Check if we have token in localStorage (for persisting login state)
    const token = localStorage.getItem('token');
    if (token) {
      this.isAuthenticatedSubject.next(true);
      this.currentUserSubject.next(this.mockUser);
    }
  }

  login(loginDto: UserLoginDto): Observable<AuthResponseDto> {
    console.log(`Attempting login with username: ${loginDto.username}`);

    return this.http
      .post<AuthResponseDto>(`${environment.apiUrl}/auth/login`, loginDto)
      .pipe(
        tap((response) => this.handleAuthentication(response)),
        catchError(this.handleError)
      );
  }

  register(registerDto: UserRegisterDto): Observable<any> {
    return this.http
      .post<any>(`${environment.apiUrl}/auth/register`, registerDto)
      .pipe(catchError(this.handleError));
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

  private handleAuthentication(response: AuthResponseDto): void {
    const { token, user } = response;

    // Store token in localStorage
    localStorage.setItem('token', token);

    // Update authentication state
    this.isAuthenticatedSubject.next(true);
    this.currentUserSubject.next(user);
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
