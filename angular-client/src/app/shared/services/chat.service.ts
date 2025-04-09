import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, throwError, BehaviorSubject } from 'rxjs';
import { delay, map, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { User } from './user.service';

// Define interfaces matching .NET DTOs
export interface ChatMessageDto {
  id: string;
  text: string;
  date: Date;
  userId: string;
  userName?: string;
  user?: User;
  chatroomId: string;
}

export interface ChatroomDto {
  id: string;
  name: string;
  created: Date;
  userIds: string[];
  users?: User[];
  lastActivity?: Date;
}

export interface ChatroomDetailDto extends ChatroomDto {
  chats: ChatMessageDto[];
}

export interface CreateChatroomDto {
  name: string;
  userIds: string[];
}

export interface UpdateChatroomDto {
  name: string;
  userIds: string[];
}

export interface CreateMessageDto {
  text: string;
  userId: string;
  userName?: string;
}

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  // Mock data for development (fallback if API is not available)
  private mockChatrooms: ChatroomDto[] = [
    {
      id: '1',
      name: 'General Chat',
      created: new Date(),
      userIds: ['1', '2'],
      users: [
        { id: '1', firstName: 'John', lastName: 'Doe' },
        { id: '2', firstName: 'Jane', lastName: 'Smith' },
      ],
      lastActivity: new Date(),
    },
    {
      id: '2',
      name: 'Project Discussion',
      created: new Date(),
      userIds: ['1', '3'],
      users: [
        { id: '1', firstName: 'John', lastName: 'Doe' },
        { id: '3', firstName: 'Alice', lastName: 'Johnson' },
      ],
      lastActivity: new Date(),
    },
  ];

  private mockChatroomDetails: { [key: string]: ChatroomDetailDto } = {
    '1': {
      ...this.mockChatrooms[0],
      chats: [
        {
          id: '1',
          text: 'Hello there!',
          date: new Date(),
          userId: '1',
          user: { id: '1', firstName: 'John', lastName: 'Doe' },
          chatroomId: '1',
        },
        {
          id: '2',
          text: 'Hi John!',
          date: new Date(),
          userId: '2',
          user: { id: '2', firstName: 'Jane', lastName: 'Smith' },
          chatroomId: '1',
        },
      ],
    },
    '2': {
      ...this.mockChatrooms[1],
      chats: [
        {
          id: '3',
          text: 'How is the project going?',
          date: new Date(),
          userId: '3',
          user: { id: '3', firstName: 'Alice', lastName: 'Johnson' },
          chatroomId: '2',
        },
      ],
    },
  };

  // Message storage - holds messages by chatroom ID
  private messagesByRoom: {
    [roomId: string]: BehaviorSubject<ChatMessageDto[]>;
  } = {};

  constructor(private http: HttpClient, private authService: AuthService) {}

  // Get all chatrooms
  getChatrooms(): Observable<ChatroomDto[]> {
    console.log('Fetching chatrooms');
    return this.http
      .get<ChatroomDto[]>(
        `${environment.apiUrl}/chatrooms`,
        this.getAuthHeaders()
      )
      .pipe(
        catchError((error) => {
          console.warn('API error, falling back to mock data', error);
          return of(this.mockChatrooms);
        })
      );
  }

  // Get a specific chatroom by ID
  getChatroom(id: string): Observable<ChatroomDetailDto> {
    console.log(`Fetching chatroom with ID: ${id}`);
    return this.http
      .get<ChatroomDetailDto>(
        `${environment.apiUrl}/chatrooms/${id}`,
        this.getAuthHeaders()
      )
      .pipe(
        catchError((error) => {
          console.warn('API error, falling back to mock data', error);
          const chatroom = this.mockChatroomDetails[id];
          if (chatroom) {
            return of(chatroom);
          }
          return throwError(
            () => new Error(`Chatroom with ID ${id} not found`)
          );
        })
      );
  }

  // Create a new chatroom
  createChatroom(createDto: CreateChatroomDto): Observable<ChatroomDto> {
    console.log('Creating new chatroom:', createDto);
    return this.http
      .post<ChatroomDto>(
        `${environment.apiUrl}/chatrooms`,
        createDto,
        this.getAuthHeaders()
      )
      .pipe(catchError(this.handleError));
  }

  // Send a message to a chatroom
  sendMessage(
    roomId: string,
    messageDto: ChatMessageDto
  ): Observable<ChatMessageDto> {
    console.log(`Sending message to chatroom ${roomId}:`, messageDto);
    return this.http
      .post<ChatMessageDto>(
        `${environment.apiUrl}/chatrooms/${roomId}/chats`,
        messageDto,
        this.getAuthHeaders()
      )
      .pipe(catchError(this.handleError));
  }

  // Get chatroom persona details
  getChatroomPersona(chatroomId: string): Observable<User> {
    console.log(`Fetching persona for chatroom with ID: ${chatroomId}`);
    return this.http
      .get<User>(
        `${environment.apiUrl}/chatrooms/${chatroomId}/persona`,
        this.getAuthHeaders()
      )
      .pipe(catchError(this.handleError));
  }

  // Get chatroom messages including those from the persona
  getChatroomMessagesWithPersona(
    chatroomId: string
  ): Observable<ChatMessageDto[]> {
    console.log(
      `Fetching messages with persona for chatroom with ID: ${chatroomId}`
    );
    return this.http
      .get<ChatMessageDto[]>(
        `${environment.apiUrl}/chatrooms/${chatroomId}/messagesWithPersona`,
        this.getAuthHeaders()
      )
      .pipe(catchError(this.handleError));
  }

  // Update a chatroom
  updateChatroom(
    id: string,
    updateDto: UpdateChatroomDto
  ): Observable<ChatroomDto> {
    console.log(`Updating chatroom ${id}:`, updateDto);
    return this.http
      .put<ChatroomDto>(
        `${environment.apiUrl}/chatrooms/${id}`,
        updateDto,
        this.getAuthHeaders()
      )
      .pipe(catchError(this.handleError));
  }

  // Delete a chatroom
  deleteChatroom(id: string): Observable<any> {
    console.log(`Deleting chatroom ${id}`);
    return this.http
      .delete(`${environment.apiUrl}/chatrooms/${id}`, this.getAuthHeaders())
      .pipe(catchError(this.handleError));
  }

  // Get messages observable for a specific chatroom
  getChatroomMessages$(
    chatroomId: string,
    includePersona: boolean = false
  ): Observable<ChatMessageDto[]> {
    if (!this.messagesByRoom[chatroomId]) {
      // Initialize with an empty array if this is the first request
      this.messagesByRoom[chatroomId] = new BehaviorSubject<ChatMessageDto[]>(
        []
      );

      // Load initial messages (with or without persona messages)
      if (includePersona) {
        this.loadMessagesWithPersona(chatroomId);
      } else {
        this.loadStandardMessages(chatroomId);
      }
    }

    return this.messagesByRoom[chatroomId].asObservable();
  }

  // Load standard messages (without persona)
  private loadStandardMessages(chatroomId: string): void {
    this.getChatroom(chatroomId).subscribe((room) => {
      if (room && room.chats) {
        this.messagesByRoom[chatroomId].next(room.chats);
      }
    });
  }

  // Load messages including persona messages
  private loadMessagesWithPersona(chatroomId: string): void {
    this.getChatroomMessagesWithPersona(chatroomId).subscribe((messages) => {
      if (messages) {
        this.messagesByRoom[chatroomId].next(messages);
      }
    });
  }

  // Connect to SignalR hub and set up message handling
  connect(signalrService: any): void {
    // Subscribe to the newMessage$ observable from the SignalR service
    signalrService.newMessage$.subscribe((message: ChatMessageDto) => {
      this.handleNewMessage(message);
    });
  }

  // Handle new incoming messages (both user and persona messages)
  private handleNewMessage(message: ChatMessageDto): void {
    console.log('Handling new message:', message);

    if (!message || !message.chatroomId) {
      console.error('Received invalid message:', message);
      return;
    }

    const chatroomId = message.chatroomId;

    // Initialize the BehaviorSubject if it doesn't exist for this room
    if (!this.messagesByRoom[chatroomId]) {
      this.messagesByRoom[chatroomId] = new BehaviorSubject<ChatMessageDto[]>(
        []
      );
    }

    // Get current messages and add the new one
    const currentMessages = this.messagesByRoom[chatroomId].getValue();
    const updatedMessages = [...currentMessages, message];

    // Update the BehaviorSubject with the new messages array
    this.messagesByRoom[chatroomId].next(updatedMessages);
  }

  // Helper method for auth headers
  private getAuthHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: `Bearer ${this.authService.getToken()}`,
      }),
    };
  }

  private handleError(error: any) {
    console.error('API error:', error);
    let errorMessage = 'An unknown error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else if (error.status) {
      // Server-side error
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }

    return throwError(() => new Error(errorMessage));
  }
}
