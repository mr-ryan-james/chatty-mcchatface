import { bootstrap } from 'angular2/platform/browser';
import { AppComponent } from './app.component.ts';

bootstrap(AppComponent, [])
  .then(success => console.log(`Bootstrap success`))
  .catch(error => console.log(error));
