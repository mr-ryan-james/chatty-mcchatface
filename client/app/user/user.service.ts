import { Injectable } from 'angular2/core';
import { Http, Response, Headers, RequestOptions } from 'angular2/http';
import { Observable }       from 'rxjs/Observable';
import { CONFIG } from '../config.ts';

import { AuthService } from '../auth.service.ts';

let userUrl = CONFIG.baseUrls.user;

export class User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

@Injectable()
export class UserService {
  constructor(
    private _http: Http, 
    private _authService: AuthService
    ) { 
      var a = 1;
    }


  registerUser(user: User) {
    return this.processUserRequest(user, "post")
  }

  loginUser(user: User) {
    return this.processUserRequest(user, "put");
  }

  private processUserRequest(user: User, httpMethod: string) {
    let body = JSON.stringify(user);
    let headers = new Headers({ 'Content-Type': 'application/json' });
    let options = new RequestOptions({ headers: headers });

    return this._http[httpMethod](userUrl, body, options)
      .map((res: Response) => this.processUserResponse.apply(this, [res]))
      .catch(this.handleError);
  }

  getUsers(): Observable<User[]> {
    let headers = new Headers({ 'x-access-token': this._authService.getToken() });
    let options = new RequestOptions({ headers: headers });

    return this._http.get(userUrl, options)
      .map((response: Response) => {
        return <User[]>response.json()
      });
  }

  getUser(id: number): Observable<User> {
    return this._http.get(`${userUrl}/${id}`)
      .map((response: Response) => <User>response.json().data);
  }

  private processUserResponse(res: Response) {
    if (res.status < 200 || res.status >= 300) {
      throw new Error('Response status: ' + res.status);
    }

    let json = res.json();
    let body = res.json().user;

    this._authService.setToken(json.token);

    return this.extractUser(body);
  }

  private extractUser(body: any) {
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
