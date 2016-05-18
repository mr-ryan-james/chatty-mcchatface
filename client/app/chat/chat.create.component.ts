import { Component, Input, OnInit } from 'angular2/core';
import { RouteConfig, RouteParams, Router, ROUTER_DIRECTIVES } from 'angular2/router';
import { Chat, ChatService } from './chat.service.ts';
import { User, UserService } from '../user/user.service.ts'
import { Observable } from 'rxjs/Rx';


@Component({
    selector: 'chat-create',
    templateUrl: 'app/chat/chat.create.component.html',
    styles: [`    
    a {
      text-decoration: none;
    }

    a:hover {
      text-decoration: none;
      cursor: pointer;
    }`],
    directives: [ROUTER_DIRECTIVES]
})
export class ChatCreateComponent implements OnInit {
    errorMessage: string;

    users: Observable<User[]>;
    selectedUsers: User[];

    constructor(
        private _chatService: ChatService,
        private _userService: UserService
    ) { }

    ngOnInit() {
        this.users = this._userService.getUsers();
    }

    addUser(user) {

        if (!this.selectedUsers) {
            this.selectedUsers = [];
        }

        if (this.findSelectedUserIndex(user) >= 0)
            return;


        this.selectedUsers.push(user);
    }

    removeUser(user) {
        let index = this.findSelectedUserIndex(user);
        this.selectedUsers.splice(index, 1);
    }

    private findSelectedUserIndex(user) {
        return this.selectedUsers.findIndex(function (u) {
            return u._id === user._id;
        });
    }

}