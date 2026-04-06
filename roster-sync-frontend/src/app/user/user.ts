import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-user',
  imports: [],
  templateUrl: './user.html',
  styleUrl: './user.scss',
})
export class User implements OnInit {
  readonly #router = inject(Router);

  ngOnInit() {
    const token = localStorage.getItem('auth_token');

    if (!token) {
      this.#router.navigate(['/']);
    }
  }
}
