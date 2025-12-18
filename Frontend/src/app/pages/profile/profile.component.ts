import { Component } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../components/shared/sidebar.component';


@Component({ selector: 'app-profile', templateUrl: './profile.component.html', imports: [SidebarComponent] })
export class ProfileComponent {
    name = ''
    id = ''
    roles: String[] = [];


    constructor(private auth: AuthService) {
        this.name = auth.getName();
        this.id = auth.getId();
        this.roles = auth.getRoles();
    }
}