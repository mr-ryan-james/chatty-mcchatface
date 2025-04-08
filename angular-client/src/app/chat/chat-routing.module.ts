import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChatMainComponent } from './chat-main/chat-main.component';
import { ChatRoomComponent } from './chat-room/chat-room.component';
import { AuthGuard } from '../auth/auth.guard';

const routes: Routes = [
  {
    path: '', // Base path for the chat module (e.g., /chat)
    component: ChatMainComponent,
    canActivate: [AuthGuard],
  },
  {
    path: 'room/:id', // Path for a specific room (e.g., /chat/room/123)
    component: ChatRoomComponent,
    canActivate: [AuthGuard],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class ChatRoutingModule {}
