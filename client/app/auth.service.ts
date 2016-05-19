import { Injectable } from 'angular2/core';

@Injectable()
export class AuthService {

    constructor() {

    }

    setUserInfo(user: any) {
        localStorage.setItem("user", JSON.stringify(user));
    }

    getUserInfo() {
        return JSON.parse(localStorage.getItem("user"));
    }

    setToken(token: String) {
        localStorage.setItem("token", token);
    }

    getToken(): String {
        return localStorage.getItem("token");
    }

    deleteUserAndToken() {
        localStorage.removeItem("user");
        localStorage.removeItem("token");
    }
}