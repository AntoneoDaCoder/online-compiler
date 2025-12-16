import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environment';
import { AuthService } from '../../core/services/auth.service';


@Component({ selector: 'app-google-callback', template: '<p>Processing...</p>' })
export class GoogleCallbackComponent implements OnInit {
    constructor(private route: ActivatedRoute, private http: HttpClient, private auth: AuthService) { }


    ngOnInit() {
        // сервер может редиректить на этот callback с кодом или с данными
        this.route.queryParams.subscribe(params => {
            const code = params['code'];
            const token = params['token'];


            // Вариант A: сервер вернул готовый токен в query (не рекомендуется) — обрабатываем
            if (token) {
                // формируем LoginDataDto minimal wrapper
                this.auth.handleLogin({ userId: params['userId'] || '', name: params['name'] || '', roles: [], accountCreatedAt: null, accessToken: token });
                return;
            }


            // Вариант B: получили временный code — отправляем на backend для обмена
            if (code) {
                this.http.post(`${environment.apiBaseUrl}/auth/google/callback`, { code, callback: environment.googleCallbackUrl }).subscribe({
                    next: (dto: any) => {
                        // ожидаем LoginDataDto
                        this.auth.handleLogin(dto);
                    },
                    error: () => {
                        // остаться на login — можно навигировать
                    }
                });
            }
        });
    }
}