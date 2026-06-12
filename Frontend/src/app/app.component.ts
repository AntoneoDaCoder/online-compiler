import { Component, OnInit, DestroyRef, inject, OnDestroy } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { CommonModule, Location } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { ApiService } from './core/services/api.service';
import { ToastComponent } from './core/toast.component';
import { SignalrService } from './core/services/signalr.service';
import { ToastService } from './core/services/toast.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  imports: [RouterOutlet, CommonModule, ToastComponent],
})
export class AppComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  private rolesSubScription = new Subscription();

  constructor(
    private router: Router,
    private auth: AuthService,
    private api: ApiService,
    private location: Location,
    private signalr: SignalrService,
    private toast: ToastService
  ) { }

  async ngOnInit(): Promise<void> {
    await this.auth.init();

    if (this.auth.isLoggedIn()) {
      this.syncLocalAccount();

      this.rolesSubScription.add(
        this.signalr.onUserRolesChanged.subscribe(data => {
          const added = data.rolesToAdd ?? [];
          const removed = data.rolesToRemove ?? [];

          if (added.length === 0 && removed.length === 0) {
            return;
          }

          const parts: string[] = [];

          if (added.length > 0) {
            parts.push(`Добавлены роли: ${added.join(', ')}`);
          }

          if (removed.length > 0) {
            parts.push(`Удалены роли: ${removed.join(', ')}`);
          }

          const message = parts.join('\n');

          this.toast.show(
            message,
            'info',
            5000,
            'Обновление прав'
          );
        })
      );
    }
  }

  ngOnDestroy() {
    this.rolesSubScription.unsubscribe();
  }

  private syncLocalAccount(): void {
    const userId = this.auth.getId();
    if (!userId) return;

    this.api.syncExternalAccount(userId).subscribe({
      error: (err) => console.error(err),
    });
  }

  showBack(): boolean {
    const url = this.router.url || '/';
    return !(url === '/' || url.startsWith('/login') || url.startsWith('/tasks'));
  }

  logout(): void {
    void this.auth.logout();
  }

  goBack(): void {
    this.location.back();
  }
}