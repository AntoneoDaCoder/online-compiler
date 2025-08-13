export interface Task {
  name: string;
  description: string;
  exampleOutput: string;
  supportedLanguages: string[]; // например: ['csharp', 'java']
}
