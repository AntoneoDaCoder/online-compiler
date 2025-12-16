import { provideZoneChangeDetection } from "@angular/core";
// src/main.ts
import { enableProdMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { environment } from './app/environment';
import { appRoutes } from './app/app-routing.module';
import { JwtInterceptor } from './app/core/interceptors/jwt.interceptor';

if (environment.production) {
  enableProdMode();
}

bootstrapApplication(AppComponent, {
  providers: [
    provideZoneChangeDetection(),
    provideHttpClient(),
    provideRouter(appRoutes),

    // HttpClient + DI interceptors
    provideHttpClient(withInterceptorsFromDi()),

    // JWT interceptor
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }
  ]
}).catch(err => console.error(err));
