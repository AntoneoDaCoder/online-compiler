import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { LanguageDto } from '../../core/models/dtos';
import { parse, v4 as uuidv4 } from 'uuid';
import { firstValueFrom, Subscription } from 'rxjs';
import { SignalrService } from '../../core/services/signalr.service';

@Component({
    selector: 'app-code-editor',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './code-editor.component.html',
    styleUrls: ['./code-editor.component.scss']
})
export class CodeEditorComponent implements OnInit, OnDestroy {
    @Input() problemSlug = '';
    @Input() problemVersionId = '';
    @Input() supportedLanguages: LanguageDto[] = [];
    @Input() statement: string | null = null;
    @Input() title: string = '';

    selectedLang = '';
    code = '';
    result = '';

    private codeResponseSubscription = new Subscription();

    loading = false;
    availableLangs: LanguageDto[] = [];

    constructor
        (private api: ApiService,
            private route: ActivatedRoute,
            private router: Router,
            private signalr: SignalrService) { }

    get displayHeading(): string {
        // Формат: Task-<slug>. <Title>
        const slugPart = this.problemSlug;
        const titlePart = this.title;
        return [slugPart, titlePart].filter(x => x).join('. ');
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
                    this.availableLangs = arr as LanguageDto[];
                    this.supportedLanguages = this.availableLangs;
                } else {
                    const all = await firstValueFrom(this.api.getLanguages());
                    const mapped: LanguageDto[] = [];
                    for (const id of arr) {
                        const found = (all || []).find(x => String(x.id) === String(id));
                        if (found) mapped.push(found);
                        else mapped.push({ id, code: String(id), displayName: String(id) } as LanguageDto);
                    }
                    this.availableLangs = mapped;
                    this.supportedLanguages = mapped;
                }
                if (navState.solutionLanguage)
                    this.selectedLang = navState.solutionLanguage
                else
                    this.selectedLang = this.availableLangs[0]?.code ?? '';
            }
        }

        this.route.queryParams.subscribe(async params => {
            if (params['slug']) this.problemSlug = params['slug'];
            if (params['versionId']) {
                this.problemVersionId = params['versionId'];
                if (!this.statement || !this.availableLangs.length) {
                    await this.loadVersionAndLanguages(this.problemVersionId);
                }
            }
        });

        if (this.problemVersionId && (!this.statement || !this.availableLangs.length)) {
            await this.loadVersionAndLanguages(this.problemVersionId);
        }

        this.codeResponseSubscription.add(
            this.signalr.onCodeResponse$.subscribe(response => {

                const parseDateTimeOffset = (dtOffset: any): Date => {
                    if (!dtOffset) return new Date();

                    // Если это уже строка (ISO)
                    if (typeof dtOffset === 'string') {
                        return new Date(dtOffset);
                    }

                    // Если это объект DateTimeOffset
                    if (typeof dtOffset === 'object' && dtOffset.DateTime) {
                        // Комбинируем DateTime и Offset
                        let dateStr = dtOffset.DateTime;

                        // Добавляем смещение если есть
                        if (dtOffset.Offset) {
                            dateStr += dtOffset.Offset;
                        } else {
                            dateStr += 'Z'; // UTC по умолчанию
                        }

                        return new Date(dateStr);
                    }

                    console.error('Неизвестный формат даты:', dtOffset);
                    return new Date();
                };

                const requestSentAt = parseDateTimeOffset(response.result.requestSentAt);
                const responseSentAt = parseDateTimeOffset(response.result.responseSentAt);

                const elapsedSeconds = (responseSentAt.getTime() - requestSentAt.getTime()) / 1000;

                this.result = `Status: ${response.result.status}\n\r` +
                    `Tests passed: ${response.result.passedTests}/${response.result.totalTests}\n\r` +
                    `Elapsed time: ${elapsedSeconds} sec\n\r`
                if (response.result.consoleOutput !== null)
                    this.result += `Console output: \n\r${response.result.consoleOutput}`
            }
            )
        )
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
                this.availableLangs = mapped;
                this.supportedLanguages = mapped;
                this.selectedLang = mapped[0]?.code ?? this.selectedLang;
            }
        } catch (err) {
            console.error('Failed to load version or languages', err);
        } finally {
            this.loading = false;
        }
    }

    onLangChange() {
        this.code = '';
    }

    onSubmit() {
        if (!this.selectedLang || !this.problemVersionId) {
            this.result = 'Выберите язык и убедитесь, что версия задачи загружена.';
            return;
        }

        const dto = {
            requestId: uuidv4(),
            problemSlug: this.problemSlug,
            problemVersionId: this.problemVersionId,
            languageCode: this.selectedLang,
            code: this.code,
            requestSentAt: new Date().toISOString()
        };

        this.api.submitCode(dto).subscribe({
            next: res => { this.result = "Awaiting server's response...." },
            error: err => { this.result = 'Error: ' + (err?.message ?? JSON.stringify(err)); }
        });
    }

    ngOnDestroy() {
        this.codeResponseSubscription.unsubscribe();
    }
}
