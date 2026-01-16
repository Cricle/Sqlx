// -----------------------------------------------------------------------
// <copyright file="SqlKataComparisonBenchmark.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SqlKata;
using SqlKata.Compilers;

namespace Sqlx.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark comparing Sqlx IQueryable vs SqlKata for SQL generation.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class SqlKataComparisonBenchmark
    {
        private SqliteCompiler _sqliteCompiler = null!;
        private SqlServerCompiler _sqlServerCompiler = null!;
        private MySqlCompiler _mySqlCompiler = null!;
        private PostgresCompiler _postgresCompiler = null!;

        [GlobalSetup]
        public void Setup()
        {
            _sqliteCompiler = new SqliteCompiler();
            _sqlServerCompiler = new SqlServerCompiler();
            _mySqlCompiler = new MySqlCompiler();
            _postgresCompiler = new PostgresCompiler();
        }

        #region Simple SELECT

        [Benchmark(Description = "Sqlx: SELECT *")]
        public string Sqlx_SimpleSelect()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlite().ToSql();
        }

        [Benchmark(Description = "SqlKata: SELECT *")]
        public string SqlKata_SimpleSelect()
        {
            var query = new Query("BenchmarkEntity").Select("*");
            return _sqliteCompiler.Compile(query).Sql;
        }

        #endregion

        #region WHERE single condition

        [Benchmark(Description = "Sqlx: WHERE single")]
        public string Sqlx_WhereSingle()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlite()
                .Where(u => u.Id == 1)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: WHERE single")]
        public string SqlKata_WhereSingle()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where("id", 1);
            return _sqliteCompiler.Compile(query).Sql;
        }

        #endregion

        #region WHERE multiple AND

        [Benchmark(Description = "Sqlx: WHERE AND")]
        public string Sqlx_WhereAnd()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlite()
                .Where(u => u.IsActive && u.Age >= 18 && u.Age <= 65)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: WHERE AND")]
        public string SqlKata_WhereAnd()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where("is_active", 1)
                .Where("age", ">=", 18)
                .Where("age", "<=", 65);
            return _sqliteCompiler.Compile(query).Sql;
        }

        #endregion

        #region WHERE with OR

        [Benchmark(Description = "Sqlx: WHERE OR")]
        public string Sqlx_WhereOr()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlite()
                .Where(u => u.Age < 18 || u.Age > 65 || !u.IsActive)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: WHERE OR")]
        public string SqlKata_WhereOr()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where(q => q
                    .Where("age", "<", 18)
                    .OrWhere("age", ">", 65)
                    .OrWhere("is_active", 0));
            return _sqliteCompiler.Compile(query).Sql;
        }

        #endregion

        #region Full chain query

        [Benchmark(Description = "Sqlx: Full chain")]
        public string Sqlx_FullChain()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlite()
                .Where(u => u.IsActive)
                .Where(u => u.Age >= 18)
                .Select(u => new { u.Id, u.Name, u.Age })
                .OrderBy(u => u.Name)
                .ThenByDescending(u => u.Age)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: Full chain")]
        public string SqlKata_FullChain()
        {
            var query = new Query("BenchmarkEntity")
                .Select("id", "name", "age")
                .Where("is_active", 1)
                .Where("age", ">=", 18)
                .OrderBy("name")
                .OrderByDesc("age")
                .Skip(10)
                .Take(20);
            return _sqliteCompiler.Compile(query).Sql;
        }

        #endregion

        #region Parameterized query

        [Benchmark(Description = "Sqlx: Parameterized")]
        public (string, IEnumerable<KeyValuePair<string, object?>>) Sqlx_Parameterized()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlite()
                .Where(u => u.IsActive && u.Age >= 18 && u.Name == "test")
                .ToSqlWithParameters();
        }

        [Benchmark(Description = "SqlKata: Parameterized")]
        public SqlResult SqlKata_Parameterized()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where("is_active", 1)
                .Where("age", ">=", 18)
                .Where("name", "test");
            return _sqliteCompiler.Compile(query);
        }

        #endregion

        #region Multi-dialect comparison

        [Benchmark(Description = "Sqlx: SQLite dialect")]
        public string Sqlx_Sqlite()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlite()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: SQLite dialect")]
        public string SqlKata_Sqlite()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where("is_active", 1)
                .OrderBy("name")
                .Skip(10)
                .Take(20);
            return _sqliteCompiler.Compile(query).Sql;
        }

        [Benchmark(Description = "Sqlx: SqlServer dialect")]
        public string Sqlx_SqlServer()
        {
            return SqlQuery<BenchmarkEntity>.ForSqlServer()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: SqlServer dialect")]
        public string SqlKata_SqlServer()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where("is_active", 1)
                .OrderBy("name")
                .Skip(10)
                .Take(20);
            return _sqlServerCompiler.Compile(query).Sql;
        }

        [Benchmark(Description = "Sqlx: MySQL dialect")]
        public string Sqlx_MySql()
        {
            return SqlQuery<BenchmarkEntity>.ForMySql()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: MySQL dialect")]
        public string SqlKata_MySql()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where("is_active", 1)
                .OrderBy("name")
                .Skip(10)
                .Take(20);
            return _mySqlCompiler.Compile(query).Sql;
        }

        [Benchmark(Description = "Sqlx: PostgreSQL dialect")]
        public string Sqlx_PostgreSql()
        {
            return SqlQuery<BenchmarkEntity>.ForPostgreSQL()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(20)
                .ToSql();
        }

        [Benchmark(Description = "SqlKata: PostgreSQL dialect")]
        public string SqlKata_PostgreSql()
        {
            var query = new Query("BenchmarkEntity")
                .Select("*")
                .Where("is_active", 1)
                .OrderBy("name")
                .Skip(10)
                .Take(20);
            return _postgresCompiler.Compile(query).Sql;
        }

        #endregion
    }
}

