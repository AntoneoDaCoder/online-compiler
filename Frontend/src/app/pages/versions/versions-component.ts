// src/app/pages/versions/versions-component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
    selector: 'app-versions',
    standalone: true,
    templateUrl: './versions-component.html',
    imports: [SidebarComponent, CommonModule]
})
export class VersionsComponent implements OnInit {
    versions: any[] = [];
    filtered: any[] = [];
    filterLang: string | null = null;
    filterStatus: 'all' | 'published' | 'unpublished' = 'all';
    searchSlug = '';

    constructor(
        private api: ApiService,
        private auth: AuthService,
        private router: Router
    ) { }

    ngOnInit() { this.load(); }

    load() {
        this.api.getVersions().subscribe(v => {
            this.versions = v || [];
            this.applyFilters();
        });
    }

    applyFilters() {
        // простая заготовка — вы добавите фильтры
        this.filtered = this.versions.filter(x => true);
    }

    // метод для кнопки \"Изменить\" в шаблоне
    editVersion(version: any) {
        const versionId = version?.versionId ?? version?.id;
        const problemId = version?.problemId ?? version?.problemId;
        if (problemId && versionId) {
            // маршрут: /problems/:problemId/versions/:versionId/edit
            this.router.navigate(['/problems', problemId, 'versions', versionId, 'edit']);
            return;
        }
        if (problemId) {
            // если нет versionId, перейти в общий редактор задачи
            this.router.navigate(['/problems', problemId, 'edit']);
            return;
        }
        console.warn('Cannot navigate to edit: missing problemId/versionId', version);
    }

    publishVersion(versionId: string) {
        if (!versionId) return;
        this.api.publishVersion(versionId).subscribe(() => this.load(), err => console.error(err));
    }

    deleteVersion(versionId: string) {
        if (!versionId) return;
        if (!confirm('Удалить версию?')) return;
        this.api.deleteVersion(versionId).subscribe(() => this.load(), err => console.error(err));
    }
}
