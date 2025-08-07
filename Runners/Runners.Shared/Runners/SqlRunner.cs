using Shared.DTOs;
using Shared.Enums;
using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace Runners.Shared.Runners
{
    public class SqlRunner : IRunner
    {
        private PostgreSqlContainer _container;
        private string _connectionString = "";

        public SqlRunner()
        {
            _container = new PostgreSqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithCleanUp(true)
                .Build();
        }

        // no need
        public (bool Success, string CompilationErrors) CompileCode(string fullCode, out ProcessStartInfo? pInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, ProcessStartInfo pInfo, CancellationToken cancellationToken)
        {
            try
            {
                await _container.StartAsync(cancellationToken);
                _connectionString = _container.GetConnectionString();

                // seeding logic
                // execution logic

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
                        ConsoleOutput = "some json",
                    }
                };
            }
            catch (Exception ex)
            {
                return new CodeResponseDto
                {
                    RequestId = requestId,
                    Language = "SQL",
                    Status = RequestStatus.Failed
                };
            }
            finally
            {
                await _container.StopAsync();
            }
        }

        // no need
        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            return problemSolutionDto.Code;
        }
    }
}
