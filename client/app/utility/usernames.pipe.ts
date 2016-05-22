import { Pipe, PipeTransform } from '@angular/core';
import {User} from '../user/user.service.ts';

@Pipe({name: 'userNames'})
export class UserNamesPipe implements PipeTransform {
  transform(users: User[]): String {
   
   
   return users.map(user=> `${user.firstName} ${user.lastName}`).join(', ');
  }
}