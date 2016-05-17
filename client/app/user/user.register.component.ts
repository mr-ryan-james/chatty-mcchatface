import { Component, Input, OnInit } from 'angular2/core';
import { RouteParams, Router, ROUTER_DIRECTIVES } from 'angular2/router';

import { User, UserService } from './user.service.ts';

@Component({
  selector: 'app-user',
  templateUrl: 'app/user/user.register.component.html',
  directives: [ROUTER_DIRECTIVES]
})
export class UserRegisterComponent  {
  @Input() user: User;

  errorMessage: string;

  constructor(
    private _userService: UserService,
    private _routeParams: RouteParams,
    private _router: Router) { }

  registerClicked(email, password, firstName, lastName) {
    let user = new User();
    user.email = email;
    user.firstName = firstName;
    user.lastName = lastName;
    user.password = password;

    this._userService.registerUser(user)
      .subscribe(
      user => {
        this._router.navigate(['Chat']);

      },
      error => this.errorMessage = <any>error);
  }

}