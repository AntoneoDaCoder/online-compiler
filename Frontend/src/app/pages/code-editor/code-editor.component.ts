// src/app/pages/code-editor/code-editor.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { LanguageDto } from '../../core/models/dtos';
import { v4 as uuidv4 } from 'uuid';

@Component({
    selector: 'app-code-editor',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './code-editor.component.html'
})
export class CodeEditorComponent {
    @Input() problemSlug = '';
    @Input() problemVersionId = '';
    @Input() supportedLanguages: LanguageDto[] = [];
    @Input() statement: string | null = null; // <-- сюда передаётся условие

    selectedLang = '';
    code = '';
    result = '';

    constructor(private api: ApiService) { }

    onLangChange() {
        // очистить поле при смене языка
        this.code = '';
    }

    onSubmit() {
        if (!this.selectedLang || !this.problemVersionId) return;

        const dto = {
            requestId: uuidv4(),
            problemSlug: this.problemSlug,
            problemVersionId: this.problemVersionId,
            languageCode: this.selectedLang,
            code: this.code,
            requestSentAt: new Date().toISOString()
        };

        this.api.submitCode(dto).subscribe({
            next: (res) => { this.result = JSON.stringify(res, null, 2); },
            error: (err) => { this.result = 'Error: ' + (err?.message ?? JSON.stringify(err)); }
        });
    }
}
