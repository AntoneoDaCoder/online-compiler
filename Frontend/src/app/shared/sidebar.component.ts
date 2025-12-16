import { Component, OnInit } from '@angular/core';
import { AuthService } from '../core/services/auth.service';
import { CommonModule } from '@angular/common';


@Component({ selector: 'app-sidebar', templateUrl: './sidebar.component.html', imports: [CommonModule] })
export class SidebarComponent implements OnInit {
    roles: string[] = [];


    constructor(private auth: AuthService) { }


    ngOnInit() { this.roles = this.auth.getRoles(); }


    isEditorOrAdmin() { return this.roles.includes('Editor') || this.roles.includes('Admin'); }
}