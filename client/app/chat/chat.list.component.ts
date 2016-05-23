import { Component, Input, OnInit } from '@angular/core';
import { RouteConfig, RouteParams, Router, ROUTER_DIRECTIVES } from '@angular/router-deprecated';
import {  BaseAuthComponent } from '../base.component.ts';
import {AuthService} from '../auth.service.ts';
import { Chat, Chatroom, ChatService } from './chat.service.ts';
import {  UserService } from '../user/user.service.ts';
import {MomentPipe} from '../utility/moment.pipe.ts';
import {UserNamesPipe} from '../utility/usernames.pipe.ts';
import * as io from 'socket';


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
export class ChatListComponent extends BaseAuthComponent implements OnInit {
    errorMessage: string;

    _chatrooms: Chatroom[];

    socket: any;

    constructor(
        private _chatService: ChatService,
        private _authService: AuthService,
        protected _router: Router,
        protected _userService: UserService
    ) {
        super(_router, _userService);
        super.registerService(_chatService);
    }

    ngOnInit() {
        let userInfo = this._authService.getUserInfo();
        if (!userInfo) {
            return;
        }

        let chatroomObservables = this._chatService.getChatroomsForUser();
        if (chatroomObservables) {
            chatroomObservables.subscribe(chatrooms => {
                this._chatrooms = chatrooms;
            });
        }

        this.listenForChatroomChanges();
    }

    enterChatroom(chatroom: Chatroom) {
        this._router.navigate(['Room', { id: chatroom._id }]);
    }

    private listenForChatroomChanges() {
        this.socket = io.connect();
        this.socket.emit("listenForChatrooms", this._authService.getUserInfo()._id);;

        this.socket.on("newChatroom", (chatroom) => {
            this.removeAndReaddChatroom(chatroom);
        });
    }

    private removeAndReaddChatroom(chatroom) {
        let existingIndex = this._chatrooms.findIndex((c) => c._id === chatroom._id);

        if (existingIndex === -1)
        {
            this._chatrooms.push(chatroom);
            return;
        }

        this._chatrooms.splice(existingIndex, 1)
        this._chatrooms.push(chatroom);
    }

}