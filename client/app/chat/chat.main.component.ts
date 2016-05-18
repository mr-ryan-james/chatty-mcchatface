import { Component, Input, OnInit } from 'angular2/core';
import { RouteConfig, RouteParams, Router, ROUTER_DIRECTIVES } from 'angular2/router';
import { ChatCreateComponent } from './chat.create.component.ts';
import { ChatListComponent } from './chat.list.component.ts';
import { Chat, ChatService } from './chat.service.ts';
import { AuthService } from '../auth.service.ts';


@Component({
    selector: 'app-chat',
    templateUrl: 'app/chat/chat.main.component.html',
    directives: [ChatCreateComponent, ChatListComponent]
})
export class ChatComponent implements OnInit {
    errorMessage: string;

    constructor(
        private _chatService: ChatService,
        private _authService: AuthService,
        private _router: Router
    ) { }

    ngOnInit() {
        if(!this._authService.getToken()){
            this._router.navigate(['Login']);
        }
    }

}