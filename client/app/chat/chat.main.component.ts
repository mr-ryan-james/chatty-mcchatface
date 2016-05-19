import { Component, Input } from 'angular2/core';
import { ChatCreateComponent } from './chat.create.component.ts';
import { ChatListComponent } from './chat.list.component.ts';


@Component({
    selector: 'app-chat',
    templateUrl: 'app/chat/chat.main.component.html',
    directives: [ChatCreateComponent, ChatListComponent]
})

export class ChatMainComponent   {
    errorMessage: string;

    constructor() { }

}