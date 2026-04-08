import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
    loading = false;

    constructor(private auth: AuthService) { }

    onLogin(): void {
        this.loading = true;
        this.auth.login().catch((error) => {
            console.error(error);
            this.loading = false;
        });
    }

    onRegister(): void {
        this.loading = true;
        this.auth.register().catch((error) => {
            console.error(error);
            this.loading = false;
        });
    }
}