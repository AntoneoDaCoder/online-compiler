import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { UserMetadataDto } from '../../core/models/dtos';
import { ApiService } from '../../core/services/api.service';
import { CommonModule } from '@angular/common';


@Component(
    {
        selector: 'app-profile',
        templateUrl: './profile.component.html',
        imports: [SidebarComponent, CommonModule]
    }
)

export class ProfileComponent implements OnInit {
    name = ''
    id = ''
    roles: String[] = [];

    deleteDialogOpen = false;
    deleteDialogMode: 'delete' | 'cancel' = 'delete';
    deleteSubmitting = false;


    userMetadata: UserMetadataDto | null = null;

    constructor
        (
            private auth: AuthService,
            private api: ApiService
        ) {
        this.name = auth.getName();
        this.id = auth.getId();
        this.roles = auth.getRoles();
    }

    ngOnInit(): void {
        this.api.getUserMetadata(this.id).subscribe({
            next: (response) => {
                this.userMetadata = response;
            },
            error: (error) => {
                console.log(error);
            }
        })
    }

    openDeleteDialog(mode: 'delete' | 'cancel') {
        this.deleteDialogMode = mode;
        this.deleteDialogOpen = true;
    }

    closeDeleteDialog() {
        this.deleteDialogOpen = false;
        this.deleteSubmitting = false;
    }

    confirmDeleteDialog() {
        if (this.deleteDialogMode === 'delete') {
            this.deleteSubmitting = true;
            this.api.softDeleteUser(this.id).subscribe({
                next: (response) => {
                    if (response) {
                        this.userMetadata = response;
                    }
                    this.closeDeleteDialog();
                },
                error: (error) => {
                    console.log(error);
                    this.closeDeleteDialog();
                }
            }
            )
        }
        else {
            this.deleteSubmitting = true;
            this.api.cancelUserDeletion(this.id).subscribe({
                next: () => {
                    this.userMetadata = null;
                    this.closeDeleteDialog();
                },
                error: (error) => {
                    console.log(error);
                    this.closeDeleteDialog();
                }
            })
        }
    }
}