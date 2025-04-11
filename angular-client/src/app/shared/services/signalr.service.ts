import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { ChatMessageDto, ChatroomDto } from './chat.service';
import { User } from './user.service';

@Injectable({
  providedIn: 'root',
})
export class SignalrService {
  // SignalR hub connection
  private hubConnection!: signalR.HubConnection;

  // Subjects for emitting real-time events
  private newMessageSubject = new Subject<ChatMessageDto>();
  private newChatroomSubject = new Subject<ChatroomDto>();
  private userJoinedSubject = new Subject<{ roomId: string; user: User }>();
  private userLeftSubject = new Subject<{ roomId: string; user: User }>();
  private connectionStateSubject = new Subject<boolean>();

  // Observable streams that components can subscribe to
  public newMessage$ = this.newMessageSubject.asObservable();
  public newChatroom$ = this.newChatroomSubject.asObservable();
  public userJoined$ = this.userJoinedSubject.asObservable();
  public userLeft$ = this.userLeftSubject.asObservable();
  public connectionState$ = this.connectionStateSubject.asObservable();

  // Track connection state
  private isConnected = false;
  private activeRoomId: string | null = null;

  constructor(private authService: AuthService) {}

  // Initialize connection to SignalR hub
  startConnection(): Promise<void> {
    console.log('Starting SignalR connection...');

    // Create a new hub connection
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.signalrUrl, {
        accessTokenFactory: () => this.authService.getToken() || '',
      })
      .withAutomaticReconnect()
      .build();

    // Set up event handlers
    this.registerSignalRHandlers();

    // Start the connection
    return this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR connection established');
        this.isConnected = true;
        this.connectionStateSubject.next(true);
      })
      .catch((err) => {
        console.error('Error starting SignalR connection:', err);
        this.isConnected = false;
        this.connectionStateSubject.next(false);
        throw err;
      });
  }

  // Disconnect from SignalR hub
  stopConnection(): Promise<void> {
    console.log('Stopping SignalR connection...');

    if (!this.hubConnection) {
      console.warn('No active SignalR connection to stop');
      return Promise.resolve();
    }

    return this.hubConnection
      .stop()
      .then(() => {
        console.log('SignalR connection stopped');
        this.isConnected = false;
        this.activeRoomId = null;
        this.connectionStateSubject.next(false);
      })
      .catch((err) => {
        console.error('Error stopping SignalR connection:', err);
        throw err;
      });
  }

  // Join a chat room
  joinRoom(roomId: string): Promise<void> {
    if (!this.isConnected) {
      console.error('Cannot join room: SignalR not connected');
      return Promise.reject('Not connected');
    }

    console.log(`Joining room: ${roomId}`);
    return this.hubConnection
      .invoke('JoinRoom', +roomId) // Convert string to number
      .then(() => {
        this.activeRoomId = roomId;
        console.log(`Joined room ${roomId}`);
      })
      .catch((err) => {
        console.error(`Error joining room ${roomId}:`, err);
        throw err;
      });
  }

  // Leave a chat room
  leaveRoom(roomId: string): Promise<void> {
    if (!this.isConnected) {
      console.error('Cannot leave room: SignalR not connected');
      return Promise.reject('Not connected');
    }

    console.log(`Leaving room: ${roomId}`);
    return this.hubConnection
      .invoke('LeaveRoom', roomId)
      .then(() => {
        if (this.activeRoomId === roomId) {
          this.activeRoomId = null;
        }
        console.log(`Left room ${roomId}`);
      })
      .catch((err) => {
        console.error(`Error leaving room ${roomId}:`, err);
        throw err;
      });
  }

  // Send a message to a chat room
  sendMessage(roomId: string, text: string): Promise<void> {
    if (!this.isConnected) {
      console.error('Cannot send message: SignalR not connected');
      return Promise.reject('Not connected');
    }

    console.log(`Sending message to room ${roomId}: ${text}`);
    return this.hubConnection
      .invoke('SendMessage', roomId, text)
      .catch((err) => {
        console.error(`Error sending message to room ${roomId}:`, err);
        throw err;
      });
  }

  // Register event handlers for SignalR hub
  private registerSignalRHandlers(): void {
    // Handler for receiving new messages
    this.hubConnection.on(
      'ReceiveMessage',
      (chatroomId: string, message: ChatMessageDto) => {
        console.log(`Received message for room ${chatroomId}:`, message);
        this.newMessageSubject.next(message);
      }
    );

    // Handler for new chatrooms created
    this.hubConnection.on('NewChatroom', (chatroom: ChatroomDto) => {
      console.log('New chatroom created:', chatroom);
      this.newChatroomSubject.next(chatroom);
    });

    // Handler for user joining a room
    this.hubConnection.on('UserJoined', (roomId: string, user: User) => {
      console.log(`User joined room ${roomId}:`, user);
      this.userJoinedSubject.next({ roomId, user });
    });

    // Handler for user leaving a room
    this.hubConnection.on('UserLeft', (roomId: string, user: User) => {
      console.log(`User left room ${roomId}:`, user);
      this.userLeftSubject.next({ roomId, user });
    });

    // Connection state handlers
    this.hubConnection.onreconnecting((error) => {
      console.log('SignalR connection lost, attempting to reconnect...', error);
      this.isConnected = false;
      this.connectionStateSubject.next(false);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log(
        'SignalR connection reestablished. ConnectionId:',
        connectionId
      );
      this.isConnected = true;
      this.connectionStateSubject.next(true);

      // If we were in a room before reconnection, rejoin it
      if (this.activeRoomId) {
        this.joinRoom(this.activeRoomId).catch((err) =>
          console.error(
            `Failed to rejoin room ${this.activeRoomId} after reconnection:`,
            err
          )
        );
      }
    });

    this.hubConnection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      this.isConnected = false;
      this.connectionStateSubject.next(false);
    });
  }

  // Check connection status
  isConnectedToHub(): boolean {
    return this.isConnected;
  }

  // Get the active room ID
  getActiveRoomId(): string | null {
    return this.activeRoomId;
  }
}
