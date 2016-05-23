import { Router } from '@angular/router-deprecated';
import { Observable }       from 'rxjs/Observable';
import { Subject }    from 'rxjs/Subject';

export class BaseService {

  private userNoLongerAuthenticated = new Subject<any>();
  noLongerAuthenticated$ = this.userNoLongerAuthenticated.asObservable();

    constructor( ){
        
        }

  protected handleError(error: any) {
    if(error.status === 401){
      this.userNoLongerAuthenticated.next(error);
      return;
    }
    
    // In a real world app, we might use a remote logging infrastructure
    let errMsg = error.message || (error.json().message);
    console.error(errMsg); // log to console instead
    return Observable.throw(errMsg);
  }

}