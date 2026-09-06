import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LiveSyncService } from './core/services/live-sync.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('taxipay-ui');
  // Initialize real-time SignalR connection lifecycle across the app
  private liveSync = inject(LiveSyncService);
}

