import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { SignalrService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { DeletionRequestDto, LanguageDto, ProblemDto } from '../../core/models/dtos';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { Subscription, switchMap } from 'rxjs';

export type DeletedTaskDetails = {
    task: ProblemDto;
    deleteRequest: DeletionRequestDto | null;
}

@Component({
    selector: 'app-deletion-queue',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './deletion-queue.component.html'
})
export class DeletionQueueComponent implements OnInit, OnDestroy {
    problems: ProblemDto[] = [];
    filtered: ProblemDto[] = [];

    filterSlug = '';
    showOnlyPublished: 'all' | 'published' | 'unpublished' = 'all';

    detailsDialogOpen = false;
    restoreSubmitting = false;
    restoreTarget: DeletedTaskDetails | null = null;

    constructor(
        private api: ApiService,
        private signalr: SignalrService,
        public auth: AuthService,
        private router: Router
    ) { }

    ngOnInit() {
        this.loadInitial();
    }

    ngOnDestroy(): void {
    }

    loadInitial() {
        this.api.getProblems({ getDeleted: true }).subscribe(p => {
            this.problems = p || [];
            this.applyFilters();
        });
    }

    applyFilters() {
        this.filtered = this.problems.filter(p => {
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

    setRestoreTarget(p: ProblemDto, openDialog = true) {
        this.restoreTarget = { task: p, deleteRequest: null };

        if (openDialog) {
            this.detailsDialogOpen = true;
        }

        this.api.getFilteredProblemDeletionRequests({ exactMatch: true, problemId: p.id }).subscribe({
            next: (response) => {
                const deletionRequest = response[0] ?? null;
                this.restoreTarget = { task: p, deleteRequest: deletionRequest };
            },
            error: () => {
                // target уже выставлен, диалог уже открыт/не открыт по флагу
            }
        });
    }

    onRestoreProblem(p: ProblemDto) {
        this.restoreSubmitting = true;

        this.api.restoreProblem(p.id).subscribe({
            next: () => {
                this.restoreSubmitting = false;
                this.closeDetailsDialog();

                this.problems = this.problems.filter(problem => problem.id !== p.id);
                this.applyFilters();
            },
            error: () => {
                this.restoreSubmitting = false;
            }
        });
    }

    closeDetailsDialog() {
        this.detailsDialogOpen = false;
    }

    confirmRestoreRequest() {
        if (!this.restoreTarget) return;

        this.restoreSubmitting = true;
        this.onRestoreProblem(this.restoreTarget.task);
    }
}