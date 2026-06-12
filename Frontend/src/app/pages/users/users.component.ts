import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { SignalrService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { UpdateRolesDto, UserDto } from '../../core/models/dtos';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { Subject } from 'rxjs';

export type FilterTarget = 'username' | 'email';

export type UpdateTarget = {
    id: string;
    userName: string;
    email: string;
    assignedRoles: string[];
    rolesChecked: Set<string>;   // роли к добавлению
    rolesUnchecked: Set<string>; // роли к удалению
};

const DEFAULT_ROLE = 'User';

@Component({
    selector: 'app-users',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit, OnDestroy {
    users: UserDto[] = [];
    filteredUsers: UserDto[] = [];

    filterValue = '';
    filterTarget: FilterTarget = 'username';

    detailsDialogOpen = false;
    updateRolesSubmitting = false;
    updateTarget: UpdateTarget | null = null;

    readonly defaultRole = DEFAULT_ROLE;
    availableRoles: string[] = [];

    private destroy$ = new Subject<void>();

    constructor(
        private api: ApiService,
        private signalr: SignalrService,
        public auth: AuthService
    ) { }

    ngOnInit(): void {
        this.loadInitial();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    loadInitial() {
        this.api.getUsers().subscribe({
            next: (response) => {
                this.users = response?.filter(x => x.id !== this.auth.getId()) || [];
                this.applyFilters();
            },
            error: (error) => {
                console.log(error);
            }
        });

        this.api.getAvailableRoles().subscribe({
            next: (response) => {
                this.availableRoles = (response || [])
                    .map((role: any) => role.roleName ?? role.name ?? role.title ?? role)
                    .filter((role: any) => typeof role === 'string' && role.trim().length > 0);
            },
            error: (error) => {
                console.log(error);
            }
        });
    }

    applyFilters() {
        this.filteredUsers = this.users.filter(u => {
            switch (this.filterTarget) {
                case 'username':
                    if (this.filterValue && !u.username.includes(this.filterValue)) return false;
                    break;

                case 'email':
                    if (this.filterValue && !u.email.includes(this.filterValue)) return false;
                    break;

                default:
                    return false;
            }

            return true;
        });
    }

    clearFilters() {
        this.filterTarget = 'username';
        this.filterValue = '';
        this.applyFilters();
    }

    onChangeRoles(u: UserDto) {
        this.api.getUserRoles(u.id).subscribe({
            next: (response) => {
                const userRoles = (response || [])
                    .map((role: any) => role.roleName ?? role.name ?? role.title ?? role)
                    .filter((role: any) => typeof role === 'string' && role.trim().length > 0);

                const assignedRoles = this.availableRoles.filter(role => userRoles.includes(role));

                if (!assignedRoles.includes(this.defaultRole)) {
                    assignedRoles.unshift(this.defaultRole);
                }

                this.updateTarget = {
                    id: u.id,
                    userName: u.username,
                    email: u.email,
                    assignedRoles,
                    rolesChecked: new Set<string>(),
                    rolesUnchecked: new Set<string>()
                };

                this.detailsDialogOpen = true;
            },
            error: (error) => {
                console.log(error);
            }
        });
    }

    isRoleSelected(role: string): boolean {
        if (role === this.defaultRole) return true;
        if (!this.updateTarget) return false;

        if (this.updateTarget.rolesChecked.has(role)) return true;
        if (this.updateTarget.rolesUnchecked.has(role)) return false;

        return this.updateTarget.assignedRoles.includes(role);
    }

    onRoleToggle(role: string, checked: boolean) {
        if (!this.updateTarget) return;
        if (role === this.defaultRole) return;

        const wasInitiallyAssigned = this.updateTarget.assignedRoles.includes(role);

        if (checked) {
            this.updateTarget.rolesUnchecked.delete(role);

            if (!wasInitiallyAssigned) {
                this.updateTarget.rolesChecked.add(role);
            }
        } else {
            this.updateTarget.rolesChecked.delete(role);

            if (wasInitiallyAssigned) {
                this.updateTarget.rolesUnchecked.add(role);
            }
        }
    }

    closeDetailsDialog() {
        this.detailsDialogOpen = false;
        this.updateTarget = null;
    }

    trackByRole(_: number, role: string) {
        return role;
    }

    getRolesToAdd(): string[] {
        return this.updateTarget ? Array.from(this.updateTarget.rolesChecked) : [];
    }

    getRolesToRemove(): string[] {
        return this.updateTarget ? Array.from(this.updateTarget.rolesUnchecked) : [];
    }

    confirmUpdateRoles() {
        this.updateRolesSubmitting = true;

        const roleDto: UpdateRolesDto = {
            rolesToAdd: this.getRolesToAdd(),
            rolesToRemove: this.getRolesToRemove()
        }

        if (this.updateTarget)
            this.api.updateUserRoles(this.updateTarget?.id, roleDto).subscribe(
                {
                    next: (response) => {
                        this.closeDetailsDialog();
                    },
                    error: (error) => {
                        console.log(error);
                    }
                }
            )
    }

    formatTimestamp(stamp: string | number | null | undefined): string {
        if (stamp === null || stamp === undefined) return '—';

        const n = typeof stamp === 'string' ? Number(stamp) : stamp;
        if (!Number.isFinite(n)) return '—';

        const d = new Date(n); // Keycloak already sends milliseconds
        return Number.isNaN(d.getTime()) ? '—' : d.toLocaleString('ru-RU');
    }
}