import { Component, Input, OnInit } from 'angular2/core';
import { RouteConfig, RouteParams, Router, ROUTER_DIRECTIVES } from 'angular2/router';
import { ChatCreateComponent } from './chat.create.component.ts';
import { ChatListComponent } from './chat.list.component.ts';
import { Chat, ChatService } from './chat.service.ts';


@Component({
    selector: 'app-chat',
    templateUrl: 'app/chat/chat.main.component.html',
    directives: [ChatCreateComponent, ChatListComponent]
})
export class ChatComponent implements OnInit {
    errorMessage: string;

    constructor(
        private _chatService: ChatService
    ) { }

    ngOnInit() {
        //check for auth
    }

}