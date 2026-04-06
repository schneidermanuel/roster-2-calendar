import {
  Component,
  inject,
  model,
  ModelSignal,
  OnInit,
  signal,
  WritableSignal,
} from '@angular/core';
import { Router } from '@angular/router';
import { CalendarDto, RosterSyncApiService, SyncConfigDto } from '../api';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-user',
  imports: [FormsModule],
  templateUrl: './user.html',
  styleUrl: './user.scss',
})
export class User implements OnInit {
  readonly #router = inject(Router);
  readonly #service = inject(RosterSyncApiService);

  protected syncs: WritableSignal<SyncConfigDto[]> = signal([]);
  protected calendars: WritableSignal<CalendarDto[]> = signal([]);
  protected showAddForm = signal(false);

  protected newSyncCalendarId: ModelSignal<string> = model('');
  protected newSyncRosterUrl: ModelSignal<string> = model('');

  ngOnInit() {
    const token = localStorage.getItem('auth_token');

    if (!token) {
      this.#router.navigate(['/']);
    }

    this.#service.getCalendars().subscribe({
      next: (calendars) => this.calendars.set(calendars),
      error: (err) => console.error('Error:', err),
    });

    this.#service.getSyncs().subscribe({
      next: (syncs) => this.syncs.set(syncs),
      error: (err) => console.error('Error:', err),
    });
  }
  toggleAddForm() {
    this.showAddForm.set(!this.showAddForm());
    this.newSyncCalendarId.set('');
    this.newSyncRosterUrl.set('');
  }
  addSync() {
    const calendarName = this.calendars().filter((c) => c.id === this.newSyncCalendarId())[0]?.name;
    this.#service
      .createSync({
        googleCalendarId: this.newSyncCalendarId(),
        rosterUrl: this.newSyncRosterUrl(),
        calendarName: calendarName,
      })
      .subscribe({
        next: (sync) => this.syncs.update((syncs) => [...syncs, sync]),
        error: (err) => console.error('Error:', err),
      });
    this.showAddForm.set(false);
  }

  deleteSync(id: number) {
    this.#service.deleteSync(id).subscribe({
      next: () => this.syncs.update((syncs) => syncs.filter((s) => s.id !== id)),
    });
  }

  openRoster(url: string) {
    window.open(url, '_blank');
  }

  logout() {
    localStorage.removeItem('auth_token');
    this.#router.navigate(['/']);
  }

  formatLastSync(lastSync: string | null): string {
    if (!lastSync) return 'Never synced';
    const date = new Date(lastSync);
    const now = new Date();
    const isToday = date.toDateString() === now.toDateString();
    const time = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return isToday
      ? `Last synced today at ${time}`
      : `Last synced ${date.toLocaleDateString()} at ${time}`;
  }
}
