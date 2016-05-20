import { Component } from '@angular/core';
import { HTTP_PROVIDERS } from '@angular/http';
import { RouteConfig, ROUTER_DIRECTIVES, ROUTER_PROVIDERS } from '@angular/router-deprecated';
import 'rxjs/Rx'; // load the full rxjs

import { AuthService } from './auth.service.ts';
import { UserService } from './user/user.service.ts';
import { ChatService } from './chat/chat.service.ts';
import { AppHeaderComponent } from './header/app.header.main.ts'
import { UserRegisterComponent } from './user/user.register.component.ts';
import { UserLoginComponent } from './user/user.login.component.ts';
import { ChatComponent } from './chat/chat.component.ts';


@Component({
  selector: 'chatty-mcchatface',
  templateUrl: 'app/app.component.html',
  styles: [``],
  directives: [ROUTER_DIRECTIVES,AppHeaderComponent],
  providers: [
    HTTP_PROVIDERS,
    ROUTER_PROVIDERS,
    AuthService,
    UserService,
    ChatService
  ]
})
@RouteConfig([
  { path: '/register', name: 'Register', component: UserRegisterComponent},
  { path: '/login', name: 'Login', component: UserLoginComponent },
  { path: '/mcchatface/...', name: 'Chat', component: ChatComponent, useAsDefault: true  }
])
export class AppComponent { }
