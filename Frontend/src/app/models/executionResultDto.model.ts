export interface ExecutionResultDto {
  status: string;         
  exitCode: number;
  consoleOutput?: string;
  requestSentAt: string;  
  responseSentAt: string; 
  latencyInSeconds: number;
}
