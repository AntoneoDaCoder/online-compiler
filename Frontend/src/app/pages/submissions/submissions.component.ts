// src/app/pages/submissions/submissions.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { ShortSubmissionDto, LanguageDto } from '../../core/models/dtos';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';


type ShowStatus = 'all' | 'passed' | 'failed';
type SortOrder = 'ascending' | 'descending';

interface SubmissionView {
    submission: ShortSubmissionDto;
    isPassed: boolean;
    created: Date;
}

@Component({
    selector: 'app-submissions',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './submissions.component.html'
}
)
export class SubmissionsComponent implements OnInit {
    languages: LanguageDto[] = [];
    submissions: ShortSubmissionDto[] = [];
    filtered: SubmissionView[] = [];

    filterLanguage = '';
    showOnlyStatus: ShowStatus = 'all';
    sortOrder: SortOrder = 'descending';


    private subs = new Subscription();

    constructor(private api: ApiService, private auth: AuthService, private router: Router) { }

    ngOnInit() {
        this.loadInitial();
    }

    private loadInitial() {
        this.subs.add(this.api.getLanguages().subscribe(l => this.languages = l || []));
        this.loadSubmissions();
    }

    private loadSubmissions() {
        this.subs.add(this.api.getSubmissions(this.auth.getId()).subscribe(s => {
            this.submissions = s || [];
            this.applyFilters();
        }, err => {
            console.error('Failed to load submissions', err);
            this.submissions = [];
            this.applyFilters();
        }));
    }



    applyFilters() {
        const mapToView = (s: ShortSubmissionDto): SubmissionView => {
            const isPassed = (s.passedTests === s.totalTests);
            return { submission: s, isPassed, created: this.parseDateTimeOffset(s.created) };
        };

        this.filtered = this.submissions
            .map(mapToView)
            .filter(v => {
                if (this.filterLanguage) {
                    if (String(v.submission.solutionLanguage) !== String(this.filterLanguage)) return false;
                }

                if (this.showOnlyStatus !== 'all') {
                    if (this.showOnlyStatus === 'passed' && !v.isPassed) return false;
                    if (this.showOnlyStatus === 'failed' && v.isPassed) return false;
                }

                return true;
            });

        switch(this.sortOrder)
        {
            case 'ascending':
                this.filtered = this.filtered.sort((a, b) => a.created.getTime() - b.created.getTime())
                break;
            case 'descending':
                this.filtered = this.filtered.sort((a, b) => b.created.getTime() - a.created.getTime())
                break;

            default:
                this.filtered = this.filtered.sort((a, b) => b.created.getTime() - a.created.getTime())
                break;
        }
    }

    private parseDateTimeOffset(dtOffset: any): Date {
        if (!dtOffset) return new Date();

        if (typeof dtOffset === 'string') {
            return new Date(dtOffset);
        }

        if (typeof dtOffset === 'object' && dtOffset.DateTime) {
            let dateStr = dtOffset.DateTime;
            dateStr += dtOffset.Offset ? dtOffset.Offset : 'Z';
            return new Date(dateStr);
        }

        return new Date();
    }


    clearFilters() {
        this.filterLanguage = '';
        this.showOnlyStatus = 'all';
        this.applyFilters();
    }

    formatLanguage(code?: string) {
        if (!code) return '';
        const found = this.languages.find(x => x.code === code || String(x.id) === String(code));
        return found?.displayName ?? code;
    }

    openSubmission(view: SubmissionView) {
        const id = view.submission.id;
        if (!id) return;
        this.router.navigate(['/submissions', id], {
            state: {
                submissionId: id,
                languageDisplayName: this.formatLanguage(view.submission.solutionLanguage)
            }
        });
    }

    deleteSubmission(view: SubmissionView) {
        const id = view.submission.id;
        if (!id) return;
        if (!confirm('Удалить сабмишн?')) return;
        this.api.deleteSubmission(this.auth.getId(), id).subscribe({
            next: () => {
                this.submissions = this.submissions.filter(s => s.id !== id);
                this.applyFilters();
            },
            error: err => console.error('Failed to delete submission', err)
        });
    }

    ngOnDestroy() {
        this.subs.unsubscribe();
    }
}
