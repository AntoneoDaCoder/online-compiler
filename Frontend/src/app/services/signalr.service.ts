import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;

  startConnection(): Promise<void> {
    if (this.hubConnection) return Promise.resolve();

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:12345/hubs/resultHub')
      .withAutomaticReconnect()
      .build();

    return this.hubConnection.start();
  }

  joinGroup(requestId: string): Promise<void> {
    if (!this.hubConnection) throw new Error('Hub connection not established');
    return this.hubConnection.invoke('JoinGroupAsync', requestId);
  }

  onMessage(callback: (message: string) => void): void {
    this.hubConnection?.on('ExecutionCompleted', callback);
  }
}
