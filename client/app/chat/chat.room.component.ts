import {Component} from 'angular2/core';

import {ChatService} from './chat.service.ts';

@Component({
    selector: 'chatty-room',
    templateUrl: '/app/chat/chat.room.component.html'
})
export class ChatRoomComponent{
    
    constructor(
        private _chatService:ChatService
        ){}
    
    
    
}