import { Component } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../components/shared/sidebar.component';


@Component({ selector: 'app-profile', templateUrl: './profile.component.html', imports: [SidebarComponent] })
export class ProfileComponent {
    name = localStorage.getItem('user_name');
    id = localStorage.getItem('user_id');
    roles = JSON.parse(localStorage.getItem('user_roles') || '[]');


    constructor(private auth: AuthService) { }
}