import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environment';
import { LoginDataDto } from '../models/dtos';
import { tap } from 'rxjs/operators';


@Injectable({ providedIn: 'root' })
export class AuthService {
    private tokenKey = 'access_token';


    constructor(private http: HttpClient, private router: Router) { }


    login(email: string, password: string) {
        return this.http.post<LoginDataDto>(`${environment.apiBaseUrl}/auth/login`, { email, password }).pipe(
            tap(dto => this.handleLogin(dto))
        );
    }


    register(email: string, password: string, name: string) {
        return this.http.post<LoginDataDto>(`${environment.apiBaseUrl}/auth/register`, { email, password, name }).pipe(
            tap(dto => this.handleLogin(dto))
        );
    }


    handleLogin(dto: LoginDataDto) {
        localStorage.setItem(this.tokenKey, dto.accessToken);
        // optionally store user info
        localStorage.setItem('user_name', dto.name);
        localStorage.setItem('user_id', dto.userId);
        localStorage.setItem('user_roles', JSON.stringify(dto.roles || []));
        // navigate to main page
        this.router.navigate(['/tasks']);
    }


    logout() {
        localStorage.removeItem(this.tokenKey);
        localStorage.removeItem('user_name');
        localStorage.removeItem('user_id');
        localStorage.removeItem('user_roles');
        this.router.navigate(['/login']);
    }


    getToken(): string | null {
        return localStorage.getItem(this.tokenKey);
    }


    getRoles(): string[] {
        try { return JSON.parse(localStorage.getItem('user_roles') || '[]'); } catch { return []; }
    }


    isLoggedIn(): boolean {
        return !!this.getToken();
    }
}