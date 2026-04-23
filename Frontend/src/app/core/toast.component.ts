// src/app/components/shared/toast.component.ts
import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ToastMessage, ToastService } from '../core/services/toast.service'

@Component({
    selector: 'app-toast',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="toast-host" *ngIf="toast">
      <div class="toast" [class.success]="toast.type === 'success'"
                        [class.error]="toast.type === 'error'"
                        [class.info]="toast.type === 'info'">
        <div class="toast__header">
          <div class="toast__title" *ngIf="toast.title">{{ toast.title }}</div>
          <button type="button" class="toast__close" (click)="close()">×</button>
        </div>

        <div class="toast__message">{{ toast.message }}</div>

        <div class="toast__progress" [style.animationDuration]="toast.duration + 'ms'"></div>
      </div>
    </div>
  `
})
export class ToastComponent implements OnInit, OnDestroy {
    toast: ToastMessage | null = null;

    private sub?: Subscription;
    private timeoutId?: number;

    constructor(private toastService: ToastService) { }

    ngOnInit(): void {
        this.sub = this.toastService.toast$.subscribe(t => {
            this.toast = t;

            if (this.timeoutId) {
                clearTimeout(this.timeoutId);
            }

            if (t) {
                this.timeoutId = window.setTimeout(() => this.close(), t.duration);
            }
        });
    }

    ngOnDestroy(): void {
        this.sub?.unsubscribe();
        if (this.timeoutId) clearTimeout(this.timeoutId);
    }

    close(): void {
        this.toast = null;
        this.toastService.clear();
    }
}