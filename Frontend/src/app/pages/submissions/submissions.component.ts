// src/app/pages/submissions/submissions.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { SidebarComponent } from '../../components/shared/sidebar.component';

@Component({
    selector: 'app-submissions',
    standalone: true,
    imports: [CommonModule, RouterModule, SidebarComponent],
    templateUrl: './submissions.component.html'
})
export class SubmissionsComponent implements OnInit {
    subs: any[] = [];

    constructor(private api: ApiService) { }

    ngOnInit() { this.load(); }

    load() { this.api.getSubmissions().subscribe(r => this.subs = r || []); }

    delete(id: string) {
        this.api.deleteSubmission(id).subscribe(() => this.load());
    }
}
