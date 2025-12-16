import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environment';
import { Subject, Observable } from 'rxjs';
import { AuthService } from './auth.service';


@Injectable({ providedIn: 'root' })
export class SignalrService {
    private hubConnection?: signalR.HubConnection;
    private updates$ = new Subject<any>();


    constructor(private auth: AuthService) { }


    startConnection() {
        if (this.hubConnection) return;


        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${environment.apiBaseUrl.replace(/^http/, 'ws')}/hub`, { accessTokenFactory: () => this.auth.getToken() ?? '' })
            .withAutomaticReconnect()
            .build();


        this.hubConnection.start().catch(err => console.error('SignalR start error', err));


        // Example: listen for server push events
        this.hubConnection.on('PageDataUpdated', (payload: any) => {
            // push payload to subscribers
            this.updates$.next(payload);
        });
    }


    onUpdates(): Observable<any> {
        return this.updates$.asObservable();
    }


    invoke(method: string, args?: any) {
        if (!this.hubConnection) this.startConnection();
        return this.hubConnection?.invoke(method, args);
    }
}