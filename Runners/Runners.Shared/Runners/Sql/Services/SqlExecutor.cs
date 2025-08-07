using Npgsql;
using System.Data;
using System.Text.Json;

namespace Runners.Shared.Runners.Sql.Services
{
    public static class SqlExecutor
    {
        public static async Task<string> ExecuteAsync(string connectionString, string sql)
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);

            Console.WriteLine("[SQL Runner] Start command execution.");

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync();

                var table = new DataTable();
                table.Load(reader);

                var result = table.Rows.Cast<DataRow>()
                    .Select(row => table.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col]))
                    .ToList();

                Console.WriteLine("[SQL Runner] Command executed.");

                return JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Execution failed",
                    message = ex.Message
                });
            }
        }
    }
}
