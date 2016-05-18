import { Component, Input, OnInit } from 'angular2/core';
import { RouteConfig, RouteParams, Router, ROUTER_DIRECTIVES } from 'angular2/router';

import { Chat, ChatService } from './chat.service.ts';


@Component({
    selector: 'chat-list',
    templateUrl: 'app/chat/chat.list.component.html',
    directives: [ROUTER_DIRECTIVES]
})
export class ChatListComponent implements OnInit {
    errorMessage: string;

    constructor(
        private _chatService: ChatService
    ) { }



}