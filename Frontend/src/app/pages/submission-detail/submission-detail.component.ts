import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { SidebarComponent } from '../../components/shared/sidebar.component';
import { SubmissionDto } from '../../core/models/dtos';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { firstValueFrom } from 'rxjs';

@Component({
    selector: 'app-submission-detail',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './submission-detail.component.html'
})
export class SubmissionDetailComponent implements OnInit {
    submission: SubmissionDto | null = null;

    private submissionId = '';
    langDisplayName = '';
    constructor(private api: ApiService,
        private auth: AuthService,
        private route: ActivatedRoute,
        private router: Router) { }

    ngOnInit() {
        const navState = this.router.currentNavigation()?.extras?.state ?? (history && (history.state || {}));
        if (navState && navState.submissionId && navState.languageDisplayName) {
            this.submissionId = navState.submissionId;
            this.langDisplayName = navState.languageDisplayName as string;
        } else {
            this.submissionId = this.route.snapshot.params['id'] ?? '';
        }

        if (this.submissionId) this.loadSubmission(this.submissionId);
    }

    private loadSubmission(id: string) {
        this.api.getSubmission(this.auth.getId(), id).subscribe({
            next: (dto) => this.submission = dto,
            error: err => console.error('Failed to load submission', err)
        });
    }

    async onEditVersion() {
        if (!this.submission) return;
        const versionId = this.submission.versionId;
        if (!versionId) return;

        try {
            const dto = await firstValueFrom(this.api.getUserVersion(versionId));

            const state = {
                slug: this.submission.problemSlug,
                versionId: dto.versionId,
                statement: dto.statement,
                supportedLanguages: dto.supportedLanguages,
                title: this.submission.title,
                solutionLanguage: this.submission.solutionLanguage,
                code: this.submission.solution
            };

            this.router.navigate(['/code-editor'], { state });
        } catch (err) {
            console.error('Failed to load version for edit', err);
        }
    }

    onDeleteSubmission() {
        if (!this.submission) return;
        if (!confirm('Удалить сабмишн?')) return;
        this.api.deleteSubmission(this.auth.getId(), this.submission.submissionId).subscribe({
            next: () => {
                this.router.navigate(['/submissions']);
            },
            error: err => console.error('Failed to delete submission', err)
        });
    }
}