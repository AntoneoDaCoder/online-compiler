import { enableProdMode, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { environment } from './app/environment';
import { appRoutes } from './app/app-routing.module';
import { JwtInterceptor } from './app/core/interceptors/jwt.interceptor';
import { MonacoEditorModule, NgxMonacoEditorConfig } from 'ngx-monaco-editor-v2';

if (environment.production) {
  enableProdMode();
}

const monacoConfig: NgxMonacoEditorConfig = {
  baseUrl: 'assets',
  defaultOptions: {
    scrollBeyondLastLine: false
  },
  requireConfig: {
    preferScriptTags: true
  }
};

bootstrapApplication(AppComponent, {
  providers: [
    provideZoneChangeDetection(),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptorsFromDi()),
    importProvidersFrom(MonacoEditorModule.forRoot(monacoConfig)),
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }
  ]
}).catch(err => console.error(err));