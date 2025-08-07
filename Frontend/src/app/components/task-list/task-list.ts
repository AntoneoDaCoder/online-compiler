import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Task } from '../../models/task.model';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-list.html',
  styleUrls: ['./task-list.css']
})
export class TaskListComponent {
  tasks: Task[] = [
    { name: 'FractionalKnapsack', description: 'Desc 1', exampleOutput: 'Output 1' },
    { name: 'Task 2', description: 'Desc 2', exampleOutput: 'Output 2' }
  ];

  constructor(private router: Router) { }

  openTask(task: Task) {
    this.router.navigate(['/task', task.name]);
  }
}
