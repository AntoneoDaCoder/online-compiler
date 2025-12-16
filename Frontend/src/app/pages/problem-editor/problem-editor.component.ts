// src/app/pages/problem-editor/problem-editor.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { EditorProblemVersionDto } from '../../core/models/dtos';

@Component({
    selector: 'app-problem-editor',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './problem-editor.component.html'
})
export class ProblemEditorComponent implements OnInit {
    problemId = '';
    versionId?: string | null;
    model: EditorProblemVersionDto = {
        problemId: '',
        createdAt: null,
        createdBy: null,
        statement: '',
        totalTests: 0,
        version: 0,
        supportedLanguages: [],
        testManifest: null
    };

    entryPoint = 'Solution';
    helpers: { id: string; language: string; code: string }[] = [{ id: crypto.randomUUID(), language: '', code: '' }];
    advTests: { name: string; language: string; source: string; timeoutMs?: number }[] = [];
    simpleTests: { name: string; type: 'primitive' | 'array'; primType?: string; values: any[]; expected?: any }[] = [];

    supportedLanguagesOptions: any[] = [];

    constructor(private route: ActivatedRoute, private api: ApiService, private router: Router) { }

    ngOnInit() {
        this.route.paramMap.subscribe(pm => {
            this.problemId = pm.get('problemId') || '';
            this.versionId = pm.get('versionId');
            if (this.versionId) this.loadVersion(this.versionId);
        });
    }

    loadVersion(versionId: string) {
        this.api.getEditorVersion(versionId).subscribe(dto => {
            this.model = dto;
            // распарсить манифест в local arrays если нужно
        });
    }

    addHelper() {
        this.helpers.push({ id: crypto.randomUUID(), language: '', code: '' });
    }

    removeHelper(id: string) {
        if (this.helpers.length <= 1) {
            // если нужно запретить удаление последнего — оставляем так
            return;
        }
        this.helpers = this.helpers.filter(h => h.id !== id);
    }

    addSimpleTest() {
        this.simpleTests.push({ name: '', type: 'primitive', primType: 'string', values: [], expected: '' });
    }

    removeSimpleTest(i: number) { this.simpleTests.splice(i, 1); }

    addArrayValue(t: any) {
        if (!t.values) t.values = [];
        t.values.push('');
    }

    addAdvTest() { this.advTests.push({ name: '', language: '', source: '', timeoutMs: 1000 }); }

    saveDraft() {
        const dto: any = {
            statement: this.model.statement,
            totalTests: this.simpleTests.length + this.advTests.length,
            testManifest: { helpers: this.helpers, advTests: this.advTests, simpleTests: this.simpleTests }
        };

        this.api.saveProblemVersion(this.problemId, dto).subscribe({
            next: () => this.router.navigate(['/versions']),
            error: err => console.error(err)
        });
    }

    cancel() {
        this.router.navigate(['/tasks']);
    }
}
