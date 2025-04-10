import { Pipe, PipeTransform } from '@angular/core';
import { UserDto } from '../services/chat.service'; // Import UserDto

@Pipe({
  name: 'userNames',
  standalone: true, // Make pipe standalone as it's used in standalone components
})
export class UserNamesPipe implements PipeTransform {
  transform(users: UserDto[] | null | undefined): string {
    // Use UserDto
    if (!users || users.length === 0) {
      return '';
    }

    // Handle potentially undefined names
    return users
      .map((user) => `${user.firstName || ''} ${user.lastName || ''}`.trim())
      .join(', ');
  }
}
