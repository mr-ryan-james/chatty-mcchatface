import { Component } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, UserLoginDto } from '../../shared/services/auth.service';
import { SignalrService } from '../../shared/services/signalr.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
})
export class LoginComponent {
  errorMessage: string | null = null;
  loading = false;
  returnUrl: string = '/chat';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private signalrService: SignalrService
  ) {
    // Get return url from route parameters or default to '/chat'
    this.route.queryParams.subscribe((params) => {
      this.returnUrl = params['returnUrl'] || '/chat';
    });
  }

  loginClicked(formValue: { email: string; password: string }) {
    this.errorMessage = null; // Clear previous error
    this.loading = true;

    console.log('Login attempt for:', formValue.email);

    const loginDto: UserLoginDto = {
      username: formValue.email, // Using email as username for now
      password: formValue.password,
    };

    this.authService.login(loginDto).subscribe({
      next: (response) => {
        console.log('Login successful');

        // Start SignalR connection after successful login
        this.signalrService
          .startConnection()
          .then(() => {
            console.log('SignalR connection established after login');
            // Navigate to the return URL (or chat by default)
            this.router.navigateByUrl(this.returnUrl);
          })
          .catch((err) => {
            console.error('Error establishing SignalR connection:', err);
            // Still navigate even if SignalR connection fails
            this.router.navigateByUrl(this.returnUrl);
          });

        this.loading = false;
      },
      error: (error) => {
        console.error('Login error:', error);
        this.loading = false;

        // Extract specific error message if possible
        if (typeof error === 'string') {
          this.errorMessage = error;
        } else if (error.message) {
          this.errorMessage = error.message;
        } else {
          this.errorMessage = 'Login failed. Please check your credentials.';
        }
      },
    });
  }
}
