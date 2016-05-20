import { Component, Input, OnInit } from '@angular/core';
import { RouteConfig, RouteParams, Router, ROUTER_DIRECTIVES } from '@angular/router-deprecated';
import { ChatMainComponent } from './chat.main.component.ts';
import { ChatRoomComponent } from './chat.room.component.ts';
import { Chat, ChatService } from './chat.service.ts';
import { AuthService } from '../auth.service.ts';


@Component({
    selector: 'app-chat',
    template: `<router-outlet></router-outlet>`,
    directives: [ROUTER_DIRECTIVES],
})
@RouteConfig([
    { path: '/', name: 'Main', component: ChatMainComponent, useAsDefault: true },
    { path: '/room', name: 'Room', component: ChatRoomComponent }
])
export class ChatComponent implements OnInit {

    constructor(
        private _chatService: ChatService,
        private _authService: AuthService,
        private _router: Router
    ) { }

    ngOnInit() {
        if (!this._authService.getToken()) {
            this._router.navigate(['Login']);
        }
    }


}