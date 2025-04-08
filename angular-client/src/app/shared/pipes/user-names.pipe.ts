import { Pipe, PipeTransform } from '@angular/core';

interface User {
  firstName: string;
  lastName: string;
}

@Pipe({
  name: 'userNames',
  standalone: false,
})
export class UserNamesPipe implements PipeTransform {
  transform(users: User[] | null | undefined): string {
    if (!users || users.length === 0) {
      return '';
    }

    return users.map((user) => `${user.firstName} ${user.lastName}`).join(', ');
  }
}
