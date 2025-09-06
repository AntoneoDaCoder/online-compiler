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
        private string _codeWithoutTests;
        private static readonly string[] _forbiddenKeywords =
        [
            "drop", "alter", "create", "truncate", "merge",
            "grant", "revoke", "commit", "rollback", "save",
            "execute", "sp_executesql", "bulk",    
            "set", "shutdown", "dbcc", "use", "alter login",
            "backup", "restore", "kill", "print", "waitfor"

        ];

        public MsSqlRunner()
        {
            Console.WriteLine("[SQL Server Runner] Runner started.");
        }

        public async Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            if (ContainsForbidden(_solutionCode, out var bad))
            {
                return (false, $"Forbidden keyword detected: {bad}");
            }
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                await using var cmd = connection.CreateCommand();
                cmd.Transaction = (SqlTransaction)transaction;
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

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = _codeWithoutTests;
                cmd.Transaction = (SqlTransaction)transaction;

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Tests
                foreach (var test in _problem.TestCases)
                {
                    await using var cmdTest = connection.CreateCommand();
                    cmdTest.Transaction = (SqlTransaction)transaction;

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

                await transaction.RollbackAsync(cancellationToken);
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

            // Seeding
            foreach (var def in problemSolutionDto.Problem.AdditionalDefinitions)
            {
                if (def.Language?.ToLower() == "mssql" && !string.IsNullOrWhiteSpace(def.Value))
                {
                    sb.AppendLine(def.Value);
                }
            }

            // Goal - retrieve last select statement to store the result into a temporary table
            var statements = _solutionCode.Split(";")
                                              .Select(s => s.Trim())
                                              .Where(s => !string.IsNullOrWhiteSpace(s))
                                              .ToList();

            string? lastSelect = null;

            foreach (var statement in statements)
            {
                if (statement.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                {
                    lastSelect = statement;
                }

                sb.AppendLine(statement + ";");
            }

            if (lastSelect is not null)
            {
                sb.AppendLine($"SELECT * INTO #user_result FROM ({lastSelect.TrimEnd(';')}) AS user_query;");
            }

            // Save current sql without tests
            _codeWithoutTests = sb.ToString();

            // Tests
            foreach (var testCase in problemSolutionDto.Problem.TestCases)
            {
                if (testCase.TestLanguage?.ToLower() == "mssql" && !string.IsNullOrWhiteSpace(testCase.InputExpression))
                {
                    sb.AppendLine(testCase.InputExpression.Replace("(...)", "#user_result") + ";");
                }
            }

            var finalSql = sb.ToString();

            Console.WriteLine("[MSSQL Runner] Final sql:" + finalSql);

            return finalSql;
        }

        private bool ContainsForbidden(string sql, out string keyword)
        {
            var lowered = sql.ToLowerInvariant();
            foreach (var f in _forbiddenKeywords)
            {
                if (lowered.Contains(f))
                {
                    keyword = f;
                    return true;
                }
            }
            keyword = "";
            return false;
        }
    }
}
