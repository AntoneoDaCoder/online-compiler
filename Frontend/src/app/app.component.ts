import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { CommonModule, Location } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from './core/services/auth.service';
import { ApiService } from './core/services/api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  imports: [RouterOutlet, CommonModule]
})
export class AppComponent implements OnInit {
  private destroyRef = inject(DestroyRef);

  constructor(
    private router: Router,
    private auth: AuthService,
    private api: ApiService,
    private location: Location
  ) { }

  async ngOnInit(): Promise<void> {
    await this.auth.init();

    if (this.auth.isLoggedIn()) {
      this.syncLocalAccount();
    }
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