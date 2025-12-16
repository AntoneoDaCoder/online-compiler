// src/app/app-routing.module.ts
import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { GoogleCallbackComponent } from './pages/google-callback/google-callback.component';
import { TasksComponent } from './pages/tasks/task.component';
import { CodeEditorComponent } from './pages/code-editor/code-editor.component';
import { ProblemEditorComponent } from './pages/problem-editor/problem-editor.component';
import { VersionsComponent } from './pages/versions/versions-component';
import { SubmissionsComponent } from './pages/submissions/submissions.component';
import { SubmissionDetailComponent } from './pages/submission-detail/submission-detail.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { AuthGuard } from './core/guards/auth.guard';

export const appRoutes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'auth/google-callback', component: GoogleCallbackComponent },

  // основные страницы
  { path: 'tasks', component: TasksComponent, canActivate: [AuthGuard] },
  { path: 'code-editor', component: CodeEditorComponent, canActivate: [AuthGuard] },
  { path: 'problems/:problemId/edit', component: ProblemEditorComponent, canActivate: [AuthGuard], data: { roles: ['Editor','Admin'] } },
  { path: 'problems/:problemId/versions/:versionId/edit', component: ProblemEditorComponent, canActivate: [AuthGuard], data: { roles: ['Editor','Admin'] } },
  { path: 'versions', component: VersionsComponent, canActivate: [AuthGuard], data: { roles: ['Editor','Admin'] } },
  { path: 'submissions', component: SubmissionsComponent, canActivate: [AuthGuard] },
  { path: 'submissions/:id', component: SubmissionDetailComponent, canActivate: [AuthGuard] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },

  // дефолтная
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];
