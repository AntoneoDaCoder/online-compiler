using Microsoft.Data.Sqlite;
using Shared.DTOs;
using Shared.Enums;
using Shared.Models;
using System.Text;

namespace Runners.Shared.Runners
{
    public class SqlRunner : IRunner
    {
        private readonly string _connectionString = "Data Source=shared;Mode=Memory;Cache=Shared";
        private Problem _problem;
        private string _solutionCode;
        public SqlRunner()
        {
            Console.WriteLine("[SQL Runner] Container built.");
        }

        public async Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var transaction = connection.BeginTransaction();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = fullCode;

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                transaction.Rollback();

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SQL Runner] Exception while compiling:"+ ex.Message);

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
                Language = "sql",
                Result = new ExecutionResultDto()
                {
                    RequestSentAt = requestDate,
                }
            };

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var def in _problem.AdditionalDefinitions)
                {
                    if (def.Language?.ToLower() == "sql" && !string.IsNullOrWhiteSpace(def.Value))
                    {
                        using var cmdSeed = connection.CreateCommand();
                        cmdSeed.CommandText = def.Value;
                        await cmdSeed.ExecuteNonQueryAsync(cancellationToken);
                    }
                }

                using (var cmdUser = connection.CreateCommand())
                {
                    cmdUser.CommandText = $"CREATE TEMP TABLE user_result AS {_solutionCode.TrimEnd(';')};";
                    await cmdUser.ExecuteNonQueryAsync(cancellationToken);
                }

                // Tests
                foreach (var test in _problem.TestCases)
                {
                    using var cmdTest = connection.CreateCommand();
                    string testSql = test.InputExpression.Replace("(...)", "user_result");
                    cmdTest.CommandText = testSql;
                    var scalarResult = await cmdTest.ExecuteScalarAsync(cancellationToken);

                    bool passed = int.TryParse(scalarResult?.ToString(), out var code) && code == 1;

                    Console.WriteLine("[SQL Runner] Test " + test.Name + ":" + passed);

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
                result.Result.Status = ExecutionStatus.Succeded;

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
            Console.WriteLine("[SQL Runner] Start wrapping code.");

            _problem = problemSolutionDto.Problem;
            _solutionCode = problemSolutionDto.Code;

            var sb = new StringBuilder();

            // Seeding
            foreach (var def in problemSolutionDto.Problem.AdditionalDefinitions)
            {
                if (def.Language?.ToLower() == "sql" && !string.IsNullOrWhiteSpace(def.Value))
                {
                    sb.AppendLine(def.Value);
                }
            }

            // Temp table with user's query result
            sb.AppendLine();
            sb.AppendLine($"CREATE TEMP TABLE user_result AS");
            sb.AppendLine(problemSolutionDto.Code.TrimEnd(';') + ";");
            sb.AppendLine();

            // Tests
            foreach (var testCase in problemSolutionDto.Problem.TestCases)
            {
                if (testCase.TestLanguage?.ToLower() == "sql" && !string.IsNullOrWhiteSpace(testCase.InputExpression))
                {
                    var testSql = testCase.InputExpression.Replace("(...)", "user_result");

                    sb.AppendLine(testSql.TrimEnd(';') + ";");
                    sb.AppendLine();
                }
            }

            var finalSql = sb.ToString();

            Console.WriteLine("[SQL Runner] Final sql:" + finalSql);

            return finalSql;
        }
    }
}
