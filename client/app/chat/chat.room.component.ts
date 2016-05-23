import {Component, OnInit, } from '@angular/core';
import { Router, RouteParams, RouteSegment } from '@angular/router-deprecated';
import {Chat, Chatroom, ChatService} from './chat.service.ts';
import {User, UserService} from '../user/user.service.ts';
import {BaseAuthComponent} from '../base.component.ts';
import * as io from 'socket';
import {MomentPipe} from '../utility/moment.pipe.ts';

@Component({
    selector: 'chatty-room',
    styles: [`
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
    templateUrl: '/app/chat/chat.room.component.html',
    pipes: [MomentPipe]
})
export class ChatRoomComponent extends BaseAuthComponent implements OnInit {

    chatroom: Chatroom;
    chats: Chat[];
    users: User[];
    text: String;
    
    socket: any;

    constructor(
        private _chatService: ChatService,
        protected _router: Router,
        private routeParams: RouteParams,
        protected _userService: UserService
    ) {
        super(_router, _userService);
        super.registerService(_chatService);
    }


    ngOnInit(): void {
        let id = this.routeParams.get('id');
        this._chatService
            .getChatroom(id)
            .subscribe(chatroom => {
                this.chatroom = chatroom;
                this.users = chatroom.users;
                this.chats = chatroom.chats;
                setTimeout(this.scrollDivDown, 300);
            });
            
        this.joinChatroom(id);
    }

    sendChat(): void {
        this._chatService
            .sendChat(this.text, this.chatroom._id)
            .subscribe(chat => {
                this.chats.push(chat);
                setTimeout(this.scrollDivDown, 300);
            });
        this.text = "";
    }

    exitChat(): void {
        this._router.navigate(["Main"]);
    }
    
    private scrollDivDown(){
        let div = document.getElementById("chatContainer");
        div.scrollTop = div.scrollHeight + 500;
    }
    
    private joinChatroom(id:String){
        this.socket = io.connect();
        
        this.socket.emit("joinChatroom", id);

		this.socket.on("newMessage", (chat) => {
			this.chats.push(chat);
            setTimeout(this.scrollDivDown, 300);            
		});
    }
}