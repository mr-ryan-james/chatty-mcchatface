import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { delay, map, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  username?: string;
}

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  // Mock data for development (fallback if API is not available)
  private mockUsers: User[] = [
    {
      id: '1',
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      username: 'johndoe',
    },
    {
      id: '2',
      firstName: 'Jane',
      lastName: 'Smith',
      email: 'jane@example.com',
      username: 'janesmith',
    },
    {
      id: '3',
      firstName: 'Alice',
      lastName: 'Johnson',
      email: 'alice@example.com',
      username: 'alicej',
    },
    {
      id: '4',
      firstName: 'Bob',
      lastName: 'Brown',
      email: 'bob@example.com',
      username: 'bobb',
    },
  ];

  constructor(private http: HttpClient, private authService: AuthService) {}

  // Get all users
  getUsers(): Observable<UserDto[]> {
    console.log('Fetching all users');
    return this.http
      .get<UserDto[]>(`${environment.apiUrl}/users`, this.getAuthHeaders())
      .pipe(
        catchError((error) => {
          console.warn('API error, falling back to mock data', error);
          return of(this.mockUsers as UserDto[]);
        })
      );
  }

  // Get user by ID
  getUser(id: string): Observable<UserDto> {
    console.log(`Fetching user with ID: ${id}`);
    return this.http
      .get<UserDto>(`${environment.apiUrl}/users/${id}`, this.getAuthHeaders())
      .pipe(
        catchError((error) => {
          console.warn('API error, falling back to mock data', error);
          const user = this.mockUsers.find((u) => u.id === id);
          if (user) {
            return of(user as UserDto);
          }
          return throwError(() => new Error(`User with ID ${id} not found`));
        })
      );
  }

  // Delete user
  deleteUser(id: string): Observable<any> {
    console.log(`Deleting user with ID: ${id}`);
    return this.http
      .delete(`${environment.apiUrl}/users/${id}`, this.getAuthHeaders())
      .pipe(catchError(this.handleError));
  }

  // Get current logged-in user (from API)
  getCurrentUser(): Observable<UserDto> {
    const userInfo = this.authService.getUserInfo();
    if (!userInfo || !userInfo.id) {
      return throwError(() => new Error('No user logged in'));
    }
    return this.getUser(userInfo.id);
  }

  // Get all users except the current user
  getOtherUsers(): Observable<UserDto[]> {
    const currentUser = this.authService.getUserInfo();
    return this.getUsers().pipe(
      map((users) => users.filter((u) => u.id !== currentUser?.id))
    );
  }

  // Helper method for auth headers
  private getAuthHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: `Bearer ${this.authService.getToken()}`,
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
