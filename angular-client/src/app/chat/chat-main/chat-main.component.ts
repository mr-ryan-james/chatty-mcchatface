import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatListComponent } from '../chat-list/chat-list.component';
import { ChatCreateComponent } from '../chat-create/chat-create.component';

@Component({
  selector: 'app-chat-main',
  standalone: true,
  templateUrl: './chat-main.component.html',
  styleUrl: './chat-main.component.css',
  imports: [CommonModule, ChatListComponent, ChatCreateComponent],
})
export class ChatMainComponent {}
