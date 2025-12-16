import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';


@Component({ selector: 'app-submission-detail', templateUrl: './submission-detail.component.html', imports: [CommonModule, FormsModule, SidebarComponent] })
export class SubmissionDetailComponent implements OnInit {
    submission: any = null;
    constructor(private route: ActivatedRoute, private api: ApiService, private router: Router) { }


    ngOnInit() {
        const id = this.route.snapshot.paramMap.get('id')!;
        this.api.getSubmission(id).subscribe(s => this.submission = s);
    }


    edit() {
        // открыть редактор кода, передать problemSlug и versionId
        this.router.navigate(['/code-editor'], { queryParams: { slug: this.submission.problemSlug, versionId: this.submission.versionId } });
    }
}