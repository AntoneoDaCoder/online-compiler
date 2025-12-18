import { Component, AfterViewInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../environment';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


declare global { interface Window { google: any; } }
declare const google: any;


@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss'],
    standalone: true,
    imports: [CommonModule, FormsModule]
})
export class LoginComponent implements AfterViewInit {
    email = '';
    password = '';


    name = '';
    regEmail = '';
    regPassword = '';


    showRegister = false;
    loading = false;


    constructor(private auth: AuthService) { }


    ngAfterViewInit(): void {
        // Инициализируем GSI
        if (window && (window as any).google && environment.googleClientId) {
            google.accounts.id.initialize({
                client_id: environment.googleClientId,
                callback: this.handleCredentialResponse.bind(this)
            });


            // Рендер стандартной кнопки в контейнере
            const container = document.getElementById('g_id_signin');
            if (container) {
                google.accounts.id.renderButton(container, { theme: 'outline', size: 'large' });
            }
            
        } else {
            console.warn('GSI not loaded or googleClientId not set');
        }
    }


    private handleCredentialResponse(response: any) {
        const idToken = response?.credential;
        if (!idToken) {
            console.error('No credential from Google');
            return;
        }


        this.loading = true;
        this.auth.externalLogin(idToken).subscribe({
            next: () => { this.loading = false; /* успешная логика: редирект/обновление UI */ },
            error: () => { this.loading = false; /* обработка ошибки */ }
        });
    }


    // Если хотите кастомную кнопку, вызывайте prompt(), GSI покажет диалог
    onGoogleCustomClick() {
        if (window && (window as any).google) {
            google.accounts.id.prompt();
        } else {
            console.error('Google Identity library not loaded');
        }
    }


    onLogin() {
        if (!this.email || !this.password) { return; }
        this.loading = true;
        this.auth.login(this.email, this.password).subscribe({
            next: () => { this.loading = false; },
            error: () => { this.loading = false; }
        });
    }


    onRegisterToggle() {
        this.showRegister = !this.showRegister;
        if (this.showRegister) {
            this.regEmail = this.email;
        } else {
            this.name = '';
            this.regEmail = '';
            this.regPassword = '';
        }
    }


    onRegisterSubmit() {
        if (!this.name || !this.regEmail || !this.regPassword) { return; }
        this.loading = true;
        this.auth.register(this.regEmail, this.regPassword, this.name).subscribe({
            next: () => { this.loading = false; this.showRegister = false; },
            error: () => { this.loading = false; }
        });
    }


    onCancelRegister() { this.onRegisterToggle(); }
}