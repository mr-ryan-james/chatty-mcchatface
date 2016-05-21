import { Router } from '@angular/router-deprecated';
import { UserService } from './user/user.service.ts';
import { BaseService } from './base.service.ts';

export class BaseAuthComponent {

    constructor(
        protected _router: Router,
        protected _userService: UserService
    ){
        
    }

    registerService(baseService: BaseService){
        baseService.noLongerAuthenticated$.subscribe(() => {
            this._router.navigate(['Login']);
        });
    }

}