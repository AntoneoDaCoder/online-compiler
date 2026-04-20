// src/app/app-routing.module.ts
import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { TasksComponent } from './pages/tasks/task.component';
import { CodeEditorComponent } from './pages/code-editor/code-editor.component';
import { ProblemEditorComponent } from './pages/problem-editor/problem-editor.component';
import { VersionsComponent } from './pages/versions/versions-component';
import { SubmissionsComponent } from './pages/submissions/submissions.component';
import { SubmissionDetailComponent } from './pages/submission-detail/submission-detail.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { AuthGuard } from './core/guards/auth.guard';
import { DeletionRequestsComponent } from './pages/deletion-requests/deletion-requests.component';
import { DeletionQueueComponent } from './pages/deletion-queue/deletion-queue.component';

export const appRoutes: Routes = [
  { path: 'login', component: LoginComponent },

  // основные страницы
  { path: 'tasks', component: TasksComponent, canActivate: [AuthGuard] },
  { path: 'code-editor', component: CodeEditorComponent, canActivate: [AuthGuard] },
  { path: 'problems/:slug/edit', component: ProblemEditorComponent, canActivate: [AuthGuard], data: { roles: ['Editor', 'Admin'] } },
  { path: 'versions/:versionId/edit', component: ProblemEditorComponent, canActivate: [AuthGuard], data: { roles: ['Editor', 'Admin'] } },
  { path: 'versions', component: VersionsComponent, canActivate: [AuthGuard], data: { roles: ['Editor', 'Admin'] } },
  { path: 'submissions', component: SubmissionsComponent, canActivate: [AuthGuard] },
  { path: 'submissions/:id', component: SubmissionDetailComponent, canActivate: [AuthGuard] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
  { path: 'my-deletion-requests', component: DeletionRequestsComponent, canActivate: [AuthGuard], data: { roles: ['Editor', 'Admin'], viewAsEditor: true } },
  { path: 'deletion-requests', component: DeletionRequestsComponent, canActivate: [AuthGuard], data: { roles: ['Admin'], viewAsEditor: false } },
  { path: 'deletion-queue', component: DeletionQueueComponent, canActivate: [AuthGuard], data: { roles: ['Admin'] } },

  // дефолтная
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];
