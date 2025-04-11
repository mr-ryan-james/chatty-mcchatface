import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../shared/services/user.service';
import { UserDto } from '../../shared/services/user.service';
import { PersonaInfo } from '../../shared/services/chat.service';
import {
  ChatService,
  CreateChatroomDto,
} from '../../shared/services/chat.service';
import { Subscription, forkJoin } from 'rxjs';
import { AuthService } from '../../shared/services/auth.service';
import { SignalrService } from '../../shared/services/signalr.service';
import { SharedModule } from '../../shared/shared.module';

// SelectableParticipant type
export interface SelectableParticipant {
  participantId: string | number;
  displayName: string;
  isPersona: boolean;
  originalData: UserDto | PersonaInfo;
}

@Component({
  selector: 'app-chat-create',
  templateUrl: './chat-create.component.html',
  styleUrls: ['./chat-create.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule, SharedModule],
})
export class ChatCreateComponent implements OnInit, OnDestroy {
  participants: SelectableParticipant[] = [];
  selectedParticipants: SelectableParticipant[] = [];
  personas: PersonaInfo[] = [];
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
    this.loadData();

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

  // Load both users and personas, then combine into participants
  loadData(): void {
    this.loading = true;
    this.error = '';

    const sub = forkJoin({
      users: this.userService.getOtherUsers(),
      personas: this.chatService.getPersonas(),
    }).subscribe({
      next: ({ users, personas }) => {
        this.personas = personas;
        const mappedUsers: SelectableParticipant[] = users.map((u) => ({
          participantId: u.id,
          displayName: `${u.firstName} ${u.lastName}`,
          isPersona: false,
          originalData: u,
        }));
        const mappedPersonas: SelectableParticipant[] = personas.map((p) => ({
          participantId: p.id,
          displayName: p.displayName,
          isPersona: true,
          originalData: p,
        }));
        this.participants = [...mappedUsers, ...mappedPersonas];
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching participants:', err);
        this.error = 'Failed to load participants. Please try again.';
        this.loading = false;
      },
    });

    this.subscriptions.push(sub);
  }

  addParticipant(participant: SelectableParticipant): void {
    if (
      !this.selectedParticipants.some(
        (p) => p.participantId === participant.participantId
      )
    ) {
      this.selectedParticipants.push(participant);
    }
  }

  removeParticipant(participant: SelectableParticipant): void {
    this.selectedParticipants = this.selectedParticipants.filter(
      (p) => p.participantId !== participant.participantId
    );
  }

  isParticipantSelected(participant: SelectableParticipant): boolean {
    return this.selectedParticipants.some(
      (p) => p.participantId === participant.participantId
    );
  }

  toggleParticipantSelection(participant: SelectableParticipant): void {
    if (this.isParticipantSelected(participant)) {
      this.removeParticipant(participant);
    } else {
      // Only one persona can be selected at a time
      const hasPersona = this.selectedParticipants.some((p) => p.isPersona);
      if (participant.isPersona && hasPersona) {
        // Optionally show user feedback
        console.warn('Only one persona can be selected.');
        return;
      }
      this.addParticipant(participant);
    }
  }

  getFilteredParticipants(): SelectableParticipant[] {
    if (!this.userFilter) {
      return this.participants;
    }
    const filter = this.userFilter.toLowerCase();
    return this.participants.filter((p) =>
      p.displayName.toLowerCase().includes(filter)
    );
  }

  createChat(): void {
    if (this.selectedParticipants.length === 0 || this.creating) {
      // Added || this.creating
      if (this.selectedParticipants.length === 0) {
        this.error = 'Please select at least one participant to chat with';
      }
      return;
    }

    this.creating = true;
    this.error = '';
    console.log('Creating chat with participants:', this.selectedParticipants);

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
      this.selectedParticipants.map((p) => p.displayName).join(', ');

    // Separate selected users and persona
    const realUserSelections = this.selectedParticipants.filter(
      (p) => !p.isPersona
    );
    const personaSelection = this.selectedParticipants.find((p) => p.isPersona);

    // Prepare user IDs (convert string IDs to numbers)
    const userIds = [
      +(currentUser?.id || 0), // Add current user ID as number
      ...realUserSelections.map((p) => +p.participantId), // Add selected real user IDs as numbers
    ];

    // Prepare persona ID (already a number or undefined)
    const personaUserId = personaSelection
      ? (personaSelection.participantId as number)
      : null;

    const createChatroomDto: CreateChatroomDto = {
      title: chatName,
      userIds: userIds,
      personaUserId: personaUserId,
    };

    const sub = this.chatService.createChatroom(createChatroomDto).subscribe({
      next: (chatroom) => {
        console.log('Chat room created:', chatroom);
        this.creating = false;

        // Join the room via SignalR
        if (this.signalrService.isConnectedToHub()) {
          this.signalrService
            .joinRoom(chatroom.id.toString())
            .then(() => {
              this.router.navigate(['/chat/room', chatroom.id.toString()]);
            })
            .catch((err) => {
              console.error('Failed to join room:', err);
              // Still navigate, but show an error
              this.error =
                'Room created but failed to connect. Please try again.';
              this.router.navigate(['/chat/room', chatroom.id.toString()]);
            });
        } else {
          // Just navigate if SignalR not connected
          this.router.navigate(['/chat/room', chatroom.id.toString()]);
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
