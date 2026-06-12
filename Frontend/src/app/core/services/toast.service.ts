// src/app/core/services/toast.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
    title?: string;
    message: string;
    type: ToastType;
    duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
    private readonly _toast$ = new BehaviorSubject<ToastMessage | null>(null);
    readonly toast$ = this._toast$.asObservable();

    show(message: string, type: ToastType = 'info', duration = 4000, title?: string): void {
        this._toast$.next({ message, type, duration, title });
    }

    clear(): void {
        this._toast$.next(null);
    }
}