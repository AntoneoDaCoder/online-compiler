using Npgsql;

namespace Runners.Shared.Runners.Sql.Services
{
    public static class Seeder
    {
        public static async Task SeedAsync(string connectionString)
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            Console.WriteLine("[SQL Runner] Start seeding data.");

            var cmd = conn.CreateCommand();
            cmd.CommandText = """
            CREATE TABLE users (
                id SERIAL PRIMARY KEY,
                name TEXT,
                age INT
            );
            INSERT INTO users (name, age) VALUES 
                ('Kama', 20),
                ('Kamila', 30);
            """;

            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine("[SQL Runner] Data seeded.");
        }
    }
}
