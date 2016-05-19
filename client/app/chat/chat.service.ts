import { Injectable } from 'angular2/core';
import { Http, Response, Headers, RequestOptions } from 'angular2/http';
import { Observable }       from 'rxjs/Observable';
import { CONFIG } from '../config.ts';
import {User} from '../user/user.service.ts';

let chatUrl = CONFIG.baseUrls.chat;

export class Chat {
  text: String;
  user: User;
  date: Date;
}

export class ChatRoom {
  id: String;
  title: String;
  users: User[];
}

@Injectable()
export class ChatService {
  constructor(private _http: Http) { }

  createChat(users: User[]) {

    let body = JSON.stringify(users);
    let headers = new Headers({ 'Content-Type': 'application/json' });
    let options = new RequestOptions({ headers: headers });

    return this._http.post(chatUrl, body, options)
      .map((res: Response) => this.processChatResponse.apply(this, [res]))
      .catch(this.handleError);

  }

  private processChatResponse(res: Response) {
    if (res.status < 200 || res.status >= 300) {
      throw new Error('Response status: ' + res.status);
    }

    let json = res.json();

    let chatroom = this.extractChatroom(json);
    

    return chatroom;
  }

  private extractChatroom(body: any) {
    let user = new User();

    user.id = body._id;
    user.firstName = body.firstName;
    user.lastName = body.lastName;
    user.email = body.email;
    return user;
  }

  private handleError(error: any) {
    // In a real world app, we might use a remote logging infrastructure
    let errMsg = error.message || (error.json().message);
    console.error(errMsg); // log to console instead
    return Observable.throw(errMsg);
  }

}