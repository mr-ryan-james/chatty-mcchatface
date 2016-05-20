import { Component, OnInit } from '@angular/core';
import {User} from '../user/user.service.ts'
import {AuthService} from '../auth.service.ts'
import {UserService} from '../user/user.service.ts'
import {AppHeaderLoggedOutComponent} from './app.header.loggedout.ts'
import {AppHeaderLoggedInComponent} from './app.header.loggedin.ts'

@Component({
  selector: 'chatty-header',
  templateUrl: 'app/header/app.header.main.html',
  directives: [AppHeaderLoggedInComponent,AppHeaderLoggedOutComponent],
  styles: [``]
})
export class AppHeaderComponent implements OnInit {

  loggedIn: boolean = true;
  user: User;

  constructor(
    private _authService: AuthService,
    private _userService: UserService
  ) { }

  ngOnInit() {
    this.user = this._authService.getUserInfo();
    
    this._userService.nowWeSeeYou$.subscribe(user => this.user = user);
    this._userService.nowWeDont$.subscribe(user => this.user = null);
  }
}