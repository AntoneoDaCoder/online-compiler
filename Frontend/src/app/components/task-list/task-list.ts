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
    { name: 'FractionalKnapsack', description: 'Desc 1', exampleOutput: 'Output 1', supportedLanguages: ['csharp', 'java','nodejs'] },
    { name: 'ArrayMin', description: 'Desc 2', exampleOutput: 'Output 2', supportedLanguages: ['csharp', 'java'] },
    { name: 'CustomersWithExpensiveOrders', description: 'Desc 3', exampleOutput: 'Output 3', supportedLanguages: ['sql'] },
    { name: 'CategoriesWithHighTotalPrice', description: 'Desc 4', exampleOutput: 'Output 4', supportedLanguages: ['sql'] },
    { name: 'StudentsWithMultipleCourses', description: 'Desc 5', exampleOutput: 'Output 5', supportedLanguages: ['sql'] }
  ];

  constructor(private router: Router) { }

  openTask(task: Task) {
    this.router.navigate(['/task', task.name], { state: { task } });
  }

}
