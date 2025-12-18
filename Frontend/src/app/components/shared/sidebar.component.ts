import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

@Component({
    selector: 'app-sidebar',
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss'],
    imports: [CommonModule, RouterModule]
})
export class SidebarComponent implements OnInit {
    roles: string[] = [];

    constructor(private auth: AuthService, private router: Router) { }

    ngOnInit() {
        this.roles = this.auth.getRoles();
    }

    isEditorOrAdmin() {
        return this.roles.includes('Editor') || this.roles.includes('Admin');
    }
}
