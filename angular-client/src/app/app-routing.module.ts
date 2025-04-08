import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: 'user',
    loadChildren: () => import('./user/user.module').then((m) => m.UserModule),
  },
  {
    path: 'chat',
    loadChildren: () => import('./chat/chat.module').then((m) => m.ChatModule),
  },
  { path: '', redirectTo: '/chat', pathMatch: 'full' },
  { path: 'chat', loadChildren: () => import('./chat/chat.module').then(m => m.ChatModule) }, // Default route
  { path: '**', redirectTo: '/chat' }, // Wildcard route
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
