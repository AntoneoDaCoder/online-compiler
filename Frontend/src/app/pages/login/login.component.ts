import { Component } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../app/environment';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


@Component({ selector: 'app-login', templateUrl: './login.component.html', imports: [CommonModule, FormsModule] })
export class LoginComponent {
    email = '';
    password = '';


    constructor(private auth: AuthService) { }


    onLogin() {
        this.auth.login(this.email, this.password).subscribe({
            next: () => { },
            error: () => { /* останемся на странице, можно показать ошибку */ }
        });
    }


    onRegister() {
        // навигируем в зарегистрироваться либо открываем inline форму
    }


    onGoogleLogin() {
        // перенаправляем на серверный endpoint, который сделает редирект в Google
        window.location.href = `${environment.googleAuthUrl}?callback=${encodeURIComponent(environment.googleCallbackUrl)}`;
    }
}