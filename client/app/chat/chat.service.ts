import { Injectable } from 'angular2/core';
import { Http, Response, Headers, RequestOptions } from 'angular2/http';
import { Observable } from 'rxjs/Observable';
import { CONFIG } from '../config.ts';
import {AuthService} from '../auth.service.ts';
import {User} from '../user/user.service.ts';

let chatroomUrl = CONFIG.baseUrls.chatroom;

export class Chat {
  text: String;
  user: User;
  date: Date;
}

export class Chatroom {
  _id: String;
  title: String;
  users: User[];
  chats: Chat[];
  date: Date;
  lastReads: LastRead[];
}

export class LastRead{
  userId: String;
  lastReadDate: Date;
}

@Injectable()
export class ChatService {
  constructor(
    private _http: Http,
    private _authService: AuthService
    ) { }

  createChatroom(users: User[]): Observable<Chatroom>{
    let body = JSON.stringify(users);
    let headers = new Headers({ 
      'Content-Type': 'application/json',
      'x-access-token': this._authService.getToken()
    });
    let options = new RequestOptions({ headers: headers });

    return this._http.post(chatroomUrl, body, options)
      .map((res: Response) => this.processChatResponse.apply(this, [res]))
      .catch(this.handleError);
  }
  
  getChatroom(id: String):Observable<Chatroom>{
    let url = `${chatroomUrl}/${id}`;

    let headers = new Headers({ 'x-access-token': this._authService.getToken() });
    let options = new RequestOptions({ headers: headers });

    return this._http.get(url, options)
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
    let chatroom = new Chatroom();

    chatroom._id = body._id;
    chatroom.title = body.title;
    chatroom.chats = body.lastName;
    chatroom.date = body.date;
    chatroom.lastReads = body.lastReads;
    chatroom.users = body.users;
    
    return chatroom;
  }

  private handleError(error: any) {
    // In a real world app, we might use a remote logging infrastructure
    let errMsg = error.message || (error.json().message);
    console.error(errMsg); // log to console instead
    return Observable.throw(errMsg);
  }

}