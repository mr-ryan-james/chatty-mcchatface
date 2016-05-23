import {Component} from '@angular/core';
import { Router } from '@angular/router-deprecated';

@Component({
    selector: "chatty-header-loggedout",
    templateUrl: "/app/header/app.header.loggedout.html"
})
export class AppHeaderLoggedOutComponent { 
    
    constructor(
        private _router:Router
    ){}
    
    login(){
        this._router.navigate(["Login"]);
    }
    
    signUp(){
        this._router.navigate(["Register"]);        
    }
    
}
