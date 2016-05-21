import { Injectable } from '@angular/core';
import { Router } from '@angular/router-deprecated';
import { Http, Response, Headers, RequestOptions } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { CONFIG } from '../config.ts';
import {AuthService} from '../auth.service.ts';
import {User} from '../user/user.service.ts';
import {BaseService} from '../base.service.ts';

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

export class LastRead {
  userId: String;
  lastReadDate: Date;
}

@Injectable()
export class ChatService extends BaseService {
  constructor(
    private _http: Http,
    private _authService: AuthService,
    private _router: Router
  ) {
    super();
  }

  createChatroom(users: User[]): Observable<Chatroom> {
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

  sendChat(text: String, id: String): Observable<Chat> {
    let url = `${chatroomUrl}/${id}`;

    let body = JSON.stringify({
      text: text,
      id: id
    });
    let headers = new Headers({
      'Content-Type': 'application/json',
      'x-access-token': this._authService.getToken()
    });
    let options = new RequestOptions({ headers: headers });

    return this._http.put(url, body, options)
      .map((res: Response) => <Chat>res.json())
      .catch(this.handleError);

  }

  getChatroom(id: String): Observable<Chatroom> {
    let url = `${chatroomUrl}/${id}`;

    let headers = new Headers({ 'x-access-token': this._authService.getToken() });
    let options = new RequestOptions({ headers: headers });

    return this._http.get(url, options)
      .map((res: Response) => this.processChatResponse.apply(this, [res]))
      .catch(this.handleError);
  }
  
  getChatroomsForUser() : Observable<Chatroom[]>{
    let headers = new Headers({ 'x-access-token': this._authService.getToken() });
    let options = new RequestOptions({ headers: headers });
    
    return this._http.get(chatroomUrl, options)
      .map((res: Response) => <Chatroom[]>res.json())
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
    chatroom.chats = body.chats;
    chatroom.date = body.date;
    chatroom.lastReads = body.lastReads;
    chatroom.users = body.users;

    return chatroom;
  }



}