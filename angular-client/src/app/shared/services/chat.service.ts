import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, throwError, BehaviorSubject } from 'rxjs';
import { delay, map, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { User } from './user.service';

export interface PersonaConfig {
  personaUserId: number;
  displayName: string;
  systemPrompt: string;
  preferredModelId: string;
}

// Define interfaces matching .NET DTOs
export interface ChatMessageDto {
  id: number;
  text: string;
  date: Date;
  userId: number;
  chatroomId: number;
  userFirstName?: string;
  userLastName?: string;
  role?: MessageRole;
}

export interface ChatroomDto {
  id: number;
  title: string;
  date: Date;
  userIds: number[];
  users?: UserDto[];
  chatCount: number;
}

export interface ChatroomDetailDto extends ChatroomDto {
  personaUserId?: string;
  personaConfig?: PersonaConfig | null;
  messages: ChatMessageDto[];
}

export interface CreateChatroomDto {
  title: string;
  userIds: number[];
  personaUserId?: string;
}

export interface UpdateChatroomDto {
  title: string;
  addUserIds?: number[];
  removeUserIds?: number[];
}

export interface CreateMessageDto {
  text: string;
  userId: number;
  chatroomId: number;
}
export interface UserDto {
  id: number;
  firstName?: string;
  lastName?: string;
  email?: string;
  createdAt: Date;
}

export enum MessageRole {
  User = 'user',
  Assistant = 'assistant',
}

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  getPersonas(): Observable<PersonaConfig[]> {
    return this.http.get<PersonaConfig[]>(`${environment.apiUrl}/personas`);
  }
  // Mock data for development (fallback if API is not available)
  private mockChatrooms: ChatroomDto[] = [
    {
      id: 1,
      title: 'General Chat',
      date: new Date(),
      userIds: [1, 2],
      users: [
        {
          id: 1,
          firstName: 'John',
          lastName: 'Doe',
          createdAt: new Date(),
          email: 'john@example.com',
        },
        {
          id: 2,
          firstName: 'Jane',
          lastName: 'Smith',
          createdAt: new Date(),
          email: 'jane@example.com',
        },
      ],
      chatCount: 2,
    },
    {
      id: 2,
      title: 'Project Discussion',
      date: new Date(),
      userIds: [1, 3],
      users: [
        {
          id: 1,
          firstName: 'John',
          lastName: 'Doe',
          createdAt: new Date(),
          email: 'john@example.com',
        },
        {
          id: 3,
          firstName: 'Alice',
          lastName: 'Johnson',
          createdAt: new Date(),
          email: 'alice@example.com',
        },
      ],
      chatCount: 1,
    },
  ];

  private mockChatroomDetails: { [key: string]: ChatroomDetailDto } = {
    '1': {
      ...this.mockChatrooms[0],
      messages: [
        {
          id: 1,
          text: 'Hello there!',
          date: new Date(),
          userId: 1,
          chatroomId: 1,
          userFirstName: 'John',
          userLastName: 'Doe',
          role: MessageRole.User,
        },
        {
          id: 2,
          text: 'Hi John!',
          date: new Date(),
          userId: 2,
          chatroomId: 1,
          userFirstName: 'Jane',
          userLastName: 'Smith',
          role: MessageRole.User,
        },
      ],
    },
    '2': {
      ...this.mockChatrooms[1],
      messages: [
        {
          id: 3,
          text: 'How is the project going?',
          date: new Date(),
          userId: 3,
          chatroomId: 2,
          userFirstName: 'Alice',
          userLastName: 'Johnson',
          role: MessageRole.User,
        },
      ],
    },
  };

  // Message storage - holds messages by chatroom ID
  private messagesByRoom: {
    [roomId: number]: BehaviorSubject<ChatMessageDto[]>;
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
  getChatroom(id: number): Observable<ChatroomDetailDto> {
    console.log(`Fetching chatroom with ID: ${id}`);
    return this.http
      .get<ChatroomDetailDto>(
        `${environment.apiUrl}/chatrooms/${id}`,
        this.getAuthHeaders()
      )
      .pipe(
        catchError((error) => {
          console.warn('API error, falling back to mock data', error);
          const chatroom = this.mockChatroomDetails[id.toString()];
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
    roomId: number,
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

  // Update a chatroom
  updateChatroom(
    id: number,
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
  deleteChatroom(id: number): Observable<any> {
    console.log(`Deleting chatroom ${id}`);
    return this.http
      .delete(`${environment.apiUrl}/chatrooms/${id}`, this.getAuthHeaders())
      .pipe(catchError(this.handleError));
  }

  // Get messages observable for a specific chatroom
  getChatroomMessages$(
    chatroomId: number,
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
  private loadStandardMessages(chatroomId: number): void {
    this.getChatroom(chatroomId).subscribe((room) => {
      if (room && room.messages) {
        this.messagesByRoom[chatroomId].next(room.messages);
      }
    });
  }

  // Load messages including persona messages
  private loadMessagesWithPersona(chatroomId: number): void {
    this.getChatroom(chatroomId).subscribe((room) => {
      if (room && room.messages) {
        this.messagesByRoom[chatroomId].next(room.messages);
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

    if (
      !message ||
      message.chatroomId === undefined ||
      message.chatroomId === null
    ) {
      console.error('Received invalid message:', message);
      return;
    }

    const chatroomId = message.chatroomId;

    if (!this.messagesByRoom[chatroomId]) {
      this.messagesByRoom[chatroomId] = new BehaviorSubject<ChatMessageDto[]>(
        []
      );
    }

    const currentMessages = this.messagesByRoom[chatroomId].getValue();
    const updatedMessages = [...currentMessages, message];

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
