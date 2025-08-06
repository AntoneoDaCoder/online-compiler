import { Routes } from '@angular/router';
import { TaskListComponent } from './components/task-list/task-list';
import { TaskDetailComponent } from './components/task-detail/task-detail';

export const routes: Routes = [
  { path: '', component: TaskListComponent },
  { path: 'task/:name', component: TaskDetailComponent }
];
