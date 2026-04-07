import { Injectable } from '@angular/core';
import Keycloak from 'keycloak-js';
import { environment } from '../../environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private keycloak: Keycloak = new Keycloak({
    url: environment.keycloakUrl,
    realm: environment.keycloakRealm,
    clientId: environment.keycloakClientId,
  });

  private initPromise?: Promise<boolean>;

  private init(): Promise<boolean> {
    if (!this.initPromise) {
      this.initPromise = this.keycloak.init({
        onLoad: 'check-sso',
        pkceMethod: 'S256',
        checkLoginIframe: false,
      });
    }

    return this.initPromise;
  }

  async login(redirectUri: string = window.location.href): Promise<void> {
    await this.init();
      return await this.keycloak.login({ redirectUri });
  }

  async register(redirectUri: string = window.location.href): Promise<void> {
    await this.init();
      return await this.keycloak.register({ redirectUri });
  }

  async logout(redirectUri: string = window.location.origin): Promise<void> {
    await this.init();
      return await this.keycloak.logout({ redirectUri });
  }
}