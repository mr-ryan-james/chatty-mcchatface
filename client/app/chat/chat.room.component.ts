import {Component, OnInit} from 'angular2/core';
import { Router, RouteParams, RouteSegment } from 'angular2/router';
import {Chat, Chatroom, ChatService} from './chat.service.ts';
import {User} from '../user/user.service.ts';

@Component({
    selector: 'chatty-room',
    templateUrl: '/app/chat/chat.room.component.html'
})
export class ChatRoomComponent implements OnInit {

    chatroom: Chatroom;
    users: User[];

    constructor(
        private _chatService: ChatService,
        private _router: Router,
        private routeParams:RouteParams

    ) { }


    ngOnInit(curr: RouteSegment): void {
        let id = this.routeParams.get('id');
        this._chatService
            .getChatroom(id)
            .subscribe(chatroom => {
                this.chatroom = chatroom;
                this.users = chatroom.users;
            });
    }
    
    exitChat(): void{
        this._router.navigate(["Main"]);
    }
}