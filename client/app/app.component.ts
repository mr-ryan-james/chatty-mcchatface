import { Component } from 'angular2/core';
import { HTTP_PROVIDERS } from 'angular2/http';
import { RouteConfig, ROUTER_DIRECTIVES, ROUTER_PROVIDERS } from 'angular2/router';
import 'rxjs/Rx'; // load the full rxjs

import { UserService } from './user/user.service';
import { UserComponent } from './user/user.component';


@Component({
  selector: 'chatty-mcchatface',
  templateUrl: 'app/app.component.html',
  styles: [`
    nav ul {list-style-type: none;}
    nav ul li {padding: 4px;cursor: pointer;display:inline-block}
  `],
  directives: [ROUTER_DIRECTIVES],
  providers: [
    HTTP_PROVIDERS,
    ROUTER_PROVIDERS,
    UserService
  ]
})
@RouteConfig([
  { path: '/', name: 'User', component: UserComponent, useAsDefault: true }
])
export class AppComponent { }
