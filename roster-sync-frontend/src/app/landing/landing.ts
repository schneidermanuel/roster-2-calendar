import { Component, computed, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { environment } from '../../environment/environment';

@Component({
  selector: 'app-landing',
  imports: [RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing implements OnInit {
  protected isLoggedIn = computed(() => {
    return !!this.token();
  });
  protected token = signal<string | null>(null);
  ngOnInit(): void {
    const sToken = localStorage.getItem('auth_token');
    if (sToken) {
      this.token.set(sToken);
    }
  }

  loginWithGoogle(): void {
    window.location.href = `${environment.apiUrl}/auth/google/login`;
  }
  logout(): void {
    localStorage.removeItem('auth_token');
    this.token.set(null);
  }
}
