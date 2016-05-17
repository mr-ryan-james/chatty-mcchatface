import { Component, Input, OnInit } from 'angular2/core';
import { RouteParams, Router, ROUTER_DIRECTIVES } from 'angular2/router';

import { Chat, ChatService } from './chat.service.ts';


@Component({
    selector: 'app-chat',
    templateUrl: 'app/chat/chat.main.component.html',
    directives: [ROUTER_DIRECTIVES]
})
export class ChatComponent implements OnInit  {
    errorMessage: string;

    constructor(
        private _chatService: ChatService
    ) { }



}