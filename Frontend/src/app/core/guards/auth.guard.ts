import { Injectable } from '@angular/core';
import {
    CanActivate,
    ActivatedRouteSnapshot,
    RouterStateSnapshot,
    Router,
    UrlTree,
    CanActivateChild
} from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({
    providedIn: 'root'
})
export class AuthGuard implements CanActivate, CanActivateChild {
    constructor(private auth: AuthService, private router: Router) { }

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): boolean | UrlTree | Observable<boolean | UrlTree> | Promise<boolean | UrlTree> {
        // Проверка логина
        if (!this.auth.isLoggedIn()) {
            // Можно добавить queryParam returnUrl если хотите возвращать после логина
            return this.router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
        }

        // Если маршрут требует роли — проверяем
        const requiredRoles: string[] | undefined = route.data && route.data['roles'];
        if (requiredRoles && requiredRoles.length > 0) {
            const userRoles = this.auth.getRoles() || [];
            const hasRole = requiredRoles.some(r => userRoles.includes(r));
            if (!hasRole) {
                // Нет нужных ролей — перенаправляем на страницы задач или профиль
                return this.router.createUrlTree(['/tasks']);
            }
        }

        return true;
    }

    canActivateChild(childRoute: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
        return this.canActivate(childRoute, state);
    }
}
