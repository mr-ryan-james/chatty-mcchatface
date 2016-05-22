import { Component, Input, OnInit } from '@angular/core';
import { RouteConfig, RouteParams, Router, ROUTER_DIRECTIVES } from '@angular/router-deprecated';

import { Chat, Chatroom, ChatService } from './chat.service.ts';
import {MomentPipe} from '../utility/moment.pipe.ts';
import {UserNamesPipe} from '../utility/usernames.pipe.ts';


@Component({
    selector: 'chat-list',
    templateUrl: 'app/chat/chat.list.component.html',
    styles: [`
        article{
            padding-left: 0;
        }
        
        .author-wrapper .arrow {
            float: left;
            position: relative;
            margin-left: 50px;
            width: 0;
            height: 0;
            border-top: 30px solid transparent;
            border-top: 30px solid #ccc;
            border-left: 30px solid transparent;
            border-right: 0 solid transparent;
        }
    `],
    directives: [ROUTER_DIRECTIVES],
    pipes: [MomentPipe, UserNamesPipe]
})
export class ChatListComponent implements OnInit {
    errorMessage: string;

    _chatrooms: Chatroom[];

    constructor(
        private _chatService: ChatService,
        private _router: Router
    ) { }

    ngOnInit(){
        this._chatService.getChatroomsForUser().subscribe(chatrooms=> {
            this._chatrooms = chatrooms;
        });
    }
    
    enterChatroom(chatroom:Chatroom){
        this._router.navigate(['Room', {id: chatroom._id}]);
    }

}