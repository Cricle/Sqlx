using Dapper;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.AotBenchmarks;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

[module: DapperAot]

Console.WriteLine("Sqlx vs Dapper.AOT Benchmark");
Console.WriteLine("============================");
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

// Create repositories
var sqlxRepo = new AotUserRepository(connection);

// Warmup
Console.WriteLine("Warming up...");
for (int i = 0; i < 1000; i++)
{
    await sqlxRepo.GetByIdAsync(1);
    await sqlxRepo.CountAsync();
    await connection.QueryFirstOrDefaultAsync<DapperUser>("SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id", new { id = 1L });
    await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users");
}
Console.WriteLine();

// Benchmark settings
const int iterations = 100000;

// ============ GetById Benchmark ============
Console.WriteLine($"=== GetById ({iterations} iterations) ===");

// Sqlx
var sw = Stopwatch.StartNew();
for (int i = 0; i < iterations; i++)
{
    await sqlxRepo.GetByIdAsync((i % recordCount) + 1);
}
sw.Stop();
var sqlxGetById = sw.Elapsed.TotalMicroseconds / iterations;
Console.WriteLine($"  Sqlx:       {sqlxGetById:F3} us");

// Dapper.AOT
sw.Restart();
for (int i = 0; i < iterations; i++)
{
    await connection.QueryFirstOrDefaultAsync<DapperUser>(
        "SELECT id, name, email, age, is_active, created_at, updated_at, balance, description, score FROM users WHERE id = @id",
        new { id = (long)((i % recordCount) + 1) });
}
sw.Stop();
var dapperGetById = sw.Elapsed.TotalMicroseconds / iterations;
Console.WriteLine($"  Dapper.AOT: {dapperGetById:F3} us");
Console.WriteLine($"  Sqlx is {(dapperGetById / sqlxGetById - 1) * 100:F1}% faster");
Console.WriteLine();

// ============ Count Benchmark ============
Console.WriteLine($"=== Count ({iterations} iterations) ===");

// Sqlx
sw.Restart();
for (int i = 0; i < iterations; i++)
{
    await sqlxRepo.CountAsync();
}
sw.Stop();
var sqlxCount = sw.Elapsed.TotalMicroseconds / iterations;
Console.WriteLine($"  Sqlx:       {sqlxCount:F3} us");

// Dapper.AOT
sw.Restart();
for (int i = 0; i < iterations; i++)
{
    await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users");
}
sw.Stop();
var dapperCount = sw.Elapsed.TotalMicroseconds / iterations;
Console.WriteLine($"  Dapper.AOT: {dapperCount:F3} us");
Console.WriteLine($"  Sqlx is {(dapperCount / sqlxCount - 1) * 100:F1}% faster");
Console.WriteLine();

// ============ Insert Benchmark ============
Console.WriteLine($"=== Insert ({iterations / 100} iterations) ===");

// Sqlx
sw.Restart();
for (int i = 0; i < iterations / 100; i++)
{
    var user = new AotUser
    {
        Name = $"SqlxUser{i}",
        Email = $"sqlx{i}@test.com",
        Age = 25,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        Balance = 100m,
        Score = 50
    };
    await sqlxRepo.InsertAsync(user);
}
sw.Stop();
var sqlxInsert = sw.Elapsed.TotalMicroseconds / (iterations / 100);
Console.WriteLine($"  Sqlx:       {sqlxInsert:F3} us");

// Dapper.AOT
sw.Restart();
for (int i = 0; i < iterations / 100; i++)
{
    var user = new DapperInsertUser
    {
        Name = $"DapperUser{i}",
        Email = $"dapper{i}@test.com",
        Age = 25,
        is_active = 1,
        created_at = DateTime.UtcNow.ToString("O"),
        Balance = 100.0,
        Score = 50
    };
    await connection.ExecuteAsync(
        "INSERT INTO users (name, email, age, is_active, created_at, balance, score) VALUES (@Name, @Email, @Age, @is_active, @created_at, @Balance, @Score)",
        user);
}
sw.Stop();
var dapperInsert = sw.Elapsed.TotalMicroseconds / (iterations / 100);
Console.WriteLine($"  Dapper.AOT: {dapperInsert:F3} us");
Console.WriteLine($"  Sqlx is {(dapperInsert / sqlxInsert - 1) * 100:F1}% faster");
Console.WriteLine();

// ============ Summary ============
Console.WriteLine("=== Summary ===");
Console.WriteLine($"| Operation | Sqlx | Dapper.AOT | Sqlx Advantage |");
Console.WriteLine($"|-----------|------|------------|----------------|");
Console.WriteLine($"| GetById   | {sqlxGetById:F2} us | {dapperGetById:F2} us | {(dapperGetById / sqlxGetById - 1) * 100:F1}% faster |");
Console.WriteLine($"| Count     | {sqlxCount:F2} us | {dapperCount:F2} us | {(dapperCount / sqlxCount - 1) * 100:F1}% faster |");
Console.WriteLine($"| Insert    | {sqlxInsert:F2} us | {dapperInsert:F2} us | {(dapperInsert / sqlxInsert - 1) * 100:F1}% faster |");
Console.WriteLine();

Console.WriteLine("AOT Benchmark completed!");

namespace Sqlx.AotBenchmarks
{
    // Dapper.AOT entity for SELECT - use snake_case column names
    public class DapperUser
    {
        public long id { get; set; }
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public int age { get; set; }
        public int is_active { get; set; }
        public string created_at { get; set; } = string.Empty;
        public string? updated_at { get; set; }
        public double balance { get; set; }
        public string? description { get; set; }
        public int score { get; set; }
    }

    // Dapper.AOT entity for INSERT
    public class DapperInsertUser
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public int is_active { get; set; }
        public string created_at { get; set; } = string.Empty;
        public double Balance { get; set; }
        public int Score { get; set; }
    }

    // Sqlx entity
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
