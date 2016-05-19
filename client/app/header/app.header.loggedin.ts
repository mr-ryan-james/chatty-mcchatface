import {Component, Input} from 'angular2/core';
import {User, UserService} from '../user/user.service.ts'
import { Router } from 'angular2/router';

@Component({
    selector: "chatty-header-loggedin",
    templateUrl: "/app/header/app.header.loggedin.html"
})
export class AppHeaderLoggedInComponent {

    @Input() user: User;

    constructor(
        private _userService: UserService,
        private _router: Router
    ) { }

    logout() {
        this._userService.logoutUser();
        this._router.navigate(['Login']);
    }
}