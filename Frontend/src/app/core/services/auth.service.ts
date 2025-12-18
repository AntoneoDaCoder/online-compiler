import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environment';
import { LoginDataDto } from '../models/dtos';
import { tap } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private tokenKey = 'access_token'; // оставлено для совместимости имён, но не используется

    // in-memory storage (per-tab, lives while page is open)
    private _accessToken: string | null = null;
    private _userId: string = '';
    private _userName: string = '';
    private _userRoles: string[] = [];

    private loginSubject = new Subject<void>();
    private logoutSubject = new Subject<void>();

    public onLogin$ = this.loginSubject.asObservable();
    public onLogout$ = this.logoutSubject.asObservable();

    constructor(
        private http: HttpClient,
        private router: Router
    ) { }

    login(email: string, password: string) {
        return this.http.post<LoginDataDto>(`${environment.apiBaseUrl}/auth/login/internal`, { email, password }).pipe(
            tap(dto => this.handleLogin(dto))
        );
    }

    register(email: string, password: string, name: string) {
        return this.http.post<LoginDataDto>(`${environment.apiBaseUrl}/auth/register`, { email, password, name }).pipe(
            tap(dto => this.handleLogin(dto))
        );
    }

    handleLogin(dto: LoginDataDto) {
        // save in-memory (per-tab)
        this._accessToken = dto.accessToken ?? null;
        this._userName = dto.name ?? '';
        this._userId = dto.userId ?? '';
        this._userRoles = dto.roles ? [...dto.roles] : [];

        this.loginSubject.next();

        // navigate to main page
        this.router.navigate(['/tasks']);
    }

    logout() {
        // clear in-memory
        this._accessToken = null;
        this._userName = '';
        this._userId = '';
        this._userRoles = [];

        // close signalr connection
        this.logoutSubject.next();

        this.router.navigate(['/login']);
    }

    getToken(): string | null {
        return this._accessToken;
    }

    getId(): string {
        return this._userId ?? '';
    }

    getName(): string {
        return this._userName ?? '';
    }

    getRoles(): string[] {
        return Array.isArray(this._userRoles) ? [...this._userRoles] : [];
    }

    isLoggedIn(): boolean {
        return !!this.getToken();
    }

    externalLogin(idToken: string) {
        return this.http.post<LoginDataDto>(`${environment.apiBaseUrl}/auth/login/external`, { idToken }).pipe(
            tap(dto => this.handleLogin(dto))
        );
    }
}
