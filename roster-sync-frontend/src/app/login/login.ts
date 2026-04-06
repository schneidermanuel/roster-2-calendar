import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.html',
})
export class Login implements OnInit {
  readonly #route = inject(ActivatedRoute);
  readonly #router = inject(Router);

  ngOnInit() {
    const token = this.#route.snapshot.queryParamMap.get('token');

    if (token) {
      localStorage.setItem('auth_token', token);
    }
      this.#router.navigate(['/']);
  }
}
