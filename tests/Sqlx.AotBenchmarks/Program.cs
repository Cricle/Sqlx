using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.AotBenchmarks;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

Console.WriteLine("Sqlx AOT Benchmark");
Console.WriteLine("==================");
Console.WriteLine();

// Setup database
using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            email TEXT NOT NULL,
            age INTEGER NOT NULL,
            is_active INTEGER NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT,
            balance REAL NOT NULL,
            description TEXT,
            score INTEGER NOT NULL
        )";
    cmd.ExecuteNonQuery();
}

// Seed data
const int recordCount = 10000;
Console.WriteLine($"Seeding {recordCount} records...");

using (var transaction = connection.BeginTransaction())
{
    using var cmd = connection.CreateCommand();
    cmd.Transaction = transaction;
    
    for (int i = 1; i <= recordCount; i++)
    {
        cmd.CommandText = $@"
            INSERT INTO users (name, email, age, is_active, created_at, balance, score)
            VALUES ('User{i}', 'user{i}@test.com', {20 + (i % 50)}, {(i % 2)}, '{DateTime.UtcNow:O}', {100.0 + i}, {i % 100})";
        cmd.ExecuteNonQuery();
    }
    
    transaction.Commit();
}

Console.WriteLine("Database ready.");
Console.WriteLine();

// Create repository
var repo = new AotUserRepository(connection);

// Warmup
Console.WriteLine("Warming up...");
for (int i = 0; i < 1000; i++)
{
    await repo.GetByIdAsync(1);
    await repo.CountAsync();
}
Console.WriteLine();

// Benchmark settings
const int iterations = 100000;

// Benchmark 1: GetById
Console.WriteLine($"Benchmark: GetById ({iterations} iterations)");
var sw = Stopwatch.StartNew();
for (int i = 0; i < iterations; i++)
{
    await repo.GetByIdAsync((i % recordCount) + 1);
}
sw.Stop();
Console.WriteLine($"  Total: {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"  Per op: {sw.Elapsed.TotalMicroseconds / iterations:F3} us");
Console.WriteLine();

// Benchmark 2: Count
Console.WriteLine($"Benchmark: Count ({iterations} iterations)");
sw.Restart();
for (int i = 0; i < iterations; i++)
{
    await repo.CountAsync();
}
sw.Stop();
Console.WriteLine($"  Total: {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"  Per op: {sw.Elapsed.TotalMicroseconds / iterations:F3} us");
Console.WriteLine();

// Benchmark 3: GetPaged (dynamic render)
Console.WriteLine($"Benchmark: GetPaged ({iterations / 10} iterations)");
sw.Restart();
for (int i = 0; i < iterations / 10; i++)
{
    await repo.GetPagedAsync(10, i * 10 % recordCount);
}
sw.Stop();
Console.WriteLine($"  Total: {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"  Per op: {sw.Elapsed.TotalMicroseconds / (iterations / 10):F3} us");
Console.WriteLine();

// Benchmark 4: Insert
Console.WriteLine($"Benchmark: Insert ({iterations / 100} iterations)");
sw.Restart();
for (int i = 0; i < iterations / 100; i++)
{
    var user = new AotUser
    {
        Name = $"NewUser{i}",
        Email = $"new{i}@test.com",
        Age = 25,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        Balance = 100m,
        Score = 50
    };
    await repo.InsertAsync(user);
}
sw.Stop();
Console.WriteLine($"  Total: {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"  Per op: {sw.Elapsed.TotalMicroseconds / (iterations / 100):F3} us");
Console.WriteLine();

Console.WriteLine("AOT Benchmark completed!");

namespace Sqlx.AotBenchmarks
{
    public class AotUser
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal Balance { get; set; }
        public string? Description { get; set; }
        public int Score { get; set; }
    }

    public class AotUserRepository
    {
        private readonly SqliteConnection _connection;

        private static readonly PlaceholderContext _context = new(
            SqlDefine.SQLite,
            "users",
            new ColumnMeta[]
            {
                new("id", "Id", DbType.Int64, false),
                new("name", "Name", DbType.String, false),
                new("email", "Email", DbType.String, false),
                new("age", "Age", DbType.Int32, false),
                new("is_active", "IsActive", DbType.Boolean, false),
                new("created_at", "CreatedAt", DbType.DateTime, false),
                new("updated_at", "UpdatedAt", DbType.DateTime, true),
                new("balance", "Balance", DbType.Decimal, false),
                new("description", "Description", DbType.String, true),
                new("score", "Score", DbType.Int32, false),
            });

        private static readonly SqlTemplate _getByIdTemplate = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id", _context);

        private static readonly SqlTemplate _countTemplate = SqlTemplate.Prepare(
            "SELECT COUNT(*) FROM {{table}}", _context);

        private static readonly SqlTemplate _insertTemplate = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", _context);

        private static readonly SqlTemplate _getPagedTemplate = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}", _context);

        public AotUserRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        public async Task<AotUser?> GetByIdAsync(long id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = _getByIdTemplate.Sql;
            AddParam(cmd, "@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUser(reader);
            }
            return null;
        }

        public async Task<long> CountAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = _countTemplate.Sql;
            return (long)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<long> InsertAsync(AotUser user)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = _insertTemplate.Sql + "; SELECT last_insert_rowid()";
            AddParam(cmd, "@name", user.Name);
            AddParam(cmd, "@email", user.Email);
            AddParam(cmd, "@age", user.Age);
            AddParam(cmd, "@is_active", user.IsActive ? 1 : 0);
            AddParam(cmd, "@created_at", user.CreatedAt.ToString("O"));
            AddParam(cmd, "@updated_at", user.UpdatedAt?.ToString("O") ?? (object)DBNull.Value);
            AddParam(cmd, "@balance", (double)user.Balance);
            AddParam(cmd, "@description", user.Description ?? (object)DBNull.Value);
            AddParam(cmd, "@score", user.Score);
            return (long)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<List<AotUser>> GetPagedAsync(int limit, int offset)
        {
            var result = new List<AotUser>(limit);
            var sql = _getPagedTemplate.Render(new Dictionary<string, object?>
            {
                ["limit"] = limit,
                ["offset"] = offset
            });

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(ReadUser(reader));
            }
            return result;
        }

        private static AotUser ReadUser(DbDataReader reader)
        {
            return new AotUser
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                Age = reader.GetInt32(3),
                IsActive = reader.GetInt64(4) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(5)),
                UpdatedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                Balance = (decimal)reader.GetDouble(7),
                Description = reader.IsDBNull(8) ? null : reader.GetString(8),
                Score = reader.GetInt32(9)
            };
        }

        private static void AddParam(SqliteCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}
