import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { SignalrService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageDto, ProblemDto, UserProblemVersionDto } from '../../core/models/dtos';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { Subscription, switchMap } from 'rxjs';

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

    filterSlug = '';
    showOnlyPublished: 'all' | 'published' | 'unpublished' = 'all';

    constructor(
        private api: ApiService,
        private signalr: SignalrService,
        public auth: AuthService,
        private router: Router
    ) { }

    ngOnInit() {
        this.loadInitial();
        this.userRoles = this.auth.getRoles() || [];
        if (this.userRoles.find(x => x === "Admin" || x === "Editor"))
            this.adminOrEditor = true;

        this.versionPublishedSubscription = this.signalr.onVersionPublished.pipe(
            switchMap(response => {
                const versionId = response;
                return this.api.getUserVersion(versionId);
            })
        ).subscribe(version => {
            var found = this.problems.findIndex(p => p.id === version.problemId);
            if (found > -1) {
                this.problems[found].latestVersion = version;
            }
        });
    }

    ngOnDestroy(): void {
        this.versionPublishedSubscription.unsubscribe();
    }

    isAdminOrEditor(): boolean {
        return this.userRoles.includes('Admin') || this.userRoles.includes('Editor');
    }

    loadInitial() {
        this.api.getLanguages().subscribe(l => this.languages = l || []);
        this.api.getProblems().subscribe(p => {
            this.problems = p || [];
            this.applyFilters();
        });
        this.userRoles = this.auth.getRoles() || [];
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
        // Server may use literals like "Deleted", "no version", "Unlisted", "Listed" etc.
        if (!p.status) return false;
        const s = p.status.toLowerCase();
        // treat as unpublished if explicit negative statuses
        const unpublished = ['no version', 'deleted', 'unlisted'];
        return !unpublished.includes(s);
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
}
