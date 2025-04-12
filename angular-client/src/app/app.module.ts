import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SharedModule } from './shared/shared.module';
import { APP_INITIALIZER } from '@angular/core';
import { initializeAppFactory } from './app-initializer';
import { AuthService } from './shared/services/auth.service';
import { Observable } from 'rxjs'; // Needed for the factory return type hint

@NgModule({
  declarations: [],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
    SharedModule,
    AppComponent, // Import standalone component instead of declaring it
  ],
  providers: [
    AuthService, // Ensure AuthService is provided if not already
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAppFactory,
      deps: [AuthService], // Explicitly list dependencies for the factory
      multi: true,
    },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
