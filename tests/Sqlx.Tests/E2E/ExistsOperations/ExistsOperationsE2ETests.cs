// <copyright file="ExistsOperationsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.ExistsOperations;

/// <summary>
/// E2E tests for EXISTS and NOT EXISTS operations.
/// </summary>
[TestClass]
public class ExistsOperationsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        var customersTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_customers (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_customers (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_customers (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_customers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        var ordersTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_orders (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    customer_id BIGINT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_orders (
                    id BIGSERIAL PRIMARY KEY,
                    customer_id BIGINT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_orders (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    customer_id BIGINT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_orders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    customer_id INTEGER NOT NULL,
                    amount REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        return customersTable + ";\n" + ordersTable;
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        // Insert customers
        var customers = new[] { "Alice", "Bob", "Charlie" };
        foreach (var name in customers)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_customers (name) VALUES (@name)";
            AddParameter(cmd, "@name", name);
            await cmd.ExecuteNonQueryAsync();
        }

        // Insert orders (only for Alice and Bob)
        var orders = new[] { (1L, 100m), (1L, 200m), (2L, 150m) };
        foreach (var (customerId, amount) in orders)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_orders (customer_id, amount) VALUES (@customerId, @amount)";
            AddParameter(cmd, "@customerId", customerId);
            AddParameter(cmd, "@amount", amount);
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

    // ==================== EXISTS Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_Exists_FindsMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Find customers with orders
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Alice and Bob have orders
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0]);
        Assert.AreEqual("Bob", results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Exists_FindsMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Exists_FindsMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_Exists_FindsMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    // ==================== NOT EXISTS Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_NotExists_FindsNonMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Find customers without orders
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE NOT EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Only Charlie has no orders
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Charlie", results[0]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_NotExists_FindsNonMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE NOT EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Charlie", results[0]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_NotExists_FindsNonMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE NOT EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Charlie", results[0]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("ExistsOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_NotExists_FindsNonMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_customers c
            WHERE NOT EXISTS (
                SELECT 1 FROM test_orders o WHERE o.customer_id = c.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Charlie", results[0]);
    }
}
