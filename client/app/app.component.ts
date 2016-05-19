import { Component } from 'angular2/core';
import { HTTP_PROVIDERS } from 'angular2/http';
import { RouteConfig, ROUTER_DIRECTIVES, ROUTER_PROVIDERS } from 'angular2/router';
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
  { path: '/register', name: 'Register', component: UserRegisterComponent, useAsDefault: true },
  { path: '/login', name: 'Login', component: UserLoginComponent },
  { path: '/mcchatface/...', name: 'Chat', component: ChatComponent }
])
export class AppComponent { }
