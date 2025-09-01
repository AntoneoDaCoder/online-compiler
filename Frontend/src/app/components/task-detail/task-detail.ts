import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { Task } from '../../models/task.model';
import { Language } from '../../models/language.model';
import { CodeTemplate } from '../../models/codeTemplate.model';
import { HttpClient } from '@angular/common/http';
import { SignalRService } from '../../services/signalr.service';
import { v4 as uuidv4 } from 'uuid';
import { ExecutionResultDto } from '../../models/executionResultDto.model';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.html',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  styleUrls: ['./task-detail.css']
})
export class TaskDetailComponent implements OnInit {
  task: Task | undefined;

  allLanguages: Language[] = [
    { id: 'csharp', name: 'C#' },
    { id: 'java', name: 'Java' },
    { id: "postgresql", name: "PostgreSQL" },
    { id: "mssql", name: "MSSQL" },
    { id: 'nodejs', name: 'NodeJS' },
    { id: 'kotlin', name: 'Kotlin' },
    {id:'typescript',name:'TypeScript'}
    //  { id: 'swift', name: 'Swift' }
  ];

  languages: Language[] = [];
  selectedLanguage = this.languages[0];
  code: string = '';
  output: string = '';
  parsedResult: ExecutionResultDto | null = null;

  templates: CodeTemplate[] = [
    //     {
    //       language: 'swift',
    //       taskName: 'FractionalKnapsack',
    //       body:
    //         `/*Additional definition for convenience

    // class Item {
    //   var value: Int
    //   var weight: Int
    //   var ratio: Double { return Double(value) / Double(weight) }

    //   init(value: Int, weight: Int) {
    //     self.value = value
    //     self.weight = weight
    //   }
    // }
    //   */

    //   class Solution {
    //     func fractionalKnapsack(_ items: [Item], _ capacity: Int) -> Double {
    //       //your solution here
    //     }
    // }`
    //     },
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
    },
    {
      language: 'java',
      taskName: 'FractionalKnapsack',
      body:
        `/*Additional definition for convenience
public static class Item {
  public int value;
  public int weight;
 
  public Item(int value, int weight) {
      this.value = value;
      this.weight = weight;
    }
 
    public double getRatio() {
      return (double) value / weight;
    }
}
*/

public static class Solution {
  public double fractionalKnapsack(Item[] items, int capacity) {
      //your solution here
  }
}`
    },
    {
      language:'kotlin',
      taskName:'FractionalKnapsack',
      body:
`
/* Additional definition for convenience

data class Item(
    val value: Int,
    val weight: Int
) {
    val ratio: Double
        get() = value.toDouble() / weight
}

*/

class Solution {
    fun fractionalKnapsack(items: Array<Item>, capacity: Int): Double {
        // your solution here
    }
}
`
    },
    {
      language: 'csharp',
      taskName: 'ArrayMin',
      body:
        `
public class Solution
{
       public int FindMinimum(int[] arr)
       {
          //your solution
       }
}
`
    },
    {
      language: 'java',
      taskName: 'ArrayMin',
      body:
        `
public static class Solution
{
      public int findMinimum(int[] arr)
      {
          //your solution
      }
}
`
    },
    {
      language:'typescript',
      taskName:'ArrayMin',
      body:
`
class Solution {
      findMinimum(arr: number[]): number {
      //your solution here
    }
}
`
    },
    {
      language: 'nodejs',
      taskName: 'FractionalKnapsack',
      body:
        `
/* Additional definition for convenience
class Item {
    constructor(value, weight) {
        this.value = value;
        this.weight = weight;
    }
    get ratio() {
        return this.value / this.weight;
    }
}   
*/

class Solution {
    fractionalKnapsack(items, capacity) {
        //your solution here
    }
}
`
    },
    {
      language: 'nodejs',
      taskName: 'ArrayMin',
      body:
        `
class Solution {
      findMinimum(arr) {
        //your solution here
      }
}
`
    }
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private signalRService: SignalRService) { }

  ngOnInit() {
    // Пытаемся получить задачу из state
    const navState = history.state as { task?: Task };
    if (navState.task) {
      this.task = navState.task;
    } else {
      // fallback, если зашли напрямую в URL
      const taskName = this.route.snapshot.paramMap.get('name');
      console.error('Нет данных задачи в state, нужно грузить с API по имени:', taskName);
      return;
    }

    // фильтруем доступные языки
    this.languages = this.allLanguages.filter(lang =>
      this.task!.supportedLanguages.includes(lang.id)
    );

    // выбираем первый доступный
    this.selectedLanguage = this.languages[0];

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
      this.code = '';
    }
  }

  async runCode() {
    const requestId = uuidv4();

    await this.signalRService.startConnection();
    await this.signalRService.joinGroup(requestId);


    this.signalRService.onMessage((message: any) => {
      // Проверяем, если message — объект, просто присваиваем
      if (typeof message === 'string') {
        try {
          const data = JSON.parse(message);
          this.parsedResult = data.result;
        } catch {
          this.output = message;
          this.parsedResult = null;
        }
      } else if (typeof message === 'object' && message !== null) {
        // Если объект — присваиваем напрямую
        this.parsedResult = message.result || message; // зависит от структуры
        this.output = '';
      } else {
        this.output = String(message);
        this.parsedResult = null;
      }
    });


    const dto = {
      requestId,
      language: this.selectedLanguage.id,
      problemName: this.task?.name ?? '',
      code: this.code,
      maxAllowedTimeInMilliseconds: 2000,
      callbackUrl: '',
      requestSentAt: new Date()
    };

    this.http.post('http://localhost:12345/api/jobs/start', dto).subscribe({
      next: () => {
        this.output = 'Ожидаем ответ от сервера...';
      },
      error: (err) => {
        console.error(err);
        this.output = 'Ошибка при отправке запроса.';
      }
    });
  }

  goBack() {
    this.router.navigate(['/']);
  }

}
