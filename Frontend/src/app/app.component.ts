import { Component, OnInit } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { CommonModule, Location } from '@angular/common';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  imports: [RouterOutlet, CommonModule]
})
export class AppComponent implements OnInit {
  constructor(
    private router: Router,
    private auth: AuthService,
    private location: Location
  ) {}

  ngOnInit(): void {
    void this.auth.init();
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