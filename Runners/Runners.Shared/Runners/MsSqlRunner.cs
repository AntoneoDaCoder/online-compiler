using Microsoft.Data.SqlClient;
using Shared.DTOs;
using Shared.Enums;
using Shared.Models;
using System.Text;

namespace Runners.Shared.Runners
{
    public class MsSqlRunner: IRunner
    {
        private readonly string _connectionString = "Server=mssql-service.mssql.svc.cluster.local,1433;Database=master;User Id=sa;Password=Admin123!;Encrypt=False;TrustServerCertificate=True;";
        private Problem _problem;
        private string _solutionCode;

        public MsSqlRunner()
        {
            Console.WriteLine("[SQL Server Runner] Runner started.");
        }

        public async Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var transaction = connection.BeginTransaction();

                await using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = fullCode;

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                await transaction.RollbackAsync(cancellationToken);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SQL Server Runner] Exception while compiling: " + ex.Message);
                return (false, ex.Message);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            var result = new CodeResponseDto()
            {
                RequestId = requestId,
                Language = "mssql",
                Result = new ExecutionResultDto() { RequestSentAt = requestDate }
            };

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = connection.BeginTransaction();

            try
            {
                // Seeding
                foreach (var def in _problem.AdditionalDefinitions)
                {
                    if (def.Language?.ToLower() == "mssql" && !string.IsNullOrWhiteSpace(def.Value))
                    {
                        await using var cmdSeed = connection.CreateCommand();
                        cmdSeed.Transaction = transaction;
                        cmdSeed.CommandText = def.Value;
                        await cmdSeed.ExecuteNonQueryAsync(cancellationToken);
                    }
                }

                // User query as temp table
                await using (var cmdUser = connection.CreateCommand())
                {
                    cmdUser.Transaction = transaction;
                    cmdUser.CommandText = $"SELECT * INTO #user_result FROM ({_solutionCode.TrimEnd(';')}) AS user_query;";
                    await cmdUser.ExecuteNonQueryAsync(cancellationToken);
                }

                // Tests
                foreach (var test in _problem.TestCases)
                {
                    await using var cmdTest = connection.CreateCommand();
                    cmdTest.Transaction = transaction;
                    string testSql = test.InputExpression.Replace("(...)", "#user_result");
                    cmdTest.CommandText = testSql;
                    var scalarResult = await cmdTest.ExecuteScalarAsync(cancellationToken);

                    bool passed = int.TryParse(scalarResult?.ToString(), out var code) && code == 1;
                    Console.WriteLine("[SQL Server Runner] Test " + test.Name + ":" + passed);

                    if (!passed)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        result.Status = RequestStatus.Failed;
                        result.Result.Status = ExecutionStatus.FailedToExecute;
                        result.Result.ConsoleOutput = test.Name;
                        return result;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                result.Status = RequestStatus.Succeeded;
                result.Result.Status = ExecutionStatus.Succeeded;

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.RuntimeError;
                result.Result.ConsoleOutput = ex.Message;
                return result;
            }
        }

        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            _problem = problemSolutionDto.Problem;
            _solutionCode = problemSolutionDto.Code;

            var sb = new StringBuilder();

            foreach (var def in problemSolutionDto.Problem.AdditionalDefinitions)
            {
                if (def.Language?.ToLower() == "mssql" && !string.IsNullOrWhiteSpace(def.Value))
                {
                    sb.AppendLine(def.Value);
                }
            }

            // Temp table with user query result
            sb.AppendLine();
            sb.AppendLine($"SELECT * INTO #user_result FROM ({problemSolutionDto.Code.TrimEnd(';')}) AS user_query;");
            sb.AppendLine();

            // Tests
            foreach (var testCase in problemSolutionDto.Problem.TestCases)
            {
                if (testCase.TestLanguage?.ToLower() == "mssql" && !string.IsNullOrWhiteSpace(testCase.InputExpression))
                {
                    sb.AppendLine(testCase.InputExpression.Replace("(...)", "#user_result") + ";");
                }
            }

            return sb.ToString();
        }
    
    }
}
