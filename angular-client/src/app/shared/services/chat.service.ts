import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
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

  constructor(private http: HttpClient, private authService: AuthService) {}

  // Get all chatrooms
  getChatrooms(): Observable<ChatroomDto[]> {
    console.log('Fetching chatrooms');
    return this.http
      .get<ChatroomDto[]>(
        `${environment.apiUrl}/chatroom`,
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
        `${environment.apiUrl}/chatroom/${id}`,
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
        `${environment.apiUrl}/chatroom`,
        createDto,
        this.getAuthHeaders()
      )
      .pipe(catchError(this.handleError));
  }

  // Send a message to a chatroom
  sendMessage(
    roomId: string,
    messageDto: CreateMessageDto
  ): Observable<ChatMessageDto> {
    console.log(`Sending message to chatroom ${roomId}:`, messageDto);
    return this.http
      .post<ChatMessageDto>(
        `${environment.apiUrl}/chatroom/${roomId}/chats`,
        messageDto,
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
        `${environment.apiUrl}/chatroom/${id}`,
        updateDto,
        this.getAuthHeaders()
      )
      .pipe(catchError(this.handleError));
  }

  // Delete a chatroom
  deleteChatroom(id: string): Observable<any> {
    console.log(`Deleting chatroom ${id}`);
    return this.http
      .delete(`${environment.apiUrl}/chatroom/${id}`, this.getAuthHeaders())
      .pipe(catchError(this.handleError));
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
