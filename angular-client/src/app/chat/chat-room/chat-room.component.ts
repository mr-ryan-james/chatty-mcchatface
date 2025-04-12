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
import { PersonaInfo } from '../../shared/services/chat.service';
import { UserDto } from '../../shared/services/chat.service';
import { SignalrService } from '../../shared/services/signalr.service';
import { AuthService } from '../../shared/services/auth.service';
import { Subscription } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { ParseAiMessagePipe } from '../../shared/pipes/parse-ai-message.pipe';

@Component({
  selector: 'app-chat-room',
  templateUrl: './chat-room.component.html',
  styleUrls: ['./chat-room.component.css'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    SharedModule,
    ParseAiMessagePipe,
  ],
})
export class ChatRoomComponent implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('chatContainer') private chatContainer!: ElementRef;

  roomId: string = '';
  chatroom: ChatroomDetailDto | null = null;
  chats: ChatMessageDto[] = [];
  usersInRoom: UserDto[] = [];
  text: string = '';
  shouldScrollToBottom: boolean = false;
  loading: boolean = false;
  sending: boolean = false;
  error: string = '';

  // Persona information
  personaId: string = '';
  personaName: string = '';
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
    console.log('ChatRoomComponent ngOnInit - Version 3 (In-place parsing)'); // Version Log
    const paramSub = this.route.paramMap.subscribe((params) => {
      console.log('Inside ngOnInit - Route params:', params);
      const id = params.get('id');
      if (id) {
        console.log('Room ID from route:', id);
        this.roomId = id;

        this.setupSignalRConnection();

        // Subscribe to the ChatService's message observable for real-time updates
        const messagesSub = this.chatService
          .getChatroomMessages$(+this.roomId, true)
          .subscribe((messages: ChatMessageDto[]) => {
            console.log(
              'Initial messages loaded (raw):',
              JSON.stringify(messages)
            ); // Log raw messages
            // Assign the raw messages first
            this.chats = messages;
            console.log(
              'Assigned this.chats (before parsing):',
              JSON.stringify(this.chats)
            ); // Log state before parsing
            this.shouldScrollToBottom = true;
          });

        this.subscriptions.push(messagesSub);
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

    const roomSub = this.chatService.getChatroom(+this.roomId).subscribe({
      next: (chatroom) => {
        console.log('Loaded chatroom:', chatroom);
        this.chatroom = chatroom;
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
        message.chatroomId === +this.roomId &&
        !this.chats.some((m) => m.id === message.id)
      ) {
        // The pipe will handle parsing in the template
        // No need to process here anymore
        this.chats.push(message); // Push the raw message
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
        .then(() => {
          console.log(`Joined room ${this.roomId} via SignalR`);
          this.loadChatRoom(); // Ensure this line is present
        })
        .catch((err) => {
          console.error('Error joining room via SignalR:', err);
          this.error =
            'Failed to join chat room. Please try refreshing the page.';
        });
    }
  }

  sendChat(): void {
    // Check if the message is empty or whitespace
    if (!this.text || this.text.trim() === '') {
      return; // Don't send empty messages
    }

    const currentUser = this.authService.getUserInfo();
    if (!currentUser || !currentUser.id) {
      console.error('Cannot send message, user not logged in or ID missing.');
      return;
    }
    const messageDto: ChatMessageDto = {
      chatroomId: +this.roomId,
      userId: +currentUser.id,
      text: this.text,
      date: new Date(),
      id: 0,
      userFirstName: currentUser.firstName,
      userLastName: currentUser.lastName,
    };

    this.chatService.sendMessage(+this.roomId, messageDto).subscribe(() => {
      this.text = '';
    });
  }

  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault(); // Prevent default behavior (e.g., adding a new line)
      this.sendChat(); // Send the message
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

  get personaUserId(): string | null {
    return this.chatroom?.personaUserId ?? null;
  }

  get personaConfig(): PersonaInfo | null {
    return this.chatroom?.personaConfig ?? null;
  }
}
