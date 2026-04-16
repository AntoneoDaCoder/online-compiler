import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import Keycloak from 'keycloak-js';
import { environment } from '../../environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private keycloak: Keycloak = new Keycloak({
        url: environment.keycloakUrl,
        realm: environment.keycloakRealm,
        clientId: environment.keycloakClientId,
    });

    private initialized = false;
    private initPromise?: Promise<boolean>;

    private _accessToken: string | null = null;
    private _userId = '';
    private _userName = '';
    private _userRoles: string[] = [];

    private loginSubject = new Subject<void>();
    private logoutSubject = new Subject<void>();

    public onLogin$ = this.loginSubject.asObservable();
    public onLogout$ = this.logoutSubject.asObservable();

    constructor(private router: Router) { }

    async init(): Promise<boolean> {
        if (this.initialized) {
            return this.isLoggedIn();
        }

        if (!this.initPromise) {
            this.bindEvents();

            this.initPromise = this.keycloak
                .init({
                    onLoad: 'check-sso',
                    pkceMethod: 'S256',
                    checkLoginIframe: false,
                    silentCheckSsoRedirectUri: `${window.location.origin}/assets/silent-check-sso.html`,
                    silentCheckSsoFallback: false,
                    responseMode: 'query',
                })
                .then(async (authenticated) => {
                    this.initialized = true;

                    if (authenticated) {
                        await this.handleLogin();
                    } else {
                        this.clearState();
                    }

                    return authenticated;
                })
                .catch((err) => {
                    console.error('Keycloak init failed', err);
                    this.clearState();
                    this.initialized = true;
                    return false;
                });
        }

        return this.initPromise;
    }

    async login(redirectUri: string = `${window.location.origin}/tasks`): Promise<void> {
        await this.init();
        return this.keycloak.login({ redirectUri });
    }

    async register(redirectUri: string = `${window.location.origin}/tasks`): Promise<void> {
        await this.init();
        return this.keycloak.register({ redirectUri });
    }

    async handleLogin(): Promise<void> {
        this._accessToken = this.keycloak.token ?? null;

        const token = this.keycloak.tokenParsed as any;

        this._userId = token?.sub ?? '';
        this._userName =
            token?.name ??
            token?.preferred_username ??
            token?.email ??
            '';

        const realmRoles = this.keycloak.realmAccess?.roles ?? [];
        const clientRoles =
            this.keycloak.resourceAccess?.[environment.keycloakClientId]?.roles ?? [];

        this._userRoles = Array.from(new Set([...realmRoles, ...clientRoles]));

        this.cleanAuthUrl();

        this.loginSubject.next();
    }

    async logout(redirectUri: string = `${window.location.origin}/login`): Promise<void> {
        this.clearState();
        this.logoutSubject.next();
        await this.keycloak.logout({ redirectUri });
    }

    async ensureFreshToken(minValidity = 30): Promise<string | null> {
        await this.init();

        if (!this.keycloak.authenticated) {
            return null;
        }

        try {
            await this.keycloak.updateToken(minValidity);
            this._accessToken = this.keycloak.token ?? null;
            return this._accessToken;
        } catch (error) {
            console.error('Token refresh failed', error);
            this.clearState();
            return null;
        }
    }

    getToken(): string | null {
        return this._accessToken;
    }

    getId(): string {
        return this._userId;
    }

    getName(): string {
        return this._userName;
    }

    getRoles(): string[] {
        return [...this._userRoles];
    }

    isLoggedIn(): boolean {
        return !!this._accessToken;
    }

    private cleanAuthUrl(): void {
        setTimeout(() => {
            const url = new URL(window.location.href);

            url.searchParams.delete('state');
            url.searchParams.delete('session_state');
            url.searchParams.delete('iss');
            url.searchParams.delete('code');

            window.history.replaceState(
                {},
                document.title,
                url.pathname + url.search
            );
        }, 0);
    }

    private bindEvents(): void {
        this.keycloak.onAuthSuccess = () => {
            void this.handleLogin();
        };

        this.keycloak.onAuthRefreshSuccess = () => {
            void this.handleLogin();
        };

        this.keycloak.onAuthLogout = () => {
            this.clearState();
            this.logoutSubject.next();
        };

        this.keycloak.onTokenExpired = () => {
            void this.ensureFreshToken();
        };
    }

    private clearState(): void {
        this._accessToken = null;
        this._userId = '';
        this._userName = '';
        this._userRoles = [];
    }
}