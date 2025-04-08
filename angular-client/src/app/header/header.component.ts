import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../shared/services/auth.service';
import { User } from '../shared/services/user.service'; // Assuming User interface is here
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css'],
  standalone: true,
  imports: [CommonModule, RouterModule],
})
export class HeaderComponent implements OnInit {
  isAuthenticated$: Observable<boolean>;
  currentUser$: Observable<User | null>;

  constructor(private authService: AuthService, private router: Router) {
    console.log('[HeaderComponent] Constructor called'); // ADD THIS
    this.isAuthenticated$ = this.authService.isAuthenticated$;
    this.currentUser$ = this.authService.currentUser$;
  }

  ngOnInit(): void {
    // ADD THIS METHOD
    console.log('[HeaderComponent] ngOnInit called');
    // Optional: Log subscription changes if needed for deeper debugging
    // this.isAuthenticated$.subscribe(val => console.log('[HeaderComponent] isAuthenticated$ emitted:', val));
    // this.currentUser$.subscribe(val => console.log('[HeaderComponent] currentUser$ emitted:', val));
  }

  logout(): void {
    this.authService.logout();
    // AuthService already handles navigation to login on logout,
    // but we can add it here too for explicitness if desired.
    // this.router.navigate(['/user/login']);
  }
}
