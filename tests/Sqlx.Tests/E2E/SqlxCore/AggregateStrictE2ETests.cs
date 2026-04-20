// <copyright file="AggregateStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试聚合函数在所有数据库上的 SQL 转换。
/// Strict E2E tests for aggregate translation across all databases.
/// </summary>
[TestClass]
public class AggregateStrictE2ETests : E2ETestBase
{
    public class TestEntity
    {
        public long Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Value { get; set; }
        public decimal Price { get; set; }
    }

    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_entities (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    category VARCHAR(50) NOT NULL,
                    value INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_entities (
                    id BIGSERIAL PRIMARY KEY,
                    category VARCHAR(50) NOT NULL,
                    value INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_entities (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    category NVARCHAR(50) NOT NULL,
                    value INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category TEXT NOT NULL,
                    value INTEGER NOT NULL,
                    price REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Aggregate Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("MySQL")]
    public async Task MySQL_GroupBy_Count_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var sql = SqlQuery<TestEntity>.ForMySql()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("COUNT(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("MySQL")]
    public async Task MySQL_GroupBy_Sum_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var sql = SqlQuery<TestEntity>.ForMySql()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Total = x.Sum(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("SUM(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("MySQL")]
    public async Task MySQL_GroupBy_Average_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var sql = SqlQuery<TestEntity>.ForMySql()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Avg = x.Average(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("AVG(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("MySQL")]
    public async Task MySQL_GroupBy_Min_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var sql = SqlQuery<TestEntity>.ForMySql()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Min = x.Min(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MIN(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("MySQL")]
    public async Task MySQL_GroupBy_Max_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var sql = SqlQuery<TestEntity>.ForMySql()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Max = x.Max(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MAX(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    // ==================== PostgreSQL Aggregate Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GroupBy_Count_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var sql = SqlQuery<TestEntity>.ForPostgreSQL()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("COUNT(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GroupBy_Sum_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var sql = SqlQuery<TestEntity>.ForPostgreSQL()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Total = x.Sum(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("SUM(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GroupBy_Average_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var sql = SqlQuery<TestEntity>.ForPostgreSQL()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Avg = x.Average(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("AVG(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GroupBy_Min_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var sql = SqlQuery<TestEntity>.ForPostgreSQL()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Min = x.Min(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MIN(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GroupBy_Max_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var sql = SqlQuery<TestEntity>.ForPostgreSQL()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Max = x.Max(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MAX(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    // ==================== SQL Server Aggregate Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GroupBy_Count_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var sql = SqlQuery<TestEntity>.ForSqlServer()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("COUNT(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GroupBy_Sum_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var sql = SqlQuery<TestEntity>.ForSqlServer()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Total = x.Sum(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("SUM(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GroupBy_Average_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var sql = SqlQuery<TestEntity>.ForSqlServer()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Avg = x.Average(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("AVG(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GroupBy_Min_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var sql = SqlQuery<TestEntity>.ForSqlServer()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Min = x.Min(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MIN(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GroupBy_Max_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var sql = SqlQuery<TestEntity>.ForSqlServer()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Max = x.Max(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MAX(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    // ==================== SQLite Aggregate Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SQLite")]
    public async Task SQLite_GroupBy_Count_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var sql = SqlQuery<TestEntity>.ForSqlite()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("COUNT(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SQLite")]
    public async Task SQLite_GroupBy_Sum_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var sql = SqlQuery<TestEntity>.ForSqlite()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Total = x.Sum(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("SUM(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SQLite")]
    public async Task SQLite_GroupBy_Average_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var sql = SqlQuery<TestEntity>.ForSqlite()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Avg = x.Average(y => y.Value) })
            .ToSql();

        Assert.IsTrue(sql.Contains("AVG(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SQLite")]
    public async Task SQLite_GroupBy_Min_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var sql = SqlQuery<TestEntity>.ForSqlite()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Min = x.Min(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MIN(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregate")]
    [TestCategory("SQLite")]
    public async Task SQLite_GroupBy_Max_GeneratesCorrectSql()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var sql = SqlQuery<TestEntity>.ForSqlite()
            .GroupBy(x => x.Category)
            .Select(x => new { x.Key, Max = x.Max(y => y.Price) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MAX(", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }
}
