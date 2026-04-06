import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { APIS, RosterSyncApiService } from '../api';

@Component({
  selector: 'app-user',
  imports: [],
  templateUrl: './user.html',
  styleUrl: './user.scss',
})
export class User implements OnInit {
  readonly #router = inject(Router);
  readonly calendarService = inject(RosterSyncApiService);

  ngOnInit() {
    const token = localStorage.getItem('auth_token');

    if (!token) {
      this.#router.navigate(['/']);
    }

    this.calendarService.getCalendars().subscribe({
      next: (calendars) => console.log('Calendars:', calendars),
      error: (err) => console.error('Error:', err),
    });
  }
}
