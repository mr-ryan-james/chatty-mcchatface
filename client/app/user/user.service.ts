import { Injectable } from '@angular/core';
import {BaseService} from '../base.service.ts';
import { Http, Response, Headers, RequestOptions } from '@angular/http';
import { Observable }       from 'rxjs/Observable';
import { CONFIG } from '../config.ts';
import { Subject }    from 'rxjs/Subject';
import { AuthService } from '../auth.service.ts';

let userUrl = CONFIG.baseUrls.user;

export class User {
  _id: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

@Injectable()
export class UserService extends BaseService {
  
  private userNowKnown = new Subject<User>();
  private userNowUnKnown = new Subject<User>();
  nowWeSeeYou$ = this.userNowKnown.asObservable();
  nowWeDont$ = this.userNowUnKnown.asObservable();
  
  constructor(
    private _http: Http, 
    private _authService: AuthService
    ) { 
      super();
    }


  registerUser(user: User) {
    return this.processUserAuthRequest(user, "post")
  }

  loginUser(user: User) {
    return this.processUserAuthRequest(user, "put");
  }
  
  logoutUser(){
    this._authService.deleteUserAndToken();
    this.userNowUnKnown.next(null);
  }

  private processUserAuthRequest(user: User, httpMethod: string) {
    let body = JSON.stringify(user);
    let headers = new Headers({ 'Content-Type': 'application/json' });
    let options = new RequestOptions({ headers: headers });

    return this._http[httpMethod](userUrl, body, options)
      .map((res: Response) => this.processUserResponse.apply(this, [res]))
      .catch(function(error:String){
        return Observable.throw(error);
      });
  }

  getUsers(): Observable<User[]> {
    let headers = new Headers({ 'x-access-token': this._authService.getToken() });
    let options = new RequestOptions({ headers: headers });

    return this._http.get(userUrl, options)
      .map((response: Response) => {
        return <User[]>response.json();
      })
      .catch((error:any, caught: Observable<User[]>) => {
        return Observable.throw(error);
      });
  }

  getUser(id: number): Observable<User> {
    return this._http.get(`${userUrl}/${id}`)
      .map((response: Response) => <User>response.json().data)
      .catch((error:any, caught: Observable<User>) => {
        return Observable.throw(error);
      });
  }
 

  private processUserResponse(res: Response) {
    if (res.status < 200 || res.status >= 300) {
      throw new Error('Response status: ' + res.status);
    }

    let json = res.json();
    let body = res.json().user;

    let user = this.extractUser(body);
    this._authService.setToken(json.token);
    this._authService.setUserInfo(user);
    this.userNowKnown.next(user)

    return user;
  }

  private extractUser(body: any) : User {
    let user = new User();

    user._id = body._id;
    user.firstName = body.firstName;
    user.lastName = body.lastName;
    user.email = body.email;
    return user;
  }


}
