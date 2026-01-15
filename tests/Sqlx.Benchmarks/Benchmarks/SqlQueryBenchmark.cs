// -----------------------------------------------------------------------
// <copyright file="SqlQueryBenchmark.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Data.Sqlite;

namespace Sqlx.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark for IQueryable SQL generation performance.
    /// Tests the speed of generating SQL from LINQ expressions.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class SqlQueryBenchmark
    {
        private SqliteConnection _connection = null!;
        private BenchmarkEntityReader _reader = null!;

        [GlobalSetup]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _reader = new BenchmarkEntityReader();

            using var createCmd = _connection.CreateCommand();
            createCmd.CommandText = @"
                CREATE TABLE BenchmarkEntity (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    is_active INTEGER NOT NULL,
                    balance REAL NOT NULL,
                    created_at TEXT NOT NULL
                )";
            createCmd.ExecuteNonQuery();

            // Insert 1000 rows using parameterized batch insert
            using var transaction = _connection.BeginTransaction();
            using var insertCmd = _connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = @"
                INSERT INTO BenchmarkEntity (id, name, email, age, is_active, balance, created_at)
                VALUES (@id, @name, @email, @age, @is_active, @balance, @created_at)";

            var idParam = insertCmd.CreateParameter();
            idParam.ParameterName = "@id";
            insertCmd.Parameters.Add(idParam);

            var nameParam = insertCmd.CreateParameter();
            nameParam.ParameterName = "@name";
            insertCmd.Parameters.Add(nameParam);

            var emailParam = insertCmd.CreateParameter();
            emailParam.ParameterName = "@email";
            insertCmd.Parameters.Add(emailParam);

            var ageParam = insertCmd.CreateParameter();
            ageParam.ParameterName = "@age";
            insertCmd.Parameters.Add(ageParam);

            var isActiveParam = insertCmd.CreateParameter();
            isActiveParam.ParameterName = "@is_active";
            insertCmd.Parameters.Add(isActiveParam);

            var balanceParam = insertCmd.CreateParameter();
            balanceParam.ParameterName = "@balance";
            insertCmd.Parameters.Add(balanceParam);

            var createdAtParam = insertCmd.CreateParameter();
            createdAtParam.ParameterName = "@created_at";
            insertCmd.Parameters.Add(createdAtParam);

            var baseDate = new DateTime(2024, 1, 1);
            for (int i = 1; i <= 1000; i++)
            {
                idParam.Value = i;
                nameParam.Value = "User" + i;
                emailParam.Value = i % 3 == 0 ? DBNull.Value : (object)$"user{i}@test.com";
                ageParam.Value = 18 + (i % 50);
                isActiveParam.Value = i % 2;
                balanceParam.Value = 1000.0 + (i * 100);
                createdAtParam.Value = baseDate.AddDays(i).ToString("yyyy-MM-dd HH:mm:ss");
                insertCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _connection?.Dispose();
        }

        #region SQL Generation Benchmarks

        [Benchmark(Description = "Gen: SELECT *")]
        public string Gen_SimpleSelect()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>().ToSql();
        }

        [Benchmark(Description = "Gen: WHERE single")]
        public string Gen_WhereSingle()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Id == 1)
                .ToSql();
        }

        [Benchmark(Description = "Gen: WHERE AND")]
        public string Gen_WhereMultipleAnd()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive && u.Age >= 18 && u.Age <= 65)
                .ToSql();
        }

        [Benchmark(Description = "Gen: WHERE OR")]
        public string Gen_WhereWithOr()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Age < 18 || u.Age > 65 || !u.IsActive)
                .ToSql();
        }

        [Benchmark(Description = "Gen: Full chain")]
        public string Gen_FullChain()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .Where(u => u.Age >= 18)
                .Select(u => new { u.Id, u.Name, u.Age })
                .OrderBy(u => u.Name)
                .ThenByDescending(u => u.Age)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "Gen: String functions")]
        public string Gen_StringFunctions()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Name.Contains("test") && u.Name.ToUpper() == "TEST")
                .ToSql();
        }

        [Benchmark(Description = "Gen: Math functions")]
        public string Gen_MathFunctions()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => Math.Abs(u.Age) > 0 && Math.Round((double)u.Balance) > 1000)
                .ToSql();
        }

        #endregion

        #region Parameterized SQL Benchmarks

        [Benchmark(Description = "Param: Simple")]
        public (string, IEnumerable<KeyValuePair<string, object?>>) Param_Simple()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Id == 1)
                .ToSqlWithParameters();
        }

        [Benchmark(Description = "Param: Complex")]
        public (string, IEnumerable<KeyValuePair<string, object?>>) Param_Complex()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive && u.Age >= 18 && u.Name == "test")
                .ToSqlWithParameters();
        }

        #endregion

        #region Synchronous Execution Benchmarks

        [Benchmark(Description = "Sync: SELECT * (1000 rows)")]
        public List<BenchmarkEntity> Sync_SelectAll()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .WithConnection(_connection)
                .WithReader(_reader)
                .ToList();
        }

        [Benchmark(Description = "Sync: WHERE + ORDER + LIMIT")]
        public List<BenchmarkEntity> Sync_WhereOrderLimit()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .Where(u => u.Age >= 25)
                .OrderBy(u => u.Name)
                .Take(100)
                .WithConnection(_connection)
                .WithReader(_reader)
                .ToList();
        }

        [Benchmark(Description = "Sync: Pagination")]
        public List<BenchmarkEntity> Sync_Pagination()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .OrderBy(u => u.Id)
                .Skip(500)
                .Take(100)
                .WithConnection(_connection)
                .WithReader(_reader)
                .ToList();
        }

        [Benchmark(Description = "Sync: FirstOrDefault")]
        public BenchmarkEntity? Sync_FirstOrDefault()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Id == 500)
                .WithConnection(_connection)
                .WithReader(_reader)
                .FirstOrDefault();
        }

        #endregion

        #region Asynchronous Execution Benchmarks (System.Linq.Async)

        [Benchmark(Description = "Async: ToListAsync (1000 rows)")]
        public async Task<List<BenchmarkEntity>> Async_ToListAsync()
        {
            var query = (IAsyncEnumerable<BenchmarkEntity>)SqlQuery.ForSqlite<BenchmarkEntity>()
                .WithConnection(_connection)
                .WithReader(_reader);
            return await System.Linq.AsyncEnumerable.ToListAsync(query);
        }

        [Benchmark(Description = "Async: WHERE + ToListAsync")]
        public async Task<List<BenchmarkEntity>> Async_WhereToListAsync()
        {
            var query = (IAsyncEnumerable<BenchmarkEntity>)SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .Where(u => u.Age >= 25)
                .OrderBy(u => u.Name)
                .Take(100)
                .WithConnection(_connection)
                .WithReader(_reader);
            return await System.Linq.AsyncEnumerable.ToListAsync(query);
        }

        [Benchmark(Description = "Async: FirstOrDefaultAsync")]
        public async Task<BenchmarkEntity?> Async_FirstOrDefaultAsync()
        {
            var query = (IAsyncEnumerable<BenchmarkEntity>)SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Id == 500)
                .WithConnection(_connection)
                .WithReader(_reader);
            return await System.Linq.AsyncEnumerable.FirstOrDefaultAsync(query);
        }

        [Benchmark(Description = "Async: CountAsync")]
        public async Task<int> Async_CountAsync()
        {
            var query = (IAsyncEnumerable<BenchmarkEntity>)SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .WithConnection(_connection)
                .WithReader(_reader);
            return await System.Linq.AsyncEnumerable.CountAsync(query);
        }

        [Benchmark(Description = "Async: AnyAsync")]
        public async Task<bool> Async_AnyAsync()
        {
            var query = (IAsyncEnumerable<BenchmarkEntity>)SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Id == 500)
                .WithConnection(_connection)
                .WithReader(_reader);
            return await System.Linq.AsyncEnumerable.AnyAsync(query);
        }

        [Benchmark(Description = "Async: Pagination ToListAsync")]
        public async Task<List<BenchmarkEntity>> Async_PaginationToListAsync()
        {
            var query = (IAsyncEnumerable<BenchmarkEntity>)SqlQuery.ForSqlite<BenchmarkEntity>()
                .OrderBy(u => u.Id)
                .Skip(500)
                .Take(100)
                .WithConnection(_connection)
                .WithReader(_reader);
            return await System.Linq.AsyncEnumerable.ToListAsync(query);
        }

        #endregion

        #region Multi-Dialect Benchmarks

        [Benchmark(Description = "Dialect: SQLite")]
        public string Dialect_Sqlite()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "Dialect: SqlServer")]
        public string Dialect_SqlServer()
        {
            return SqlQuery.ForSqlServer<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "Dialect: MySQL")]
        public string Dialect_MySql()
        {
            return SqlQuery.ForMySql<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "Dialect: PostgreSQL")]
        public string Dialect_PostgreSql()
        {
            return SqlQuery.ForPostgreSQL<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "Dialect: Oracle")]
        public string Dialect_Oracle()
        {
            return SqlQuery.ForOracle<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        #endregion
    }

    /// <summary>
    /// Entity for benchmark tests.
    /// </summary>
    public class BenchmarkEntity
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public int Age { get; set; }

        public bool IsActive { get; set; }

        public decimal Balance { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Result reader for BenchmarkEntity.
    /// </summary>
    public class BenchmarkEntityReader : IResultReader<BenchmarkEntity>
    {
        public BenchmarkEntity Read(IDataReader reader)
        {
            return new BenchmarkEntity
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                Age = reader.GetInt32(3),
                IsActive = reader.GetInt32(4) == 1,
                Balance = (decimal)reader.GetDouble(5),
                CreatedAt = DateTime.Parse(reader.GetString(6))
            };
        }

        public BenchmarkEntity Read(IDataReader reader, int[] ordinals)
        {
            return new BenchmarkEntity
            {
                Id = reader.GetInt64(ordinals[0]),
                Name = reader.GetString(ordinals[1]),
                Email = reader.IsDBNull(ordinals[2]) ? null : reader.GetString(ordinals[2]),
                Age = reader.GetInt32(ordinals[3]),
                IsActive = reader.GetInt32(ordinals[4]) == 1,
                Balance = (decimal)reader.GetDouble(ordinals[5]),
                CreatedAt = DateTime.Parse(reader.GetString(ordinals[6]))
            };
        }

        public int[] GetOrdinals(IDataReader reader)
        {
            return new[]
            {
                reader.GetOrdinal("id"),
                reader.GetOrdinal("name"),
                reader.GetOrdinal("email"),
                reader.GetOrdinal("age"),
                reader.GetOrdinal("is_active"),
                reader.GetOrdinal("balance"),
                reader.GetOrdinal("created_at")
            };
        }
    }
}
