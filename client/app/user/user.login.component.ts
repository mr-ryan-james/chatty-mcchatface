import { Component, Input, OnInit } from '@angular/core';
import { RouteParams, Router, ROUTER_DIRECTIVES } from '@angular/router-deprecated';

import { User, UserService } from './user.service.ts';

@Component({
    selector: 'app-user',
    templateUrl: 'app/user/user.login.component.html',
    directives: [ROUTER_DIRECTIVES]
})
export class UserLoginComponent {
    errorMessage: string;

    constructor(
        private _userService: UserService,
        private _router: Router
    ) { }

    loginClicked(email, password) {
        let user = new User();
        user.email = email;
        user.password = password;

        this._userService.loginUser(user)
            .subscribe(
            user => {
                this._router.navigate(['Chat']);
            },
            error => this.errorMessage = <any>error);
    }

}