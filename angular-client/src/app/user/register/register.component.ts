import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AuthService,
  UserRegisterDto,
} from '../../shared/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule],
})
export class RegisterComponent {
  errorMessage: string | null = null;
  loading = false;

  constructor(private router: Router, private authService: AuthService) {}

  registerClicked(formValue: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }) {
    this.errorMessage = null; // Clear previous error
    this.loading = true;

    console.log('Register attempt for:', formValue.email);

    // Create a username from email for now (can be changed later)
    const username = formValue.email.split('@')[0];

    const registerDto: UserRegisterDto = {
      email: formValue.email,
      password: formValue.password,
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      username: username,
    };

    this.authService.register(registerDto).subscribe({
      next: (response) => {
        console.log('Registration successful');
        this.loading = false;

        // Navigate to the chat page (or another appropriate page) after successful registration
        this.router.navigate(['/chat']); // Or potentially '/dashboard' or '/'
      },
      error: (error) => {
        console.error('Registration error:', error);
        this.loading = false;

        // Extract specific error message if possible
        if (typeof error === 'string') {
          this.errorMessage = error;
        } else if (error.message) {
          this.errorMessage = error.message;
        } else {
          this.errorMessage = 'Registration failed. Please try again.';
        }
      },
    });
  }
}
