import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
import { NavbarComponent } from '../navbar/navbar.component';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent, SidebarComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private router = inject(Router);
  public currentUrl = signal<string>(this.router.url);

  public isAuthRoute = computed(() => {
    const rawPath = this.currentUrl().split('?')[0];
    const path = rawPath === '' ? '/' : (rawPath.replace(/\/$/, '') || '/');
    const authRoutes = ['/', '/login', '/auth/login', '/register', '/auth/register', '/admin/login'];
    return authRoutes.includes(path);
  });

  public showNavbar = computed(() => !this.isAuthRoute());
  public showSidebar = computed(() => !this.isAuthRoute());

  constructor() {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: unknown) => {
        const navEnd = event as NavigationEnd;
        this.currentUrl.set(navEnd.urlAfterRedirects || navEnd.url);
      });
  }
}
