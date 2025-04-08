import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewInit,
  ViewChild,
  ElementRef,
  NgZone,
} from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
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
export class HeaderComponent implements OnInit, OnDestroy, AfterViewInit {
  currentUser: User | null = null;
  private userSubscription: Subscription | null = null;

  @ViewChild('logoCanvas') logoCanvasRef!: ElementRef<HTMLCanvasElement>;
  private ctx!: CanvasRenderingContext2D;
  private animationFrameId: number = 0;
  private width: number = 0;
  private height: number = 0;

  constructor(
    private router: Router,
    private authService: AuthService,
    private signalrService: SignalrService,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.userSubscription = this.authService.currentUser$.subscribe((user) => {
      this.currentUser = user;
      console.log('Header received user:', this.currentUser);
    });
  }

  ngOnDestroy(): void {
    if (this.userSubscription) {
      this.userSubscription.unsubscribe();
    }

    // Cancel any ongoing animation
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
    }
  }

  ngAfterViewInit(): void {
    this.initCanvas();
  }

  private initCanvas(): void {
    const canvas = this.logoCanvasRef.nativeElement;
    this.ctx = canvas.getContext('2d')!;
    this.width = canvas.width;
    this.height = canvas.height;

    // Initial font and style setup
    this.ctx.font = '60px Arial';
    const gradient = this.ctx.createLinearGradient(0, 0, this.width, 0);
    gradient.addColorStop(1, 'white');
    this.ctx.fillStyle = gradient;
    this.ctx.fillText('Chatty McChatface', 0, 50);

    this.initAnimation();
  }

  private initAnimation(): void {
    // Run the animation loop outside of Angular's change detection
    this.zone.runOutsideAngular(() => {
      this.animationFrameId = requestAnimationFrame(() => this.animateFrame());
    });
  }

  private animateFrame(): void {
    this.ctx.save();
    this.runShiz();
    this.ctx.restore();
    // Continue the animation loop
    this.animationFrameId = requestAnimationFrame(() => this.animateFrame());
  }

  private runShiz(): void {
    this.ctx.globalAlpha = 0.95;
    this.ctx.globalCompositeOperation = 'lighter';
    this.ctx.lineWidth = 1.5;
    this.ctx.strokeStyle = '#ff5522';

    this.doStyle();
  }

  private doStyle(): void {
    const time = Date.now() / 1600;
    const mix = Math.sin(time);
    this.ctx.translate(this.width / 2, this.height / 2);
    this.ctx.rotate(((Date.now() / 1600) % (Math.PI * 2)) * (1 - mix));
    this.ctx.beginPath();

    for (let t = 0; t < Math.PI * 12; t += Math.PI / 32) {
      const xy = this.mixf(this.f1(t), this.f2(t), mix);
      this.ctx.lineTo(xy.x * 8, -xy.y * 8);
    }

    this.ctx.closePath();
    this.ctx.stroke();
  }

  private mixf(
    xy1: { x: number; y: number },
    xy2: { x: number; y: number },
    mix: number
  ): { x: number; y: number } {
    return {
      x: xy1.x * mix + xy2.x * (1 - mix),
      y: xy1.y * mix + xy2.y * (1 - mix),
    };
  }

  private f1(t: number): { x: number; y: number } {
    return {
      x: 13 * Math.cos(t) - 6 * Math.cos((11 / 6) * t),
      y: 11 * Math.sin(t) - 6 * Math.sin((11 / 6) * t),
    };
  }

  private f2(t: number): { x: number; y: number } {
    return {
      x: 3 * Math.cos(3 * t),
      y: 5 * (3 * Math.sin(t)),
    };
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
