import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { EditorProblemVersionDto, LanguageDto } from '../../core/models/dtos';

/* --- Вспомогательные типы --- */
type HelperItem = { id: string; languageCode: string; inline?: string };
type AdvTestItem = { id: string; name: string; languageCode: string; source: string; timeoutMs?: number };

type ParamDescriptor = {
    name: string;
    typeDescriptor: TypeDescriptor | null;
    // ui-only
    uiType: 'primitive' | 'array';
    uiPrim?: 'bool' | 'string' | 'int' | 'double';
};

type SimpleTestItem = {
    id: string;
    name: string;
    // inputs: each index corresponds to signature parameter; value is either primitive or array (for array param)
    inputs: any[]; // e.g. [ 1, [1,2], "abc" ]
    expected: any;
    comparator: string;
    // ui state
    showInputsEditor?: boolean;
    generatedName?: string; // tracked generated name to decide update on comparator change
    invalidInputs?: boolean;
    invalidName?: boolean;
    invalidValues?: boolean[]; // per parameter (for arrays can be more complex, we use this as flag)
};

type TypeDescriptor = {
    kind: string; // e.g. 'primitive' | 'array' | others
    name?: string | null; // primitive name like 'int','double','string','bool'
    items?: TypeDescriptor | null;
    of?: TypeDescriptor | null;
};

/* --- Компонент --- */
@Component({
    selector: 'app-problem-editor',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule],
    templateUrl: './problem-editor.component.html',
    styleUrls: ['./problem-editor.component.scss']
})
export class ProblemEditorComponent implements OnInit {
    slug = '';
    problemId = '';

    model: EditorProblemVersionDto = {
        problemId: '',
        versionId: '',
        createdAt: null,
        createdBy: null,
        statement: '',
        totalTests: 0,
        version: 0,
        supportedLanguages: [],
        isPublished: false,
        testManifest: null
    };

    entryPoint = 'Solution';
    helpers: HelperItem[] = [];
    advTests: AdvTestItem[] = [];
    simpleTests: SimpleTestItem[] = [];

    // signature parameters for the function we are testing
    parameters: ParamDescriptor[] = [];
    returnType: TypeDescriptor | null = null;

    supportedLanguagesOptions: LanguageDto[] = [];
    isSaving = false;
    wasDraft = false;
    errors: string[] = [];

    comparatorOptions = [
        { value: 'seq_eq', label: 'sequence equals (seq_eq)' },
        { value: 'seq_eq_sorted', label: 'sequence equals sorted (seq_eq_sorted)' },
        { value: 'contains', label: 'contains' },
        { value: 'eq', label: 'equals (eq)' },
        { value: 'neq', label: 'not equals (neq)' }
    ];

    constructor(
        private route: ActivatedRoute,
        private api: ApiService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.api.getLanguages().subscribe({
            next: langs => this.supportedLanguagesOptions = langs || [],
            error: err => console.error('Failed to load languages', err)
        });


        const nav = this.router.currentNavigation?.();
        const state =
            (nav?.extras?.state as { [key: string]: any } | null) ??
            (history?.state as { [key: string]: any } | null);

        const stateDto = state?.['editorVersionDto'] as EditorProblemVersionDto | undefined;

        if (stateDto) {
            this.model = stateDto;
            this.wasDraft = state?.['wasDraft'] as boolean;
            this.problemId = stateDto.problemId ?? this.problemId;
            this.entryPoint = stateDto.testManifest?.entrypoint ?? this.entryPoint;
            this.parseManifest(stateDto.testManifest);
            return;
        }


        this.route.paramMap.subscribe(pm => {
            this.slug = pm.get('slug') ?? '';
            if (this.slug) {
                this.api.getEditorVersion(this.slug).subscribe({
                    next: dto => {
                        if (!dto) {
                            this.resetEditorState();
                            return;
                        }
                        this.model = dto;
                        console.log(this.model);
                        this.entryPoint = dto.testManifest?.entrypoint ?? 'Solution';
                        this.parseManifest(dto.testManifest);
                    },
                    error: err => {
                        console.error('Failed to load editor version', err);
                        this.resetEditorState();
                    }
                });
            } else {
                this.resetEditorState();
            }
        });
    }

    private newId(): string {
        try { return crypto.randomUUID(); } catch { return Math.random().toString(36).slice(2); }
    }

    private resetEditorState() {
        this.model = {
            problemId: this.problemId || '',
            versionId: '',
            createdAt: null,
            createdBy: null,
            isPublished: false,
            statement: '',
            totalTests: 0,
            version: 0,
            supportedLanguages: [],
            testManifest: null
        };
        this.entryPoint = 'Solution';

        // !!! теперь по умолчанию пустые коллекции (пользователь добавит карточки вручную)
        this.helpers = [];
        this.advTests = [];

        // сигнатура / возвращаемый тип — пустые по умолчанию (можно задать default)
        this.parameters = [];
        this.returnType = { kind: 'primitive', name: 'string' };

        // минимум одна sample-карточка всё равно оставляем, т.к. она обязательна для сохранения
        this.simpleTests = [this.createEmptySampleTest(0)];

        this.errors = [];
    }


    /* ---------- Naming helpers ---------- */
    private generateSampleName(comparator: string, index: number) {
        return `t_sample_${comparator}_${index + 1}`;
    }
    private generateAdvName(index: number) {
        return `adv_test_${index + 1}`;
    }

    /* ---------- TypeDescriptor helpers ---------- */
    private resolveTypeDesc(td?: TypeDescriptor | null): { uiType: 'primitive' | 'array', prim?: 'bool' | 'string' | 'int' | 'double' } {
        if (!td) return { uiType: 'primitive', prim: 'string' };
        // If it's an array kind → look into items/of to find primitive
        const kind = td.kind?.toLowerCase() ?? '';
        if (kind === 'array' || kind === 'list' || kind === 'sequence') {
            const inner = td.items ?? td.of ?? null;
            const innerName = inner?.name?.toLowerCase() ?? inner?.kind?.toLowerCase();
            if (innerName?.includes('int')) return { uiType: 'array', prim: 'int' };
            if (innerName?.includes('double') || innerName?.includes('float')) return { uiType: 'array', prim: 'double' };
            if (innerName?.includes('bool')) return { uiType: 'array', prim: 'bool' };
            return { uiType: 'array', prim: 'string' };
        } else {
            const name = (td.name ?? '').toLowerCase();
            if (name.includes('int')) return { uiType: 'primitive', prim: 'int' };
            if (name.includes('double') || name.includes('float')) return { uiType: 'primitive', prim: 'double' };
            if (name.includes('bool')) return { uiType: 'primitive', prim: 'bool' };
            return { uiType: 'primitive', prim: 'string' };
        }
    }

    /* ---------- Create empty sample test (uses current parameters/returnType) ---------- */
    private createEmptySampleTest(idxForName = -1): SimpleTestItem {
        const inputs = this.parameters.map(pd => pd.uiType === 'array' ? [''] : '');
        const comparator = this.comparatorOptions[0].value;
        const generatedName = this.generateSampleName(comparator, (idxForName >= 0 ? idxForName : (this.simpleTests?.length ?? 0)));
        return {
            id: this.newId(),
            name: generatedName,
            inputs,
            expected: (this.returnType ? (this.resolveTypeDesc(this.returnType).uiType === 'array' ? [''] : '') : ''),
            comparator,
            showInputsEditor: false,
            generatedName,
            invalidInputs: false,
            invalidName: false,
            invalidValues: []
        };
    }

    /* ---------- Parse manifest + signature + tests ---------- */
    private parseManifest(manifest: any | null) {
        // reset ui containers
        this.helpers = [];
        this.advTests = [];
        this.simpleTests = [];
        this.parameters = [];
        this.returnType = null;

        if (!manifest) {
            // defaults
            this.helpers.push({ id: this.newId(), languageCode: this.supportedLanguagesOptions[0]?.code ?? '', inline: '' });
            this.advTests.push({ id: this.newId(), name: this.generateAdvName(0), languageCode: this.supportedLanguagesOptions[0]?.code ?? '', source: '', timeoutMs: 1000 });
            this.parameters = []; // none
            this.returnType = { kind: 'primitive', name: 'string' };
            this.simpleTests.push(this.createEmptySampleTest(0));
            return;
        }

        // ENTRYPOINT handled elsewhere
        // --- signature ---
        const sig = manifest.signature ?? null;
        if (sig && sig.parameters && Array.isArray(sig.parameters)) {
            this.parameters = sig.parameters.map((p: any) => {
                const td: TypeDescriptor | null = p.type ?? p.typeDescriptor ?? null;
                const resolved = this.resolveTypeDesc(td);
                return {
                    name: p.name ?? 'p',
                    typeDescriptor: td,
                    uiType: resolved.uiType,
                    uiPrim: resolved.prim
                } as ParamDescriptor;
            });
            this.returnType = sig.returnType ?? null;
        } else {
            // no signature in manifest: fallback to none
            this.parameters = [];
            this.returnType = manifest.returnType ?? { kind: 'primitive', name: 'string' };
        }

        // --- helpers ---
        if (Array.isArray(manifest.helpers)) {
            for (const h of manifest.helpers) {
                this.helpers.push({ id: this.newId(), languageCode: h.languageCode ?? h.language ?? this.supportedLanguagesOptions[0]?.code ?? '', inline: h.inline ?? h.code ?? '' });
            }
        } else if (manifest.helpers && typeof manifest.helpers === 'object') {
            this.helpers.push({ id: this.newId(), languageCode: manifest.helpers.languageCode ?? this.supportedLanguagesOptions[0]?.code ?? '', inline: manifest.helpers.inline ?? '' });
        }
        // --- adv tests ---
        const advs = manifest.advTests ?? manifest.advancedTests ?? [];
        if (Array.isArray(advs) && advs.length) {
            let idx = 0;
            for (const a of advs) {
                this.advTests.push({
                    id: this.newId(),
                    name: a.name ?? this.generateAdvName(idx),
                    languageCode: a.languageCode ?? a.language ?? this.supportedLanguagesOptions[0]?.code ?? '',
                    source: a.source ?? a.body ?? '',
                    timeoutMs: a.timeoutMs ?? a.timeout ?? 1000
                });
                idx++;
            }
        }
        // --- sample tests ---
        const simple = manifest.sampleTests ?? manifest.simpleTests ?? manifest.tests ?? [];
        if (Array.isArray(simple) && simple.length) {
            for (let si = 0; si < simple.length; si++) {
                const t = simple[si];
                // inputs may be:
                // - a single primitive: inputs = <value>
                // - an array of values for single param: inputs = [ ... ]
                // - an array of inputs for multiple parameters: inputs = [ inputForParam0, inputForParam1, ... ]
                const inputsRaw = t.inputs ?? t.inputs ?? t.values ?? [];
                const inputsForUi: any[] = [];

                // if signature has parameters, map accordingly
                if (this.parameters.length > 0) {
                    // if inputsRaw is array and length equals parameters -> assume per-param
                    if (Array.isArray(inputsRaw) && inputsRaw.length === this.parameters.length) {
                        for (let pIdx = 0; pIdx < this.parameters.length; pIdx++) {
                            const pd = this.parameters[pIdx];
                            const val = inputsRaw[pIdx];
                            if (pd.uiType === 'array') {
                                // ensure array
                                if (Array.isArray(val)) {
                                    // convert primitives to strings where appropriate to bind to inputs
                                    inputsForUi[pIdx] = val.map((x: any) => (typeof x === 'object' ? x : x));
                                } else {
                                    // sometimes manifest can have single value; wrap
                                    inputsForUi[pIdx] = Array.isArray(val) ? val.slice() : [val];
                                }
                            } else {
                                // primitive
                                inputsForUi[pIdx] = val;
                            }
                        }
                    } else {
                        // If inputsRaw is an array but doesn't match number of params, try to distribute:
                        // If there is exactly one param, use inputsRaw as param value (wrap if needed).
                        if (this.parameters.length === 1) {
                            const pd = this.parameters[0];
                            if (pd.uiType === 'array') {
                                inputsForUi[0] = Array.isArray(inputsRaw) ? inputsRaw.slice() : [inputsRaw];
                            } else {
                                // single primitive — take first element or raw
                                inputsForUi[0] = Array.isArray(inputsRaw) ? inputsRaw[0] : inputsRaw;
                            }
                        } else {
                            // fallback: create empty inputs matching types
                            inputsForUi.push(...this.parameters.map(pd => pd.uiType === 'array' ? [''] : ''));
                        }
                    }
                } else {
                    // no signature parameters — fallback to previous behaviour: single input
                    if (Array.isArray(inputsRaw) && inputsRaw.length === 1) {
                        inputsForUi[0] = inputsRaw[0];
                    } else if (Array.isArray(inputsRaw) && inputsRaw.length > 1) {
                        // unknown: treat whole inputsRaw as a single array input
                        inputsForUi[0] = inputsRaw.slice();
                    } else {
                        inputsForUi[0] = inputsRaw;
                    }
                }

                // expected parsing — try to respect returnType (array or primitive)
                let expectedUi: any;
                if (this.returnType) {
                    const r = this.resolveTypeDesc(this.returnType);
                    if (r.uiType === 'array') {
                        expectedUi = Array.isArray(t.expected) ? t.expected.slice() : (Array.isArray(t.expected) ? t.expected.slice() : ['']);
                    } else {
                        expectedUi = t.expected ?? '';
                    }
                } else {
                    expectedUi = t.expected ?? '';
                }

                const comparator = t.comparator ?? this.comparatorOptions[0].value;
                const generatedName = this.generateSampleName(comparator, si);

                this.simpleTests.push({
                    id: this.newId(),
                    name: t.name ?? generatedName,
                    inputs: inputsForUi.length ? inputsForUi : (this.parameters.map(pd => pd.uiType === 'array' ? [''] : '')),
                    expected: expectedUi,
                    comparator,
                    showInputsEditor: false,
                    generatedName,
                    invalidInputs: false,
                    invalidName: false,
                    invalidValues: []
                });
            }
        } else {
            // default single sample test
            this.simpleTests.push(this.createEmptySampleTest(0));
        }
    }

    /* ---------- Helpers for UI actions ---------- */
    addHelper() { this.helpers.push({ id: this.newId(), languageCode: this.supportedLanguagesOptions[0]?.code ?? '', inline: '' }); }
    removeHelper(id: string) { if (this.helpers.length <= 1) return; this.helpers = this.helpers.filter(h => h.id !== id); }

    addAdvTest() { this.advTests.push({ id: this.newId(), name: this.generateAdvName(this.advTests.length), languageCode: this.supportedLanguagesOptions[0]?.code ?? '', source: '', timeoutMs: 1000 }); }
    removeAdvTest(i: number) { if (this.advTests.length <= 1) return; if (i >= 0 && i < this.advTests.length) this.advTests.splice(i, 1); }

    addSimpleTest() {
        const st = this.createEmptySampleTest(this.simpleTests.length);
        this.simpleTests.push(st);
    }
    removeSimpleTest(i: number) { if (this.simpleTests.length <= 1) return; if (i >= 0 && i < this.simpleTests.length) this.simpleTests.splice(i, 1); }

    // When comparator changes update generated name if user didn't rename manually
    onComparatorChange(t: SimpleTestItem, newComparator: string, idx: number) {
        const newGen = this.generateSampleName(newComparator, idx);
        if (!t.name || t.name === t.generatedName) {
            t.name = newGen;
            t.generatedName = newGen;
        } else {
            // If user changed name, don't overwrite; but still update generatedName so future changes follow logic
            t.generatedName = newGen;
        }
    }

    toggleInputsEditor(t: SimpleTestItem) {
        t.showInputsEditor = !t.showInputsEditor;
        if (t.showInputsEditor) {
            // ensure inputs array exists and matches parameters length
            if (!Array.isArray(t.inputs)) t.inputs = [];
            // extend to match parameters
            for (let pi = 0; pi < this.parameters.length; pi++) {
                const pd = this.parameters[pi];
                if (typeof t.inputs[pi] === 'undefined') {
                    t.inputs[pi] = pd.uiType === 'array' ? [''] : '';
                } else {
                    // normalize scalar->array when parameter became array
                    if (pd.uiType === 'array' && !Array.isArray(t.inputs[pi])) {
                        t.inputs[pi] = [t.inputs[pi] ?? ''];
                    }
                    // array->scalar
                    if (pd.uiType === 'primitive' && Array.isArray(t.inputs[pi])) {
                        t.inputs[pi] = t.inputs[pi][0] ?? '';
                    }
                }
            }

            // ensure expected exists if returnType is array
            if (this.returnType?.kind === 'array' && !Array.isArray(t.expected)) {
                t.expected = [''];
            }
            // for primitive expected, ensure non-null
            if (this.returnType && this.returnType.kind !== 'array' && (t.expected === undefined)) {
                t.expected = '';
            }
        }
    }


    /* ---------- Parameter (signature) management ---------- */
    addParameter() {
        const idx = this.parameters.length;
        const defaultTd: TypeDescriptor = { kind: 'primitive', name: 'string' };
        const resolved = this.resolveTypeDesc(defaultTd);
        this.parameters.push({
            name: `p${idx + 1}`,
            typeDescriptor: defaultTd,
            uiType: resolved.uiType,
            uiPrim: resolved.prim
        });
        // propagate inputs structure to existing sample tests
        for (const t of this.simpleTests) {
            t.inputs.push(this.parameters[this.parameters.length - 1].uiType === 'array' ? [''] : '');
        }
    }

    removeParameter(idx: number) {
        if (idx < 0 || idx >= this.parameters.length) return;
        this.parameters.splice(idx, 1);
        // remove corresponding input from tests
        for (const t of this.simpleTests) {
            t.inputs.splice(idx, 1);
        }
    }

    // change parameter type from UI
    onParameterTypeChange(idx: number, newTd: TypeDescriptor) {
        const pd = this.parameters[idx];
        const resolved = this.resolveTypeDesc(newTd);
        pd.typeDescriptor = newTd;
        pd.uiType = resolved.uiType;
        pd.uiPrim = resolved.prim;
        // adjust all tests' inputs at this index to match new uiType
        for (const t of this.simpleTests) {
            if (pd.uiType === 'array') {
                if (!Array.isArray(t.inputs[idx])) t.inputs[idx] = [''];
            } else {
                if (Array.isArray(t.inputs[idx])) t.inputs[idx] = t.inputs[idx][0] ?? '';
            }
        }
    }

    /* ---------- Array element operations for a given test and parameter index ---------- */
    addArrayElementForParam(t: SimpleTestItem, paramIdx: number, primType?: string) {
        if (!Array.isArray(t.inputs[paramIdx])) t.inputs[paramIdx] = [];
        t.inputs[paramIdx].push(primType === 'bool' ? false : '');
    }
    removeArrayElementForParam(t: SimpleTestItem, paramIdx: number, elIdx: number) {
        if (!Array.isArray(t.inputs[paramIdx])) return;
        if (t.inputs[paramIdx].length <= 1) return;
        t.inputs[paramIdx].splice(elIdx, 1);
    }

    /* ---------- Expected array element ops (return value) ---------- */
    addExpectedElement(t: SimpleTestItem) {
        if (!Array.isArray(t.expected)) t.expected = [];
        t.expected.push('');
    }
    removeExpectedElement(t: SimpleTestItem, idx: number) {
        if (!Array.isArray(t.expected)) return;
        if (t.expected.length <= 1) return;
        t.expected.splice(idx, 1);
    }

    /* ---------- Validation ---------- */
    private resetValidationFlags() {
        this.errors = [];
        for (const t of this.simpleTests) {
            t.invalidInputs = false;
            t.invalidName = false;
            t.invalidValues = [];
        }
    }

    validateBeforeSave(): boolean {
        this.resetValidationFlags();
        if (!this.model.statement || this.model.statement.trim().length < 5) {
            this.errors.push('Условие задачи должно быть заполнено (минимум 5 символов).');
        }

        let hasValidSample = false;
        for (let si = 0; si < this.simpleTests.length; si++) {
            const t = this.simpleTests[si];
            if (!t.name || !t.name.trim()) {
                t.invalidName = true;
                this.errors.push(`Sample тест #${si + 1} — имя не заполнено.`);
                continue;
            }
            // validate inputs per signature
            for (let pIdx = 0; pIdx < this.parameters.length; pIdx++) {
                const pd = this.parameters[pIdx];
                const val = t.inputs[pIdx];
                if (pd.uiType === 'primitive') {
                    if (val === null || typeof val === 'undefined' || (typeof val === 'string' && val.trim() === '')) {
                        t.invalidInputs = true;
                        this.errors.push(`Sample "${t.name}" — входной параметр "${pd.name}" не задан.`);
                    } else if (pd.uiPrim === 'int') {
                        const parsed = Number(val);
                        if (Number.isNaN(parsed) || !Number.isInteger(parsed)) {
                            t.invalidInputs = true;
                            this.errors.push(`Sample "${t.name}" — параметр "${pd.name}" должен быть int.`);
                        }
                    } else if (pd.uiPrim === 'double') {
                        const parsed = Number(val);
                        if (Number.isNaN(parsed)) {
                            t.invalidInputs = true;
                            this.errors.push(`Sample "${t.name}" — параметр "${pd.name}" должен быть число.`);
                        }
                    }
                } else { // array param
                    if (!Array.isArray(val) || val.length === 0) {
                        t.invalidInputs = true;
                        this.errors.push(`Sample "${t.name}" — параметр "${pd.name}" (массив) пуст.`);
                    } else {
                        // check each element
                        for (let ei = 0; ei < val.length; ei++) {
                            const ev = val[ei];
                            if (pd.uiPrim === 'int') {
                                const parsed = Number(ev);
                                if (Number.isNaN(parsed) || !Number.isInteger(parsed)) {
                                    t.invalidInputs = true;
                                    t.invalidValues![ei] = true;
                                    this.errors.push(`Sample "${t.name}" — параметр "${pd.name}"[${ei}] должен быть int.`);
                                }
                            } else if (pd.uiPrim === 'double') {
                                const parsed = Number(ev);
                                if (Number.isNaN(parsed)) {
                                    t.invalidInputs = true;
                                    t.invalidValues![ei] = true;
                                    this.errors.push(`Sample "${t.name}" — параметр "${pd.name}"[${ei}] должен быть число.`);
                                }
                            } else if (pd.uiPrim === 'bool') {
                                if (!(ev === true || ev === false || ev === 'true' || ev === 'false')) {
                                    t.invalidInputs = true;
                                    t.invalidValues![ei] = true;
                                    this.errors.push(`Sample "${t.name}" — параметр "${pd.name}"[${ei}] должен быть boolean.`);
                                }
                            } else { // string
                                if (ev === null || typeof ev === 'undefined' || (typeof ev === 'string' && ev.trim() === '')) {
                                    t.invalidInputs = true;
                                    t.invalidValues![ei] = true;
                                    this.errors.push(`Sample "${t.name}" — параметр "${pd.name}"[${ei}] пустая строка.`);
                                }
                            }
                        }
                    }
                }
            }

            // if no signature parameters, require at least one input
            if (this.parameters.length === 0) {
                if (!t.inputs || t.inputs.length === 0 || (t.inputs[0] === '' || t.inputs[0] === null || typeof t.inputs[0] === 'undefined')) {
                    t.invalidInputs = true;
                    this.errors.push(`Sample "${t.name}" — входные данные не заданы.`);
                } else {
                    hasValidSample = true;
                }
            } else {
                // if params exist and no invalid flags -> sample valid
                if (!t.invalidInputs && !t.invalidName) hasValidSample = true;
            }
        }

        if (!hasValidSample) this.errors.push('Нужно минимум один валидный sample test.');

        return this.errors.length === 0;
    }

    /* ---------- Save ---------- */
    saveDraft() {
        if (!this.validateBeforeSave()) return;

        this.isSaving = true;

        const manifest: any = {
            entrypoint: this.entryPoint,
            signature: {
                returnType: this.returnType,
                parameters: this.parameters.map(p => ({
                    name: p.name,
                    type: p.typeDescriptor ?? { kind: p.uiType === 'array' ? 'array' : 'primitive', name: p.uiPrim }
                }))
            },
            helpers: this.helpers.map(h => ({ inline: h.inline ?? '', languageCode: h.languageCode })),
            advancedTests: this.advTests.map(a => ({ name: a.name, source: a.source, languageCode: a.languageCode, timeoutMs: a.timeoutMs })),
            sampleTests: this.simpleTests.map((s) => ({
                name: s.name,
                timeoutMs: 1000,
                inputs: s.inputs,
                expected: s.expected,
                comparator: s.comparator
            }))
        };

        const dto = {
            statement: this.model.statement,
            totalTests: (this.simpleTests.length + this.advTests.length),
            testManifest: manifest
        };


        if (this.wasDraft) {
            this.api.updateVersionDraft(this.model.problemId, this.model.versionId, dto).subscribe({
                next: () => {
                    this.isSaving = false;
                    this.router.navigate(['/versions']);
                },
                error: err => {
                    console.error('Save error', err);
                    this.isSaving = false;
                    this.errors = [err?.message ?? 'Ошибка при сохранении версии'];
                }
            });
        }
        else {
            this.api.saveProblemVersion(this.model.problemId, dto).subscribe({
                next: () => {
                    this.isSaving = false;
                    this.router.navigate(['/versions']);
                },
                error: err => {
                    console.error('Save error', err);
                    this.isSaving = false;
                    this.errors = [err?.message ?? 'Ошибка при сохранении версии'];
                }
            });
        }
    }

    cancel() { this.router.navigate(['/tasks']); }
}
