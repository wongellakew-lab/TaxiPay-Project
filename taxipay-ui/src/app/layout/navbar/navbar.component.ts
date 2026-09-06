import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { NotificationCenterComponent } from '../../shared/components/notification-center/notification-center.component';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    NotificationCenterComponent
  ],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  public auth = inject(AuthService);
  private router = inject(Router);
  public currentUrl = signal<string>(this.router.url);

  public isAuthPage = computed(() => {
    const rawPath = this.currentUrl().split('?')[0];
    const path = rawPath.replace(/\/$/, '') || '/';
    return ['/login', '/auth/login', '/register', '/auth/register', '/admin/login'].includes(path);
  });

  constructor() {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: unknown) => {
        const navEnd = event as NavigationEnd;
        this.currentUrl.set(navEnd.urlAfterRedirects || navEnd.url);
      });
  }
}
