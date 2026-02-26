// <copyright file="JoinsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Joins;

/// <summary>
/// E2E tests for SQL JOIN operations (INNER, LEFT, RIGHT, FULL OUTER).
/// </summary>
[TestClass]
public class JoinsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        var usersTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        var ordersTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_orders (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    product VARCHAR(100) NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_orders (
                    id BIGSERIAL PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    product VARCHAR(100) NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_orders (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    product NVARCHAR(100) NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_orders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    product TEXT NOT NULL,
                    amount REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        return usersTable + ";\n" + ordersTable;
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        // Insert users
        var users = new[] { ("Alice", "alice@example.com"), ("Bob", "bob@example.com"), ("Charlie", "charlie@example.com") };
        foreach (var (name, email) in users)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_users (name, email) VALUES (@name, @email)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@email", email);
            await cmd.ExecuteNonQueryAsync();
        }

        // Insert orders (Alice has 2 orders, Bob has 1 order, Charlie has no orders)
        var orders = new[] { (1L, "Laptop", 999.99m), (1L, "Mouse", 29.99m), (2L, "Keyboard", 79.99m) };
        foreach (var (userId, product, amount) in orders)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_orders (user_id, product, amount) VALUES (@user_id, @product, @amount)";
            AddParameter(cmd, "@user_id", userId);
            AddParameter(cmd, "@product", product);
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

    // ==================== INNER JOIN Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("MySQL")]
    public async Task MySQL_InnerJoin_ReturnsMatchingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product, o.amount
            FROM test_users u
            INNER JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string Product, decimal Amount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
        }

        // Assert - Only users with orders (Alice and Bob)
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Laptop", results[0].Product);
        Assert.AreEqual("Alice", results[1].Name);
        Assert.AreEqual("Mouse", results[1].Product);
        Assert.AreEqual("Bob", results[2].Name);
        Assert.AreEqual("Keyboard", results[2].Product);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_InnerJoin_ReturnsMatchingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product, o.amount
            FROM test_users u
            INNER JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string Product, decimal Amount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Bob", results[2].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_InnerJoin_ReturnsMatchingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product, o.amount
            FROM test_users u
            INNER JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string Product, decimal Amount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Bob", results[2].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("SQLite")]
    public async Task SQLite_InnerJoin_ReturnsMatchingRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product, o.amount
            FROM test_users u
            INNER JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string Product, double Amount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetDouble(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Bob", results[2].Name);
    }

    // ==================== LEFT JOIN Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("MySQL")]
    public async Task MySQL_LeftJoin_ReturnsAllLeftRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string? Product)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        // Assert - All users including Charlie (with NULL order)
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Laptop", results[0].Product);
        Assert.AreEqual("Alice", results[1].Name);
        Assert.AreEqual("Mouse", results[1].Product);
        Assert.AreEqual("Bob", results[2].Name);
        Assert.AreEqual("Keyboard", results[2].Product);
        Assert.AreEqual("Charlie", results[3].Name);
        Assert.IsNull(results[3].Product);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_LeftJoin_ReturnsAllLeftRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string? Product)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Charlie", results[3].Name);
        Assert.IsNull(results[3].Product);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_LeftJoin_ReturnsAllLeftRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string? Product)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Charlie", results[3].Name);
        Assert.IsNull(results[3].Product);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("SQLite")]
    public async Task SQLite_LeftJoin_ReturnsAllLeftRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, o.product
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            ORDER BY u.name, o.product";

        var results = new List<(string Name, string? Product)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Charlie", results[3].Name);
        Assert.IsNull(results[3].Product);
    }

    // ==================== Aggregation with JOIN Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("MySQL")]
    public async Task MySQL_JoinWithAggregation_CalculatesCorrectTotals()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, COUNT(o.id) as order_count, COALESCE(SUM(o.amount), 0) as total_amount
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            GROUP BY u.id, u.name
            ORDER BY u.name";

        var results = new List<(string Name, long OrderCount, decimal TotalAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), Convert.ToInt64(reader.GetValue(1)), reader.GetDecimal(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual(2, results[0].OrderCount);
        Assert.AreEqual(1029.98m, results[0].TotalAmount, 0.01m);
        
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual(1, results[1].OrderCount);
        Assert.AreEqual(79.99m, results[1].TotalAmount, 0.01m);
        
        Assert.AreEqual("Charlie", results[2].Name);
        Assert.AreEqual(0, results[2].OrderCount);
        Assert.AreEqual(0m, results[2].TotalAmount);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_JoinWithAggregation_CalculatesCorrectTotals()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, COUNT(o.id) as order_count, COALESCE(SUM(o.amount), 0) as total_amount
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            GROUP BY u.id, u.name
            ORDER BY u.name";

        var results = new List<(string Name, long OrderCount, decimal TotalAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1), reader.GetDecimal(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual(2, results[0].OrderCount);
        Assert.AreEqual("Charlie", results[2].Name);
        Assert.AreEqual(0, results[2].OrderCount);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_JoinWithAggregation_CalculatesCorrectTotals()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, COUNT(o.id) as order_count, COALESCE(SUM(o.amount), 0) as total_amount
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            GROUP BY u.id, u.name
            ORDER BY u.name";

        var results = new List<(string Name, int OrderCount, decimal TotalAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1), reader.GetDecimal(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual(2, results[0].OrderCount);
        Assert.AreEqual("Charlie", results[2].Name);
        Assert.AreEqual(0, results[2].OrderCount);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Joins")]
    [TestCategory("SQLite")]
    public async Task SQLite_JoinWithAggregation_CalculatesCorrectTotals()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, COUNT(o.id) as order_count, COALESCE(SUM(o.amount), 0) as total_amount
            FROM test_users u
            LEFT JOIN test_orders o ON u.id = o.user_id
            GROUP BY u.id, u.name
            ORDER BY u.name";

        var results = new List<(string Name, long OrderCount, double TotalAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1), reader.GetDouble(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual(2, results[0].OrderCount);
        Assert.AreEqual(1029.98, results[0].TotalAmount, 0.01);
        Assert.AreEqual("Charlie", results[2].Name);
        Assert.AreEqual(0, results[2].OrderCount);
    }
}
