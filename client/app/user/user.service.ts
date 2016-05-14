import { Injectable } from 'angular2/core';
import { Http, Response } from 'angular2/http';

import { CONFIG } from '../config';

let userUrl = CONFIG.baseUrls.user;

export class User {
  id: string;
  firstName: string;
  email: string;
}

@Injectable()
export class UserService {
  constructor(private _http: Http) { }

  getUsers() {
    return this._http.get(userUrl)
      .map((response: Response) => <User[]>response.json().data);
  }

  getUser(id: number) {
    return this._http.get(`${userUrl}/${id}`)
      .map((response: Response) => <User>response.json().data);
  }
}
