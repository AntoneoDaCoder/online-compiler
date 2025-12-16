// src/app/pages/tasks/task.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { ApiService } from '../../core/services/api.service';
import { SignalrService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageDto, ProblemDto, UserProblemVersionDto } from '../../core/models/dtos';
import { SidebarComponent } from '../../components/shared/sidebar.component';

@Component({
    selector: 'app-tasks',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, SidebarComponent],
    templateUrl: './task.component.html'
})
export class TasksComponent implements OnInit {
    languages: LanguageDto[] = [];
    problems: ProblemDto[] = [];
    filtered: ProblemDto[] = [];

    // фильтры
    filterSlug = '';
    filterLang: string | null = null; // language id
    showOnlyPublished: 'all' | 'published' | 'unpublished' = 'all';

    constructor(
        private api: ApiService,
        private signalr: SignalrService,
        public auth: AuthService
    ) { }

    ngOnInit() {
        this.loadInitial();

        // При инициализации можно стартовать SignalR если уже есть токен
        try { this.signalr.startConnection(); } catch { /* ignore */ }

        this.signalr.onUpdates().subscribe(payload => {
            console.log('SignalR payload', payload);
            // сюда можно вставить логику обновления состояния (например, merge)
        });
    }

    loadInitial() {
        this.api.getLanguages().subscribe(l => this.languages = l);
        this.api.getProblems().subscribe(p => {
            this.problems = p;
            this.applyFilters();
        });
    }

    // Метод вызываемый из шаблона
    updateData() {
        // Попробуем стартовать соединение и вызвать hub метод
        try { this.signalr.startConnection(); } catch { /* ignore */ }

        const promise = this.signalr.invoke('RequestPageData', { page: 'Tasks' });
        if (promise) {
            promise.then((res) => {
                console.log('SignalR response', res);
                // TODO: обработать res и обновить this.problems / this.languages
            }).catch(err => console.error('SignalR invoke error', err));
        } else {
            console.warn('SignalR invoke returned undefined — соединение ещё не готово');
        }
    }

    applyFilters() {
        this.filtered = this.problems.filter(p => {
            if (this.filterSlug && !p.slug.includes(this.filterSlug)) return false;

            if (this.filterLang) {
                const latest = p.latestVersion as UserProblemVersionDto | undefined | null;
                if (!latest) return false;
                if (!latest.supportedLanguages || !latest.supportedLanguages.includes(this.filterLang)) return false;
            }

            if (this.showOnlyPublished !== 'all') {
                const isPublished = !!p.status && p.status.toLowerCase() !== 'no version' && p.status.toLowerCase() !== 'deleted';
                if (this.showOnlyPublished === 'published' && !isPublished) return false;
                if (this.showOnlyPublished === 'unpublished' && isPublished) return false;
            }

            return true;
        });
    }

    clearFilters() {
        this.filterSlug = '';
        this.filterLang = null;
        this.showOnlyPublished = 'all';
        this.applyFilters();
    }

    onOpenProblem(problem: ProblemDto) {
        const roles = this.auth.getRoles();
        const isEditorOrAdmin = roles.includes('Editor') || roles.includes('Admin');
        const hasPublished = problem.status && problem.status.toLowerCase() !== 'no version' && problem.status.toLowerCase() !== 'deleted';

        if (isEditorOrAdmin && !hasPublished) {
            // логика открытия редактора задачи
            console.log('Open problem editor for', problem.id);
        } else {
            // логика открытия редактора кода
            console.log('Open code editor for', problem.slug);
        }
    }

    // Внутри класса TasksComponent
    formatSupportedLanguages(langIds?: string[] | null): string {
        if (!langIds || !this.languages || this.languages.length === 0) return '';
        return langIds
            .map(id => this.languages.find(x => x.id === id)?.displayName ?? id)
            .join(', ');
    }

    getProblemActionLabel(p: ProblemDto): string {
        const roles = this.auth.getRoles();
        const isEditorOrAdmin = roles.includes('Editor') || roles.includes('Admin');
        const hasPublished = !!p.status && p.status.toLowerCase() !== 'no version' && p.status.toLowerCase() !== 'deleted';
        return (isEditorOrAdmin && !hasPublished) ? 'Изменить' : 'Решить';
    }

}
