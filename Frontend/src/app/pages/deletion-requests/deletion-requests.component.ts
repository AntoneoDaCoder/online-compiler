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
import { NgZone } from '@angular/core';

export type FilterTarget = 'slug' | 'reason' | 'title' | 'status';

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
    showOnlyApproved: 'all' | 'approved' | 'not-approved' = 'all';

    viewAsEditor = true;

    private destroy$ = new Subject<void>();

    private deletionRequestCreated = new Subscription();
    private deletionRequestDeleted = new Subscription();
    private deletionRequestApproved = new Subscription();

    constructor(
        private api: ApiService,
        private signalr: SignalrService,
        public auth: AuthService,
        private route: ActivatedRoute,
        private ngZone: NgZone
    ) { }

    ngOnInit(): void {
        this.route.data
            .pipe(takeUntil(this.destroy$))
            .subscribe(data => {
                this.viewAsEditor = data['viewAsEditor'];
            });

        this.setupSubscriptions();

        this.loadInitial();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();

        this.deletionRequestDeleted.unsubscribe();
        this.deletionRequestApproved.unsubscribe();

        if (!this.viewAsEditor) {
            this.deletionRequestCreated.unsubscribe();
        }
    }

    loadInitial() {
        const userId = this.auth.getId();
        this.roles = this.auth.getRoles();

        if (!this.viewAsEditor) {
            this.api.getFilteredProblemDeletionRequests({ excludeUser: true, userId: userId, onlyNotApproved: true })
                .subscribe(r => {
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

                case 'status':
                    if (this.showOnlyApproved !== 'all') {
                        const isApproved = r.isApproved;
                        if (this.showOnlyApproved === 'approved' && !isApproved) return false;
                        if (this.showOnlyApproved === 'not-approved' && isApproved) return false;
                    }
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
                //no processing logic because signalr has to notify admins about request deletion
                //because if editor cancels a request, we have to notify admins
                //if admin cancels request, we have to notify other admins and user
                //signalr will handle it with ease
            },
            error: (error) => {
                console.log(error);
            }
        })
    }

    onApproveRequest(r: DeletionRequestDto) {
        this.api.approveProblemDeletionRequest(r.problemId, r.id).subscribe({
            next: () => {
                if (this.viewAsEditor) {
                    var found = this.requests.findIndex(x => x.id === r.id);
                    if (found > -1) {
                        this.requests[found].isApproved = true;
                        this.applyFilters();
                    }
                }
            },
            error: (error) => {
                console.log(error);
            }
        })
    }

    isAdmin() {
        return this.roles.includes('Admin')
    }

    formatStatus(s: boolean) {
        if (s) {
            return "Подтверждён";
        }
        else
            return "Не подтверждён";
    }

    private setupSubscriptions() {
        if (!this.viewAsEditor) {
            this.deletionRequestCreated.add(
                this.signalr.onDeletionRequestCreated.subscribe(response => {
                    this.ngZone.run(() => {
                        if (response.initiatorId == this.auth.getId() || response.isApproved == true) return;

                        this.requests.push(response);

                        this.applyFilters();
                    })
                })
            )
        }

        this.deletionRequestDeleted.add(
            this.signalr.onDeletionRequestDeleted.subscribe(response => {
                this.ngZone.run(() => {
                    this.requests = this.requests.filter(r => r.id !== response);
                    this.applyFilters();
                })
            })
        )

        this.deletionRequestApproved = this.signalr.onDeletionRequestApproved.pipe(
            switchMap(response => {
                return response;
            })
        ).subscribe({
            next: (requestId) => {
                if (this.viewAsEditor) {
                    var found = this.requests.findIndex(x => x.id === requestId);
                    if (found > -1) {
                        this.requests[found].isApproved = true;
                        this.applyFilters();
                    }
                }
                else {
                    this.requests = this.requests.filter(x => x.id !== requestId);
                    this.applyFilters();
                }
            },
            error: (error) => {
                console.log(error);
            }
        });

    }
}