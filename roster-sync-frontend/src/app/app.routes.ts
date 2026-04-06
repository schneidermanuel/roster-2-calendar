import { Routes } from '@angular/router';
import { Landing } from './landing/landing';
import { Privacy } from './privacy/privacy';
import { Login } from './login/login';
import { User } from './user/user';

export const routes: Routes = [
  { path: '', component: Landing },
  { path: 'privacy', component: Privacy },
  { path: 'login', component: Login },
  { path: 'user', component: User },
];
