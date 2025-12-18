import { Component } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { CommonModule, Location } from '@angular/common';
import { AuthService } from './core/services/auth.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    imports: [RouterOutlet, CommonModule]
})
export class AppComponent {
    constructor(private router: Router, private auth: AuthService, private location: Location) { }

    showBack(): boolean {
        const url = this.router.url || '/';
        // don't show back on login or main tasks page
        return !(url === '/' || url.startsWith('/login') || url.startsWith('/tasks'));
    }

    logout() {
        this.auth.logout();
    }

    goBack() {
        this.location.back();
    }
}
