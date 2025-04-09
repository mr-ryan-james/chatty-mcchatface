import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { take, map, tap } from 'rxjs';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';
import { User } from '../../shared/services/user.service';
import { SignalrService } from '../../shared/services/signalr.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css'],
  standalone: true,
  imports: [CommonModule, RouterModule],
})
export class HeaderComponent implements OnInit, OnDestroy {
  currentUser: User | null = null;
  private userSubscription: Subscription | null = null;
  fullTitle = 'Chatty McChatface';
  displayedTitle = '';
  private typingSubscription: any;

  constructor(
    private router: Router,
    private authService: AuthService,
    private signalrService: SignalrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userSubscription = this.authService.currentUser$.subscribe((user) => {
      this.currentUser = user;
      console.log('Header received user:', this.currentUser);
    });
    this.startTypingAnimation();
  }

  ngOnDestroy(): void {
    if (this.userSubscription) {
      this.userSubscription.unsubscribe();
    }
    if (this.typingSubscription) {
      this.typingSubscription.unsubscribe();
    }
  }

  private startTypingAnimation(): void {
    const typingSpeed = 150; // Milliseconds between characters
    this.displayedTitle = ''; // Reset title

    this.typingSubscription = interval(typingSpeed)
      .pipe(
        take(this.fullTitle.length), // Take one emission for each character
        map((index) => this.fullTitle.substring(0, index + 1)), // Get substring
        tap((titlePart) => {
          this.displayedTitle = titlePart;
          this.cdr.detectChanges(); // Manually trigger change detection
        })
      )
      .subscribe();
  }

  login(): void {
    this.router.navigate(['/user/login']);
  }

  signUp(): void {
    this.router.navigate(['/user/register']);
  }

  logout(): void {
    // Disconnect from SignalR before logging out
    if (this.signalrService.isConnectedToHub()) {
      this.signalrService
        .stopConnection()
        .catch((err) => console.error('Error disconnecting from SignalR:', err))
        .finally(() => {
          // Complete the logout process regardless of SignalR disconnection status
          this.completeLogout();
        });
    } else {
      this.completeLogout();
    }
  }

  private completeLogout(): void {
    // Call the auth service logout method
    this.authService.logout();

    // The router navigation is already handled in the AuthService.logout method
    // but we could add additional logic here if needed
  }
}
