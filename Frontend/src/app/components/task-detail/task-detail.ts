import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule, Router } from '@angular/router'; // если используешь router
import { Task } from '../../models/task.model';
import { Language } from '../../models/language.model';
import { CodeTemplate } from '../../models/codeTemplate.model';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.html',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  styleUrls: ['./task-detail.css']
})
export class TaskDetailComponent implements OnInit {
  task: Task | undefined;
  languages: Language[] = [
    { id: 'swift', name: 'Swift' },
    { id: 'csharp', name: 'C#' }
  ];
  selectedLanguage = this.languages[0];
  code: string = '';
  output: string = '';

  // Заглушки
  templates: CodeTemplate[] = [
    {
      language: 'swift',
      taskName: 'FractionalKnapsack',
      body:
`/*Additional definition for convenience

class Item {
  var value: Int
  var weight: Int
  var ratio: Double { return Double(value) / Double(weight) }

  init(value: Int, weight: Int) {
    self.value = value
    self.weight = weight
  }
}
  */

  class Solution {
    func fractionalKnapsack(_ items: [Item], _ capacity: Int) -> Double {
      //your solution here
    }
}`
    },
    {
      language: 'csharp',
      taskName: 'FractionalKnapsack',
      body: 
`/*Additional definition for convenience

public class Item
{
  public int Value;
  public int Weight;
  public double Ratio => (double)Value / Weight;
}

*/

public class Solution
{
  public double FractionalKnapsack(Item[] items, int capacity)
  {
    //your solution here
  }
}`
    }
  ];

  constructor(private route: ActivatedRoute, private router: Router) { }

  ngOnInit() {
    const taskName = this.route.snapshot.paramMap.get('name');
    console.log('taskName from route:', taskName);

    this.task = {
      name: taskName || '',
      description: 'Описание задачи...',
      exampleOutput: 'Пример вывода...'
    };

    this.loadTemplate();
  }



  loadTemplate() {
    if (!this.task || !this.task.name) {
      this.code = '';
      return;
    }

    const template = this.templates.find(t =>
      t.taskName.toLowerCase() === this.task!.name.toLowerCase() &&
      t.language.toLowerCase() === this.selectedLanguage.id.toLowerCase()
    );

    if (template) {
      this.code = template.body;
    } else {
      this.code = ''; // Шаблон не найден — очистить редактор
    }
  }

  runCode() {
    // Заглушка под запрос к серверу
    this.output = 'Сервер вернул: OK';
  }

  goBack() {
    this.router.navigate(['/']);
  }

}
