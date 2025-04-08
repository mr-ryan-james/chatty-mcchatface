import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ChatRoutingModule } from './chat-routing.module';
import { ChatComponent } from './chat.component';
import { ChatMainComponent } from './chat-main/chat-main.component';
import { ChatListComponent } from './chat-list/chat-list.component';
import { ChatCreateComponent } from './chat-create/chat-create.component';
import { ChatRoomComponent } from './chat-room/chat-room.component';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [ChatComponent],
  imports: [
    CommonModule,
    FormsModule,
    ChatRoutingModule,
    SharedModule,
    ChatMainComponent,
    ChatListComponent,
    ChatCreateComponent,
    ChatRoomComponent,
  ],
})
export class ChatModule {}
