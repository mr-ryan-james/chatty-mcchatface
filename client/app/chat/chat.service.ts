import { Injectable } from 'angular2/core';
import { Http, Response, Headers, RequestOptions } from 'angular2/http';
import { Observable }       from 'rxjs/Observable';
import { CONFIG } from '../config.ts';

let chatUrl = CONFIG.baseUrls.chat;

export class Chat {
  id: string;
  userIds: string[];
}

@Injectable()
export class ChatService  {
  constructor(private _http: Http) { }
  
}