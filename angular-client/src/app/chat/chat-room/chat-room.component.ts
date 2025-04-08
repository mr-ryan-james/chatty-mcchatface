import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
  AfterViewChecked,
  OnDestroy,
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import {
  ChatService,
  ChatroomDetailDto,
  ChatMessageDto,
  CreateMessageDto,
} from '../../shared/services/chat.service';
import { User } from '../../shared/services/user.service';
import { SignalrService } from '../../shared/services/signalr.service';
import { AuthService } from '../../shared/services/auth.service';
import { Subscription } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-chat-room',
  templateUrl: './chat-room.component.html',
  styleUrls: ['./chat-room.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SharedModule],
})
export class ChatRoomComponent implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('chatContainer') private chatContainer!: ElementRef;

  roomId: string = '';
  chatroom: ChatroomDetailDto | null = null;
  chats: ChatMessageDto[] = [];
  usersInRoom: User[] = [];
  text: string = '';
  shouldScrollToBottom: boolean = false;
  loading: boolean = false;
  sending: boolean = false;
  error: string = '';
  private subscriptions: Subscription[] = [];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private chatService: ChatService,
    private signalrService: SignalrService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Get room ID from route params
    const paramSub = this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.roomId = id;
        this.loadChatRoom();
        this.setupSignalRConnection();
      } else {
        console.error('No room ID provided');
        this.router.navigate(['/chat']);
      }
    });

    this.subscriptions.push(paramSub);
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  ngOnDestroy(): void {
    // Clean up subscriptions
    this.subscriptions.forEach((sub) => sub.unsubscribe());

    // Leave the chat room in SignalR
    if (this.signalrService.isConnectedToHub() && this.roomId) {
      this.signalrService
        .leaveRoom(this.roomId)
        .catch((err) => console.error('Error leaving room:', err));
    }
  }

  loadChatRoom(): void {
    console.log(`Loading chat room ${this.roomId}`);
    this.loading = true;
    this.error = '';

    const roomSub = this.chatService.getChatroom(this.roomId).subscribe({
      next: (chatroom) => {
        console.log('Loaded chatroom:', chatroom);
        this.chatroom = chatroom;
        this.chats = chatroom.chats || [];
        this.usersInRoom = chatroom.users || [];
        this.loading = false;
        this.shouldScrollToBottom = true;
      },
      error: (err) => {
        console.error('Error loading chatroom:', err);
        this.error = 'Failed to load chat room. Please try again.';
        this.loading = false;
      },
    });

    this.subscriptions.push(roomSub);
  }

  setupSignalRConnection(): void {
    console.log('Setting up SignalR connection for room:', this.roomId);

    // If not connected, connect to SignalR hub
    if (!this.signalrService.isConnectedToHub()) {
      this.signalrService
        .startConnection()
        .then(() => {
          console.log('SignalR connection established');
          this.joinRoom();
        })
        .catch((err) => {
          console.error('Error connecting to SignalR:', err);
          this.error =
            'Failed to connect to chat server. Real-time messaging may not work.';
        });
    } else {
      // If already connected, just join the room
      this.joinRoom();
    }

    // Listen for new messages
    const messageSub = this.signalrService.newMessage$.subscribe((message) => {
      console.log('New message received via SignalR:', message);

      // Only add if it's for our current room and not already in the list
      if (
        message.chatroomId === this.roomId &&
        !this.chats.some((m) => m.id === message.id)
      ) {
        this.chats.push(message);
        this.shouldScrollToBottom = true;
      }
    });

    this.subscriptions.push(messageSub);

    // Listen for connection state changes
    const connectionSub = this.signalrService.connectionState$.subscribe(
      (connected) => {
        if (!connected) {
          this.error = 'Connection to chat server lost. Trying to reconnect...';
        } else if (
          this.error ===
          'Connection to chat server lost. Trying to reconnect...'
        ) {
          this.error = '';
          // If we were in a room before reconnection, rejoin it
          this.joinRoom();
        }
      }
    );

    this.subscriptions.push(connectionSub);
  }

  joinRoom(): void {
    if (this.signalrService.isConnectedToHub() && this.roomId) {
      this.signalrService
        .joinRoom(this.roomId)
        .then(() => console.log(`Joined room ${this.roomId} via SignalR`))
        .catch((err) => {
          console.error('Error joining room via SignalR:', err);
          this.error =
            'Failed to join chat room. Please try refreshing the page.';
        });
    }
  }

  sendChat(): void {
    if (!this.text.trim() || this.sending) {
      return;
    }

    const currentUser = this.authService.getUserInfo();
    if (!currentUser) {
      this.error = 'You must be logged in to send messages';
      return;
    }

    this.sending = true;
    console.log(`Sending message to room ${this.roomId}:`, this.text);

    const messageDto: CreateMessageDto = {
      text: this.text.trim(),
      userId: currentUser.id,
    };

    // Send via HTTP API first
    const sendSub = this.chatService
      .sendMessage(this.roomId, messageDto)
      .subscribe({
        next: (message) => {
          console.log('Message sent successfully:', message);

          // Add to our local list if it's not already there (might be added by SignalR already)
          if (!this.chats.some((m) => m.id === message.id)) {
            this.chats.push(message);
            this.shouldScrollToBottom = true;
          }

          this.text = '';
          this.sending = false;
        },
        error: (err) => {
          console.error('Error sending message:', err);
          this.error = 'Failed to send message. Please try again.';
          this.sending = false;
        },
      });

    this.subscriptions.push(sendSub);

    // Also send via SignalR for real-time delivery
    if (this.signalrService.isConnectedToHub()) {
      this.signalrService
        .sendMessage(this.roomId, this.text.trim())
        .catch((err) =>
          console.error('Error sending message via SignalR:', err)
        );
    }
  }

  exitChat(): void {
    console.log('Exiting chat room');

    // Leave the room in SignalR
    if (this.signalrService.isConnectedToHub() && this.roomId) {
      this.signalrService
        .leaveRoom(this.roomId)
        .catch((err) => console.error('Error leaving room:', err));
    }

    this.router.navigate(['/chat']);
  }

  private scrollToBottom(): void {
    try {
      this.chatContainer.nativeElement.scrollTop =
        this.chatContainer.nativeElement.scrollHeight;
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }
}
