import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService, UserDto } from '../../shared/services/user.service';
import {
  ChatService,
  CreateChatroomDto,
} from '../../shared/services/chat.service';
import { Subscription } from 'rxjs';
import { AuthService } from '../../shared/services/auth.service';
import { SignalrService } from '../../shared/services/signalr.service';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-chat-create',
  templateUrl: './chat-create.component.html',
  styleUrls: ['./chat-create.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule, SharedModule],
})
export class ChatCreateComponent implements OnInit, OnDestroy {
  users: UserDto[] = [];
  selectedUsers: UserDto[] = [];
  loading = false;
  creating = false;
  error = '';
  userFilter: string = '';
  chatroomName = '';
  private subscriptions: Subscription[] = [];

  constructor(
    private router: Router,
    private userService: UserService,
    private chatService: ChatService,
    private authService: AuthService,
    private signalrService: SignalrService
  ) {}

  ngOnInit(): void {
    this.fetchUsers();

    // Ensure SignalR is connected
    if (!this.signalrService.isConnectedToHub()) {
      this.signalrService.startConnection().catch((err) => {
        console.error('Failed to establish SignalR connection:', err);
        this.error =
          'Could not connect to chat server. Some features may not work properly.';
      });
    }
  }

  ngOnDestroy(): void {
    // Clean up subscriptions to prevent memory leaks
    this.subscriptions.forEach((sub) => sub.unsubscribe());
  }

  fetchUsers(): void {
    this.loading = true;
    this.error = '';

    const sub = this.userService.getOtherUsers().subscribe({
      next: (users) => {
        console.log('Fetched users:', users);
        this.users = users;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching users:', err);
        this.error = 'Failed to load users. Please try again.';
        this.loading = false;
      },
    });

    this.subscriptions.push(sub);
  }

  addUser(user: UserDto): void {
    // Prevent adding duplicates
    if (!this.selectedUsers.some((u) => u.id === user.id)) {
      this.selectedUsers.push(user);
    }
  }

  removeUser(user: UserDto): void {
    // Remove from selected users
    this.selectedUsers = this.selectedUsers.filter((u) => u.id !== user.id);
  }

  isSelected(user: UserDto): boolean {
    return this.selectedUsers.some((u) => u.id === user.id);
  }

  toggleUserSelection(user: UserDto): void {
    if (this.isSelected(user)) {
      this.removeUser(user);
    } else {
      this.addUser(user);
    }
  }

  getFilteredUsers(): UserDto[] {
    if (!this.userFilter) {
      return this.users;
    }
    const filter = this.userFilter.toLowerCase();
    return this.users.filter(
      (user) =>
        user.firstName.toLowerCase().includes(filter) ||
        user.lastName.toLowerCase().includes(filter) ||
        user.email.toLowerCase().includes(filter)
    );
  }

  createChat(): void {
    if (this.selectedUsers.length === 0 || this.creating) {
      // Added || this.creating
      if (this.selectedUsers.length === 0) {
        this.error = 'Please select at least one user to chat with';
      }
      return;
    }

    this.creating = true;
    this.error = '';
    console.log('Creating chat with users:', this.selectedUsers);

    // Include the current user in the chat
    const currentUser = this.authService.getUserInfo();
    if (!currentUser) {
      this.error = 'You must be logged in to create a chat';
      this.creating = false;
      return;
    }

    // Create a name for the chat (if not provided)
    const chatName =
      this.chatroomName ||
      this.selectedUsers.map((u) => `${u.firstName} ${u.lastName}`).join(', ');

    // Create the new chatroom DTO
    const createChatroomDto: CreateChatroomDto = {
      name: chatName,
      userIds: [currentUser.id, ...this.selectedUsers.map((u) => u.id)],
    };

    const sub = this.chatService.createChatroom(createChatroomDto).subscribe({
      next: (chatroom) => {
        console.log('Chat room created:', chatroom);
        this.creating = false;

        // Join the room via SignalR
        if (this.signalrService.isConnectedToHub()) {
          this.signalrService
            .joinRoom(chatroom.id)
            .then(() => {
              this.router.navigate(['/chat/room', chatroom.id]);
            })
            .catch((err) => {
              console.error('Failed to join room:', err);
              // Still navigate, but show an error
              this.error =
                'Room created but failed to connect. Please try again.';
              this.router.navigate(['/chat/room', chatroom.id]);
            });
        } else {
          // Just navigate if SignalR not connected
          this.router.navigate(['/chat/room', chatroom.id]);
        }
      },
      error: (err) => {
        console.error('Error creating chatroom:', err);
        this.error = 'Failed to create chat room. Please try again.';
        this.creating = false;
      },
    });

    this.subscriptions.push(sub);
  }
}
