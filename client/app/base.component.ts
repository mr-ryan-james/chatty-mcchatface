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
            this.attemptToRoute(this._router);
        });
    }
    
    private attemptToRoute(router){
        if(router == null){
            throw new Error("Unable to autonavigate to Login");
        }
        
        try{
            router.navigate(['Login']);
        }
        catch(err){
            this.attemptToRoute(router.parent);
        }
    }

}