import { Component, OnInit } from '@angular/core';
import { SignalrService } from './core/services/signalr.service';
import { AuthService } from './core/services/auth.service';
import { RouterOutlet } from '@angular/router';


@Component({
    selector: 'app-root',
    imports: [RouterOutlet],
    templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
    constructor(private signalr: SignalrService, private auth: AuthService) { }


    ngOnInit() {
        // Если пользователь уже залогинен — стартуем SignalR соединение
        if (this.auth.isLoggedIn()) {
            this.signalr.startConnection();
        }
    }
}