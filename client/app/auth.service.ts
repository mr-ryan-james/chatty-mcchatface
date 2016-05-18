import { Injectable } from 'angular2/core';

@Injectable()
export class AuthService {
    
    constructor(){
        
    }
    
    setToken(token:String){
        localStorage.setItem("token", token);
    }
    
    getToken():String{
        return localStorage.getItem("token");
    }
}