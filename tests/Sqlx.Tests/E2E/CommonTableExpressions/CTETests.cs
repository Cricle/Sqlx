// <copyright file="CTETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.CommonTableExpressions;

/// <summary>
/// E2E tests for Common Table Expressions (CTEs) and recursive CTEs.
/// </summary>
[TestClass]
public class CTETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_employees (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    manager_id BIGINT NULL,
                    salary DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_employees (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    manager_id BIGINT NULL,
                    salary DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_employees (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    manager_id BIGINT NULL,
                    salary DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_employees (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    manager_id INTEGER NULL,
                    salary REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var employees = new[]
        {
            (1L, "CEO", (long?)null, 200000m),
            (2L, "VP Sales", 1L, 150000m),
            (3L, "VP Engineering", 1L, 160000m),
            (4L, "Sales Manager", 2L, 100000m),
            (5L, "Engineer", 3L, 90000m),
        };

        foreach (var (id, name, managerId, salary) in employees)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_employees (id, name, manager_id, salary) VALUES (@id, @name, @managerId, @salary)";
            AddParameter(cmd, "@id", id);
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@managerId", managerId);
            AddParameter(cmd, "@salary", salary);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== Simple CTE Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("MySQL")]
    public async Task MySQL_SimpleCTE_FiltersData()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH high_earners AS (
                SELECT name, salary
                FROM test_employees
                WHERE salary >= 100000
            )
            SELECT name FROM high_earners ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Contains("CEO"));
        Assert.IsTrue(results.Contains("VP Sales"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SimpleCTE_FiltersData()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH high_earners AS (
                SELECT name, salary
                FROM test_employees
                WHERE salary >= 100000
            )
            SELECT name FROM high_earners ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SimpleCTE_FiltersData()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH high_earners AS (
                SELECT name, salary
                FROM test_employees
                WHERE salary >= 100000
            )
            SELECT name FROM high_earners ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("SQLite")]
    public async Task SQLite_SimpleCTE_FiltersData()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH high_earners AS (
                SELECT name, salary
                FROM test_employees
                WHERE salary >= 100000
            )
            SELECT name FROM high_earners ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
    }

    // ==================== Recursive CTE Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("MySQL")]
    public async Task MySQL_RecursiveCTE_BuildsHierarchy()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH RECURSIVE org_chart AS (
                SELECT id, name, manager_id, 0 as level
                FROM test_employees
                WHERE manager_id IS NULL
                UNION ALL
                SELECT e.id, e.name, e.manager_id, oc.level + 1
                FROM test_employees e
                INNER JOIN org_chart oc ON e.manager_id = oc.id
            )
            SELECT name, level FROM org_chart ORDER BY level, name";

        var results = new List<(string Name, long Level)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("CEO", results[0].Name);
        Assert.AreEqual(0, results[0].Level);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_RecursiveCTE_BuildsHierarchy()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH RECURSIVE org_chart AS (
                SELECT id, name, manager_id, 0 as level
                FROM test_employees
                WHERE manager_id IS NULL
                UNION ALL
                SELECT e.id, e.name, e.manager_id, oc.level + 1
                FROM test_employees e
                INNER JOIN org_chart oc ON e.manager_id = oc.id
            )
            SELECT name, level FROM org_chart ORDER BY level, name";

        var results = new List<(string Name, int Level)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_RecursiveCTE_BuildsHierarchy()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH org_chart AS (
                SELECT id, name, manager_id, 0 as level
                FROM test_employees
                WHERE manager_id IS NULL
                UNION ALL
                SELECT e.id, e.name, e.manager_id, oc.level + 1
                FROM test_employees e
                INNER JOIN org_chart oc ON e.manager_id = oc.id
            )
            SELECT name, level FROM org_chart ORDER BY level, name";

        var results = new List<(string Name, int Level)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CTE")]
    [TestCategory("SQLite")]
    public async Task SQLite_RecursiveCTE_BuildsHierarchy()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            WITH RECURSIVE org_chart AS (
                SELECT id, name, manager_id, 0 as level
                FROM test_employees
                WHERE manager_id IS NULL
                UNION ALL
                SELECT e.id, e.name, e.manager_id, oc.level + 1
                FROM test_employees e
                INNER JOIN org_chart oc ON e.manager_id = oc.id
            )
            SELECT name, level FROM org_chart ORDER BY level, name";

        var results = new List<(string Name, long Level)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
    }
}
