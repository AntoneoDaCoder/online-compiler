import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { SignalrService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { DeletionRequestDto, LanguageDto, ProblemDto } from '../../core/models/dtos';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { Subscription, switchMap, takeUntil, Subject } from 'rxjs';

export type FilterTarget = 'slug' | 'reason' | 'title';

@Component({
    selector: 'app-deletion-requests',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './deletion-requests.component.html'
})
export class DeletionRequestsComponent implements OnInit, OnDestroy {
    requests: DeletionRequestDto[] = [];
    filteredRequests: DeletionRequestDto[] = [];
    roles: string[] = [];

    filterValue = '';
    filterTarget: FilterTarget = 'slug';

    viewAsEditor = true;
    private destroy$ = new Subject<void>();

    constructor(
        private api: ApiService,
        private signalr: SignalrService,
        public auth: AuthService,
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this.route.data
            .pipe(takeUntil(this.destroy$))
            .subscribe(data => {
                this.viewAsEditor = data['viewAsEditor'];
            });
        this.loadInitial();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    loadInitial() {
        const userId = this.auth.getId();
        this.roles = this.auth.getRoles();

        if (!this.viewAsEditor) {
            this.api.getFilteredProblemDeletionRequests({ excludeUser: true, userId: userId }).subscribe(r => {
                this.requests = r || [];
                this.applyFilters();
            });
        }
        else {
            this.api.getOwnProblemDeletionRequests(userId).subscribe(r => {
                this.requests = r || [];
                this.applyFilters();
            });
        }

    }

    applyFilters() {
        this.filteredRequests = this.requests.filter(r => {
            switch (this.filterTarget) {
                case 'reason':
                    if (this.filterValue && !r.reason.includes(this.filterValue)) return false;
                    break;

                case 'slug':
                    if (this.filterValue && !r.problemSlug.includes(this.filterValue)) return false;
                    break;

                case 'title':
                    if (this.filterValue && !r.problemTitle.includes(this.filterValue)) return false;
                    break;

                default:
                    return false;
            }

            return true;
        });
    }

    clearFilters() {
        this.filterTarget = 'slug';
        this.filterValue = '';
        this.applyFilters();
    }

    onCancelRequest(p: DeletionRequestDto) {
        this.api.cancelProblemDeletionRequest(p.problemId, p.id).subscribe({
            next: () => {
                this.requests = this.requests.filter(r => r.id !== p.id);
                this.applyFilters();
            },
            error: () => {

            }
        })
    }

    onApproveRequest(p: DeletionRequestDto) {
        this.api.approveProblemDeletionRequest(p.problemId, p.id).subscribe({
            next: () => {
                this.requests = this.requests.filter(r => r.id !== p.id);
                this.applyFilters();
            },
            error: () => {

            }
        })
    }

    isAdmin() {
        return this.roles.includes('Admin')
    }
}