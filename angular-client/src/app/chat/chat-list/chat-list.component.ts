import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ChatService, ChatroomDto } from '../../shared/services/chat.service';
import { SignalrService } from '../../shared/services/signalr.service';
import { Subscription } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-chat-list',
  templateUrl: './chat-list.component.html',
  styleUrls: ['./chat-list.component.css'],
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
})
export class ChatListComponent implements OnInit, OnDestroy {
  chatrooms: ChatroomDto[] = [];
  loading = false;
  error = '';
  private subscriptions: Subscription[] = [];

  constructor(
    private router: Router,
    private chatService: ChatService,
    private signalrService: SignalrService
  ) {}

  ngOnInit(): void {
    // Fetch chatrooms
    this.fetchChatrooms();

    // Set up SignalR connection
    this.signalrService
      .startConnection()
      .then(() => {
        console.log('SignalR connection established in chat-list component');
        this.setupSignalRListeners();
      })
      .catch((err) => {
        console.error('Failed to establish SignalR connection:', err);
        this.error =
          'Could not connect to chat server. Please try again later.';
      });
  }

  ngOnDestroy(): void {
    // Clean up subscriptions to prevent memory leaks
    this.subscriptions.forEach((sub) => sub.unsubscribe());
  }

  fetchChatrooms(): void {
    this.loading = true;
    this.error = '';

    const sub = this.chatService.getChatrooms().subscribe({
      next: (chatrooms) => {
        console.log('Fetched chatrooms:', chatrooms);
        this.chatrooms = chatrooms;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching chatrooms:', err);
        this.error = 'Failed to load chatrooms. Please try again.';
        this.loading = false;
      },
    });

    this.subscriptions.push(sub);
  }

  setupSignalRListeners(): void {
    console.log('Setting up SignalR listeners');

    // Listen for new chatrooms
    const newChatroomSub = this.signalrService.newChatroom$.subscribe(
      (chatroom) => {
        console.log('New chatroom received:', chatroom);
        // Check if we already have this chatroom
        const existingIndex = this.chatrooms.findIndex(
          (c) => c.id === chatroom.id
        );
        if (existingIndex === -1) {
          this.chatrooms = [chatroom, ...this.chatrooms];
        } else {
          // Update existing chatroom
          this.chatrooms[existingIndex] = chatroom;
        }
      }
    );

    this.subscriptions.push(newChatroomSub);

    // Listen for connection state changes
    const connectionSub = this.signalrService.connectionState$.subscribe(
      (connected) => {
        if (!connected) {
          this.error = 'Connection to chat server lost. Trying to reconnect...';
        } else {
          this.error = '';
        }
      }
    );

    this.subscriptions.push(connectionSub);
  }

  enterChatroom(chatroom: ChatroomDto): void {
    console.log('Entering chatroom:', chatroom.id);

    // First join the room via SignalR
    if (this.signalrService.isConnectedToHub()) {
      this.signalrService
        .joinRoom(chatroom.id)
        .then(() => {
          // Then navigate to the room
          this.router.navigate(['/chat/room', chatroom.id]);
        })
        .catch((err) => {
          console.error('Failed to join room:', err);
          this.error = 'Failed to join chat room. Please try again.';
        });
    } else {
      // If not connected to SignalR, just navigate (connection will be handled in the room component)
      this.router.navigate(['/chat/room', chatroom.id]);
    }
  }
}
