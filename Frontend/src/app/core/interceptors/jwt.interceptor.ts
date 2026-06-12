import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable, from } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';
import { environment } from '../../environment';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const isApiRequest = req.url.startsWith(environment.apiBaseUrl);

    if (!isApiRequest) {
      return next.handle(req);
    }

    return from(this.auth.ensureFreshToken()).pipe(
      switchMap((token) => {
        if (!token) {
          return next.handle(req);
        }

        const cloned = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
          },
        });

        return next.handle(cloned);
      })
    );
  }
}