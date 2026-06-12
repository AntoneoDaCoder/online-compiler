import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { LanguageDto, ProblemDto, EditorProblemVersionDto } from '../../core/models/dtos';
import { Subscription, switchMap } from 'rxjs';
import { SignalrService } from '../../core/services/signalr.service';
import { NgZone } from '@angular/core';

type ShowPublished = 'all' | 'published' | 'unpublished';

interface VersionView {
    version: EditorProblemVersionDto;
    problem?: ProblemDto | null;
    isPublished: boolean;
    versionKey: string;
}

@Component({
    selector: 'app-versions',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './versions-component.html'
})
export class VersionsComponent implements OnInit, OnDestroy {
    languages: LanguageDto[] = [];
    problems: ProblemDto[] = [];
    versions: EditorProblemVersionDto[] = [];
    filtered: VersionView[] = [];

    userRoles: string[] = [];

    private draftCreatedSubscription = new Subscription();
    private draftUpdatedSubscription = new Subscription();
    private draftDeletedSubscription = new Subscription();
    private draftPublishedSubcription = new Subscription();


    filterSlug = '';
    showOnlyPublished: ShowPublished = 'all';

    constructor(
        private api: ApiService,
        public auth: AuthService,
        private signalr: SignalrService,
        private router: Router,
        private ngZone: NgZone
    ) { }

    ngOnInit() {
        this.userRoles = this.auth.getRoles() || [];
        this.loadInitial();

        this.draftCreatedSubscription.add(
            this.signalr.onDraftCreated.subscribe(response => {
                this.ngZone.run(() => {
                    this.versions.push(response);
                    this.applyFilters();
                })
            }))

        this.draftUpdatedSubscription = this.signalr.onDraftUpdated.pipe(
            switchMap(response => {
                const versionId = response;
                return this.api.getVersionAsEditor(versionId);
            })
        ).subscribe(version => {
            this.ngZone.run(() => {
                var found = this.versions.findIndex(v => v.versionId === version.versionId);
                if (found > -1) {
                    this.versions[found] = version;
                    this.applyFilters();
                }
                else {
                    this.versions.push(version);
                    this.applyFilters();
                }
            })
        });

        this.draftDeletedSubscription.add(
            this.signalr.onDraftDeleted.subscribe(response => {
                this.ngZone.run(() => {
                    this.versions = this.versions.filter(v => v.versionId !== response);
                    this.applyFilters();
                })
            }
            )
        )

        this.draftPublishedSubcription.add(
            this.signalr.onVersionPublished.subscribe(response => {
                this.ngZone.run(() => {
                    var found = this.versions.findIndex(v => v.versionId === response);
                    if (found > -1) {
                        this.versions[found].isPublished = true;
                        this.applyFilters();
                    }
                })
            }
            )
        )
    }

    ngOnDestroy(): void {
        this.draftCreatedSubscription.unsubscribe();
        this.draftDeletedSubscription.unsubscribe();
        this.draftUpdatedSubscription.unsubscribe();
        this.draftPublishedSubcription.unsubscribe();
    }

    isAdminOrEditor(): boolean {
        return this.userRoles.includes('Admin') || this.userRoles.includes('Editor');
    }

    private loadInitial() {
        this.api.getLanguages().subscribe(l => this.languages = l || []);
        this.api.getProblems().subscribe(p => {
            this.problems = p || [];
            this.applyFilters();
        });
        this.loadVersions();
    }

    private loadVersions() {
        this.api.getVersions().subscribe(v => {
            this.versions = v || [];
            this.applyFilters();
        });
    }

    applyFilters() {
        const mapToView = (ver: EditorProblemVersionDto): VersionView => {
            const problem = this.problems.find(p => String(p.id) === String(ver.problemId)) ?? null;

            const isPublished = ver.isPublished;

            const versionKey = ver.versionId;
            return { version: ver, problem: problem ?? undefined, isPublished, versionKey };
        };

        this.filtered = this.versions
            .map(mapToView)
            .filter(v => {
                if (this.filterSlug) {
                    const slug = v.problem?.slug ?? '';
                    if (!slug.includes(this.filterSlug)) return false;
                }

                if (this.showOnlyPublished !== 'all') {
                    if (this.showOnlyPublished === 'published' && !v.isPublished) return false;
                    if (this.showOnlyPublished === 'unpublished' && v.isPublished) return false;
                }

                return true;
            });
    }

    clearFilters() {
        this.filterSlug = '';
        this.showOnlyPublished = 'all';
        this.applyFilters();
    }

    formatSupportedLanguages(langIds?: string[] | null): string {
        if (!langIds || !this.languages || this.languages.length === 0) return '';
        return langIds
            .map(id => this.languages.find(x => String(x.id) === String(id))?.displayName ?? id)
            .join(', ');
    }

    // Навигация: передаём ТОЛЬКО DTO версии в state и идём по маршруту /versions/:versionId/edit
    editVersion(view: VersionView) {
        const v = view.version;
        const versionId = v.versionId;
        if (!versionId) {
            console.warn('editVersion: missing versionId on DTO', v);
            return;
        }

        this.router.navigate(['/versions', versionId, 'edit'], {
            state: { editorVersionDto: v, wasDraft: v.isPublished === false }
        });
    }

    publishVersion(view: VersionView) {
        const versionKey = view.versionKey;
        if (!versionKey) return;
        this.api.publishVersion(versionKey).subscribe({
            next: () => {
                //cause signalr will send us message
            },
            error: err => console.error(err)
        });
    }

    deleteVersion(view: VersionView) {
        const versionKey = view.versionKey;
        const problemId = view.version.problemId
        if (!versionKey) return;
        if (!confirm('Удалить версию?')) return;
        this.api.deleteVersion(problemId, versionKey).subscribe({
            next: () => {
                //cause signalr will send us message
            },
            error: err => console.error(err)
        });
    }

    onOpenVersion(view: VersionView) {
        const v = view.version;
        const mappedSupported: LanguageDto[] = [];
        const supportedIds = v.supportedLanguages ?? [];
        for (const id of supportedIds) {
            const found = this.languages.find(x => String(x.id) === String(id));
            if (found) mappedSupported.push(found);
            else mappedSupported.push({ id, code: String(id), displayName: String(id) } as LanguageDto);
        }

        const state = {
            versionId: view.versionKey,
            slug: view.problem?.slug,
            statement: v.statement,
            supportedLanguages: mappedSupported,
            title: view.problem?.title ?? ('Problem ' + v.problemId)
        };

        this.router.navigate(['/code-editor'], { state });
    }
}
