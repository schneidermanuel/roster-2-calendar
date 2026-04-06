import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { environment } from '../../environment/environment';
import { RosterSyncApiService } from '../api';

@Component({
  selector: 'app-landing',
  imports: [RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing implements OnInit {
  readonly #service = inject(RosterSyncApiService);
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
    this.#service.googleLogin().subscribe(url=> window.location.href = url);
  }
  logout(): void {
    localStorage.removeItem('auth_token');
    this.token.set(null);
  }
}
