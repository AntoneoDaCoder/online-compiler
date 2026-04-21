import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { SignalrService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageDto, ProblemDto, ProblemUpdateDto } from '../../core/models/dtos';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { Subscription, switchMap } from 'rxjs';
import { NgZone } from '@angular/core';

@Component({
    selector: 'app-tasks',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './task.component.html'
})
export class TasksComponent implements OnInit, OnDestroy {
    languages: LanguageDto[] = [];
    problems: ProblemDto[] = [];
    filtered: ProblemDto[] = [];
    userRoles: string[] = [];
    adminOrEditor = false;

    private versionPublishedSubscription = new Subscription();
    private problemDeletedSubscription = new Subscription();
    private restoreProblemSubscription = new Subscription();
    private problemCreatedSubscription = new Subscription();

    filterSlug = '';
    showOnlyPublished: 'all' | 'published' | 'unpublished' = 'all';

    deleteDialogOpen = false;
    createDialogOpen = false;

    createSlug: string = '';
    createTitle: string = '';

    deleteReason = '';
    deleteTarget: ProblemDto | null = null;

    deleteSubmitting = false;
    createSubmitting = false;

    constructor(
        private api: ApiService,
        private signalr: SignalrService,
        public auth: AuthService,
        private router: Router,
        private ngZone: NgZone
    ) { }

    ngOnInit() {
        this.loadInitial();
        this.userRoles = this.auth.getRoles() || [];
        if (this.userRoles.find(x => x === "Admin" || x === "Editor"))
            this.adminOrEditor = true;

        this.setupSubscriptions();
    }

    ngOnDestroy(): void {
        this.versionPublishedSubscription.unsubscribe();
        this.problemDeletedSubscription.unsubscribe();
        this.restoreProblemSubscription.unsubscribe();
    }

    isAdminOrEditor(): boolean {
        return this.userRoles.includes('Admin') || this.userRoles.includes('Editor');
    }

    loadInitial() {
        this.api.getLanguages().subscribe(l => this.languages = l || []);
        this.api.getProblems({ includeLanguages: true, includeLatestVersion: true }).subscribe(p => {
            this.problems = p || [];
            this.applyFilters();
        });
        this.userRoles = this.auth.getRoles() || [];
    }

    applyFilters() {
        this.filtered = this.problems.filter(p => {
            if (p.isDeleted) return false;

            if (this.filterSlug && !p.slug.includes(this.filterSlug)) return false;

            if (this.showOnlyPublished !== 'all') {
                const isPublished = this.isPublished(p);
                if (this.showOnlyPublished === 'published' && !isPublished) return false;
                if (this.showOnlyPublished === 'unpublished' && isPublished) return false;
            }

            return true;
        });
    }

    clearFilters() {
        this.filterSlug = '';
        this.showOnlyPublished = 'all';
        this.applyFilters();
    }

    private isPublished(p: ProblemDto): boolean {
        return p.isPublished;
    }

    onOpenProblem(problem: ProblemDto) {
        const versionId = problem.latestVersion?.versionId ?? '';
        const mappedSupported: LanguageDto[] = [];
        const supportedIds = problem.latestVersion?.supportedLanguages ?? [];
        for (const id of supportedIds) {
            const found = this.languages.find(x => String(x.id) === String(id));
            if (found) mappedSupported.push(found);
            else mappedSupported.push({ id, code: String(id), displayName: String(id) } as LanguageDto);
        }

        const state = {
            versionId: versionId,
            slug: problem.slug,
            statement: problem.latestVersion?.statement ?? '',
            supportedLanguages: mappedSupported,
            title: problem.title
        };

        this.router.navigate(['/code-editor'], { state });
    }

    formatSupportedLanguages(langIds?: string[] | null): string {
        if (!langIds || !this.languages || this.languages.length === 0) return '';
        return langIds
            .map(id => this.languages.find(x => x.id === id)?.displayName ?? id)
            .join(', ');
    }

    onEditProblem(problem: ProblemDto) {
        this.router.navigate(['/problems', problem.slug, 'edit']);
    }

    onDeleteProblem(problem: ProblemDto) {
        this.deleteTarget = problem;
        this.deleteReason = '';
        this.deleteDialogOpen = true;
    }

    closeDeleteDialog() {
        this.deleteDialogOpen = false;
        this.deleteReason = '';
        this.deleteTarget = null;
        this.deleteSubmitting = false;
    }

    closeCreateDialog() {
        this.createDialogOpen = false;
        this.createSlug = '';
        this.createTitle = '';
        this.createSubmitting = false;
    }

    confirmDeleteRequest() {
        if (!this.deleteTarget) return;

        const reason = this.deleteReason.trim();
        if (!reason) return;

        this.deleteSubmitting = true;

        this.api.createProblemDeletionRequest(this.deleteTarget.id, this.auth.getId(), reason).subscribe({
            next: () => {
                this.closeDeleteDialog();
            },
            error: () => {
                this.deleteSubmitting = false;
            }
        });
    }

    getTaskSlug() {
        this.api.getTaskSlug().subscribe({
            next: (response) => {
                this.createSlug = response;
            },
            error: (error) => {
                this.createSlug = 'Task-12345';
                console.log(error);
            }
        }
        )
    }

    confirmCreateRequest() {
        const problemDto: ProblemUpdateDto = {
            slug: this.createSlug,
            title: this.createTitle
        }

        this.api.createProblem(problemDto).subscribe(
            {
                next: () => {
                    //do nothing, as signalr will notify us
                    this.closeCreateDialog();
                },
                error: (error) => {
                    this.createSubmitting = false;
                    console.log(error);
                }
            }
        )
    }
    openCreateDialog() {
        this.createDialogOpen = true;
    }

    private setupSubscriptions() {
        this.versionPublishedSubscription = this.signalr.onVersionPublished.pipe(
            switchMap(response => {
                const versionId = response;
                return this.api.getUserVersion(versionId);
            })
        ).subscribe(version => {
            const found = this.problems.findIndex(p => p.id === version.problemId);
            if (found > -1) {
                this.problems[found].latestVersion = version;
            }
        });

        this.problemDeletedSubscription.add(
            this.signalr.onProblemDeleted.subscribe(response => {
                this.ngZone.run(() => {
                    this.problems = this.problems.filter(v => v.id !== response);
                    this.applyFilters();
                })
            })
        );

        this.restoreProblemSubscription.add(
            this.signalr.onProblemRestored.subscribe(response => {
                this.ngZone.run(() => {
                    this.problems.push(response);
                    this.applyFilters();
                })
            })
        );

        this.problemCreatedSubscription.add(
            this.signalr.onProblemCreated.subscribe(response => {
                this.ngZone.run(() => {
                    this.problems.push(response);
                    this.applyFilters();
                })
            })
        );
    }
}