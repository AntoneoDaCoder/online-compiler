import { Component, Input, OnDestroy, OnInit, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { LanguageDto } from '../../core/models/dtos';
import { v4 as uuidv4 } from 'uuid';
import { firstValueFrom, Subscription } from 'rxjs';
import { SignalrService } from '../../core/services/signalr.service';
import { MonacoEditorModule } from 'ngx-monaco-editor-v2';

@Component({
    selector: 'app-code-editor',
    standalone: true,
    imports: [CommonModule, FormsModule, MonacoEditorModule],
    templateUrl: './code-editor.component.html',
    styleUrls: ['./code-editor.component.scss']
})
export class CodeEditorComponent implements OnInit, OnDestroy {
    @Input() problemSlug = '';
    @Input() problemVersionId = '';
    @Input() supportedLanguages: LanguageDto[] = [];
    @Input() statement: string | null = null;
    @Input() title: string = '';

    selectedLangId: string | null = null;
    disableSelectors = false;
    code = '';
    result = '';

    editor: any;

    editorOptions = {
        theme: 'vs-dark',
        language: 'plaintext',
        automaticLayout: true,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        fontSize: 14,
        wordWrap: 'on'
    };

    private codeResponseSubscription = new Subscription();
    loading = false;

    constructor(
        private api: ApiService,
        private route: ActivatedRoute,
        private router: Router,
        private signalr: SignalrService,
        private ngZone: NgZone
    ) { }

    get displayHeading(): string {
        return [this.problemSlug, this.title].filter(x => x).join('. ');
    }

    async ngOnInit() {
        const navState = this.router.currentNavigation()?.extras?.state ?? (history && (history.state || {}));
        if (navState) {
            if (navState.slug) this.problemSlug = navState.slug;
            if (navState.versionId) this.problemVersionId = navState.versionId;
            if (navState.statement) this.statement = navState.statement;
            if (navState.title) this.title = navState.title;

            if (navState.supportedLanguages) {
                const arr = navState.supportedLanguages as any[];
                if (arr.length && typeof arr[0] === 'object' && ('displayName' in arr[0] || 'code' in arr[0])) {
                    this.supportedLanguages = arr as LanguageDto[];
                } else {
                    const all = await firstValueFrom(this.api.getLanguages());
                    const mapped: LanguageDto[] = [];
                    for (const id of arr) {
                        const found = (all || []).find(x => String(x.id) === String(id));
                        if (found) mapped.push(found);
                        else mapped.push({ id, code: String(id), displayName: String(id) } as LanguageDto);
                    }
                    this.supportedLanguages = mapped;
                }

                this.supportedLanguages = this.supportedLanguages.sort((a, b) => a.code.localeCompare(b.code));

                if (navState.solutionLanguage) {
                    this.selectedLangId =
                        this.supportedLanguages.find(x => x.code === navState.solutionLanguage)?.id
                        ?? this.supportedLanguages[0]?.id
                        ?? null;
                } else {
                    this.selectedLangId = this.supportedLanguages[0]?.id ?? null;
                }
            }

            if (navState.code) {
                this.code = navState.code;
                this.disableSelectors = true;
            } else if (this.selectedLangId) {
                this.loadTemplateForSelectedLanguage();
            } else {
                this.code = '';
            }
        }

        this.route.queryParams.subscribe(async params => {
            if (params['slug']) this.problemSlug = params['slug'];
            if (params['versionId']) {
                this.problemVersionId = params['versionId'];
                if (!this.statement || !this.supportedLanguages.length) {
                    await this.loadVersionAndLanguages(this.problemVersionId);
                }
            }
        });

        if (this.problemVersionId && (!this.statement || !this.supportedLanguages.length)) {
            await this.loadVersionAndLanguages(this.problemVersionId);
        }

        this.codeResponseSubscription.add(
            this.signalr.onCodeResponse$.subscribe(response => {
                this.ngZone.run(() => {
                    const parseDateTimeOffset = (dtOffset: any): Date => {
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
                    };

                    const requestSentAt = parseDateTimeOffset(response.result.requestSentAt);
                    const responseSentAt = parseDateTimeOffset(response.result.responseSentAt);
                    const elapsedSeconds = (responseSentAt.getTime() - requestSentAt.getTime()) / 1000;

                    this.result =
                        `Status: ${response.result.status}\n\r` +
                        `Tests passed: ${response.result.passedTests}/${response.result.totalTests}\n\r` +
                        `Elapsed time: ${elapsedSeconds} sec\n\r`;

                    if (response.result.consoleOutput !== null) {
                        this.result += `Console output: \n\r${response.result.consoleOutput}`;
                    }
                });
            })
        );
    }

    onEditorInit(editor: any) {
        this.editor = editor;
    }

    private getMonacoLanguageCode(): string {
        const lang = this.supportedLanguages.find(x => String(x.id) === String(this.selectedLangId));
        const code = (lang?.code || 'plaintext').toLowerCase();

        const map: Record<string, string> = {
            py: 'python',
            cpp: 'cpp',
            cxx: 'cpp',
            cc: 'cpp',
            cs: 'csharp',
            js: 'javascript',
            ts: 'typescript',
            md: 'markdown',
            yml: 'yaml'
        };

        return map[code] ?? code;
    }

    private applyMonacoLanguage(): void {
        const monacoLang = this.getMonacoLanguageCode();

        this.editorOptions = {
            ...this.editorOptions,
            language: monacoLang
        };

        if (this.editor?.getModel) {
            const model = this.editor.getModel();
            if (model && (window as any).monaco?.editor?.setModelLanguage) {
                (window as any).monaco.editor.setModelLanguage(model, monacoLang);
            }
        }
    }

    private async loadTemplateForSelectedLanguage() {
        if (!this.selectedLangId) {
            this.code = '';
            return;
        }

        this.applyMonacoLanguage();

        this.api.getCodeTemplate(this.problemVersionId, this.selectedLangId).subscribe({
            next: (response) => {
                this.code = response;
            },
            error: (error) => {
                console.log(error);
                this.code = '';
            }
        });
    }

    private async loadVersionAndLanguages(versionId: string) {
        this.loading = true;
        try {
            const dto = await firstValueFrom(this.api.getUserVersion(versionId));
            if (dto) {
                this.statement = dto.statement ?? this.statement;
                const langs = await firstValueFrom(this.api.getLanguages());
                const mapped: LanguageDto[] = [];
                const all = langs || [];

                for (const id of (dto.supportedLanguages ?? [])) {
                    const found = all.find(x => String(x.id) === String(id));
                    if (found) mapped.push(found);
                    else mapped.push({ id, code: String(id), displayName: String(id) } as LanguageDto);
                }

                this.supportedLanguages = mapped.sort((a, b) => a.code.localeCompare(b.code));
                this.selectedLangId = mapped[0]?.id ?? null;
                this.applyMonacoLanguage();
            }
        } catch (err) {
            console.error('Failed to load version or languages', err);
        } finally {
            this.loading = false;
        }
    }

    onLangChange() {
        this.loadTemplateForSelectedLanguage();
    }

    onSubmit() {
        if (!this.selectedLangId || !this.problemVersionId) {
            this.result = 'Выберите язык и убедитесь, что версия задачи загружена.';
            return;
        }

        const langCode = this.supportedLanguages.find(x => x.id === this.selectedLangId)?.code;

        const dto = {
            requestId: uuidv4(),
            problemSlug: this.problemSlug,
            problemVersionId: this.problemVersionId,
            languageCode: langCode,
            code: this.code,
            requestSentAt: new Date().toISOString()
        };

        this.api.submitCode(dto).subscribe({
            next: () => { this.result = "Awaiting server's response...."; },
            error: err => { this.result = 'Error: ' + (err?.message ?? JSON.stringify(err)); }
        });
    }

    ngOnDestroy() {
        this.codeResponseSubscription.unsubscribe();
    }
}