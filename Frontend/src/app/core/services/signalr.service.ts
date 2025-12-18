import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environment';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { CodeResponseDto, EditorProblemVersionDto } from '../models/dtos';


@Injectable({ providedIn: 'root' })
export class SignalrService implements OnDestroy {

    private hubConnection?: signalR.HubConnection;

    private codeResponseMessages$ = new Subject<CodeResponseDto>();
    private versionPublishedMessages$ = new Subject<string>();

    private draftUpdatedMessages$ = new Subject<string>();
    private draftDeletedMessages$ = new Subject<string>();
    private draftCreatedMessages$ = new Subject<EditorProblemVersionDto>();

    public onCodeResponse$ = this.codeResponseMessages$.asObservable();
    public onVersionPublished = this.versionPublishedMessages$.asObservable();

    public onDraftCreated = this.draftCreatedMessages$.asObservable();
    public onDraftUpdated = this.draftUpdatedMessages$.asObservable();
    public onDraftDeleted = this.draftDeletedMessages$.asObservable();

    private connectionState = new BehaviorSubject<boolean>(false);
    public connectionState$ = this.connectionState.asObservable();


    constructor(
        private auth: AuthService
    ) {
        this.auth.onLogin$.subscribe(() => this.startConnection());
        this.auth.onLogout$.subscribe(() => this.stopConnection());

        // Если пользователь уже залогинен при старте приложения
        if (this.auth.isLoggedIn()) {
            this.startConnection();
        }
    }

    public async startConnection(): Promise<void> {
        if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
            return;
        }

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(environment.apiBaseUrl + "/hubs/user",
                { accessTokenFactory: () => this.auth.getToken() ?? '' })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Стратегия переподключения
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Регистрация обработчиков сообщений от сервера
        this.setupMessageHandlers();

        try {
            await this.hubConnection.start();
            this.connectionState.next(true);
            console.log('SignalR соединение установлено');
        } catch (err) {
            console.error('Ошибка подключения SignalR:', err);
            this.connectionState.next(false);
            throw err;
        }

        // Обработка переподключения
        this.hubConnection.onreconnected(() => {
            console.log('SignalR переподключен');
            this.connectionState.next(true);
        });

        // Обработка разрыва соединения
        this.hubConnection.onclose(() => {
            console.log('SignalR соединение закрыто');
            this.connectionState.next(false);
        });
    }

    public stopConnection(): void {
        this.hubConnection?.stop();
    }

    private setupMessageHandlers(): void {
        if (!this.hubConnection) {
            return;
        }

        // Пример обработчика для конкретного метода от сервера
        this.hubConnection.on('ExecutionCompleted', (data: CodeResponseDto) => {
            this.codeResponseMessages$.next(data);
        });

        this.hubConnection.on('VersionPublished', (data: string) => {
            this.versionPublishedMessages$.next(data);
        });

        this.hubConnection.on('DraftCreated', (data: EditorProblemVersionDto) => {
            this.draftCreatedMessages$.next(data);
        });

        this.hubConnection.on('DraftUpdated', (data: string) => {
            this.draftUpdatedMessages$.next(data);
        });

        this.hubConnection.on('DraftDeleted', (data: string) => {
            this.draftDeletedMessages$.next(data);
        });
    }

    ngOnDestroy() {
        this.stopConnection();
        this.codeResponseMessages$.complete();
        this.versionPublishedMessages$.complete();
        this.draftCreatedMessages$.complete();
        this.draftDeletedMessages$.complete();
        this.draftUpdatedMessages$.complete();
    }


}