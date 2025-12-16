import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';


@Injectable()
export class JwtInterceptor implements HttpInterceptor {
    constructor(private auth: AuthService) { }


    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const token = this.auth.getToken();


        // Do not attach token to login/register or public auth endpoints
        const url = req.url.toLowerCase();
        if (url.includes('/auth/login') || url.includes('/auth/register') || url.includes('/auth/google')) {
            return next.handle(req);
        }


        if (token) {
            const cloned = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
            return next.handle(cloned);
        }


        return next.handle(req);
    }
}