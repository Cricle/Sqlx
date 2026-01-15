// -----------------------------------------------------------------------
// <copyright file="SqlQueryBenchmark.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        [GlobalSetup]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE BenchmarkEntity (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    is_active INTEGER NOT NULL,
                    balance REAL NOT NULL,
                    created_at TEXT NOT NULL
                );

                INSERT INTO BenchmarkEntity (id, name, email, age, is_active, balance, created_at)
                SELECT 
                    value,
                    'User' || value,
                    CASE WHEN value % 3 = 0 THEN NULL ELSE 'user' || value || '@test.com' END,
                    18 + (value % 50),
                    value % 2,
                    1000.0 + (value * 100),
                    datetime('2024-01-01', '+' || value || ' days')
                FROM generate_series(1, 1000);
            ";
            cmd.ExecuteNonQuery();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _connection?.Dispose();
        }

        private static BenchmarkEntity MapEntity(IDataReader reader)
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

        #region Execution Benchmarks

        [Benchmark(Description = "Exec: SELECT * (1000 rows)")]
        public List<BenchmarkEntity> Exec_SelectAll()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .WithConnection(_connection)
                .WithMapper(MapEntity)
                .ToList();
        }

        [Benchmark(Description = "Exec: WHERE + ORDER + LIMIT")]
        public List<BenchmarkEntity> Exec_WhereOrderLimit()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.IsActive)
                .Where(u => u.Age >= 25)
                .OrderBy(u => u.Name)
                .Take(100)
                .WithConnection(_connection)
                .WithMapper(MapEntity)
                .ToList();
        }

        [Benchmark(Description = "Exec: Pagination")]
        public List<BenchmarkEntity> Exec_Pagination()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .OrderBy(u => u.Id)
                .Skip(500)
                .Take(100)
                .WithConnection(_connection)
                .WithMapper(MapEntity)
                .ToList();
        }

        [Benchmark(Description = "Exec: FirstOrDefault")]
        public BenchmarkEntity? Exec_FirstOrDefault()
        {
            return SqlQuery.ForSqlite<BenchmarkEntity>()
                .Where(u => u.Id == 500)
                .WithConnection(_connection)
                .WithMapper(MapEntity)
                .FirstOrDefault();
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
}
