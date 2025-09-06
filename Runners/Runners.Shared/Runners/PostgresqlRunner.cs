using Npgsql;
using Shared.DTOs;
using Shared.Enums;
using Shared.Models;
using System.Text;

namespace Runners.Shared.Runners
{
    public class PostgresqlRunner : IRunner
    {
        private readonly string _connectionString = "Host=postgres.postgresql.svc.cluster.local;Port=5432;Database=postgresdb;Username=postgresadmin;Password=admin123";
        private Problem _problem;
        private string _solutionCode;
        private string _codeWithoutTests;
        private static readonly string[] _forbiddenKeywords =
        [
            "drop", "alter", "create", "truncate", "merge",
            "grant", "revoke", "commit", "rollback", "savepoint",
            "execute", "prepare", "deallocate",
            "vacuum", "analyze", "reset", "discard",
            "copy", "load", "listen", "notify", "unlisten",
            "set", "show"
        ];

        public PostgresqlRunner()
        {
            Console.WriteLine("[PostgreSQL Runner] Runner started.");
        }

        public async Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            if (ContainsForbidden(_solutionCode, out var bad))
            {
                return (false, $"Forbidden keyword detected: {bad}");
            }
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = fullCode;

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                await transaction.RollbackAsync(cancellationToken);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[PostgreSQL Runner] Exception while compiling:" + ex.Message);

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
                Language = "postgresql",
                Result = new ExecutionResultDto()
                {
                    RequestSentAt = requestDate,
                }
            };

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = _codeWithoutTests;
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Tests are executed one by one
                foreach (var test in _problem.TestCases)
                {
                    using var cmdTest = connection.CreateCommand();
                    string testSql = test.InputExpression.Replace("(...)", "user_result");
                    cmdTest.CommandText = testSql;
                    var scalarResult = await cmdTest.ExecuteScalarAsync(cancellationToken);

                    bool passed = int.TryParse(scalarResult?.ToString(), out var code) && code == 1;

                    Console.WriteLine("[PostgreSQL Runner] Test " + test.Name + ":" + passed);

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
            Console.WriteLine("[PostgreSQL Runner] Start wrapping code.");

            _problem = problemSolutionDto.Problem;
            _solutionCode = problemSolutionDto.Code;

            var sb = new StringBuilder();

            // Seeding
            foreach (var def in problemSolutionDto.Problem.AdditionalDefinitions)
            {
                if (def.Language?.ToLower() == "postgresql" && !string.IsNullOrWhiteSpace(def.Value))
                {
                    sb.AppendLine(def.Value.TrimEnd(';') + ";");
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
                var query = $"CREATE TEMP TABLE user_result AS {lastSelect};";
                sb.Append(query);
            }

            // Save current sql without tests
            _codeWithoutTests = sb.ToString();

            // Tests
            foreach (var testCase in problemSolutionDto.Problem.TestCases)
            {
                if (testCase.TestLanguage?.ToLower() == "postgresql" && !string.IsNullOrWhiteSpace(testCase.InputExpression))
                {
                    var testSql = testCase.InputExpression.Replace("(...)", "user_result");

                    sb.AppendLine(testSql.TrimEnd(';') + ";");
                    sb.AppendLine();
                }
            }

            var finalSql = sb.ToString();

            Console.WriteLine("[PostgreSQL Runner] Final sql:" + finalSql);

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
