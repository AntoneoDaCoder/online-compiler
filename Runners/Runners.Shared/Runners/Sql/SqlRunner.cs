using Runners.Shared.Runners.Sql.Services;
using Shared.DTOs;
using Shared.Enums;
using Testcontainers.PostgreSql;

namespace Runners.Shared.Runners.Sql
{
    public class SqlRunner : IRunner
    {
        private PostgreSqlContainer _container;
        private string _connectionString = "";

        public SqlRunner()
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithCleanUp(true)
                .Build();

            Console.WriteLine("[SQL Runner] Container built.");
        }

        // no need
        public Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            Console.WriteLine("[SQL Runner] Skip compilation.");

            return Task.FromResult((true, string.Empty));
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            try
            {
                await _container.StartAsync(cancellationToken);

                Console.WriteLine("[SQL Runner] Container started.");

                _connectionString = _container.GetConnectionString();

                Console.WriteLine($"[SQL Runner] Connection string: {_connectionString}.");

                // seeding logic
                await Seeder.SeedAsync(_connectionString);

                // execution logic
                var resultJson = await SqlExecutor.ExecuteAsync(_connectionString, "SELECT * FROM users;");

                return new CodeResponseDto
                {
                    RequestId = requestId,
                    Language = "SQL",
                    Status = RequestStatus.Succeeded,
                    Result = new ExecutionResultDto
                    {
                        RequestSentAt = requestDate,
                        Status = ExecutionStatus.Succeded,
                        ExitCode = 0,
                        ConsoleOutput = resultJson,
                    }
                };
            }
            catch (Exception ex)
            {
                return new CodeResponseDto
                {
                    RequestId = requestId,
                    Language = "SQL",
                    Status = RequestStatus.Failed,
                    Result = new ExecutionResultDto
                    {
                        RequestSentAt = requestDate,
                        Status = ExecutionStatus.FailedToExecute,
                        ExitCode = 1,
                        ConsoleOutput = ex.ToString()
                    }
                };
            }
            finally
            {
                await _container.StopAsync();

                Console.WriteLine("[SQL Runner] Stopping container.");
            }
        }

        // no need
        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            Console.WriteLine("[SQL Runner] Skip wrapping code.");

            return problemSolutionDto.Code;
        }
    }
}
