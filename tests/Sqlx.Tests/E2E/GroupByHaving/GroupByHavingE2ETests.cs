// <copyright file="GroupByHavingE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.GroupByHaving;

/// <summary>
/// E2E tests for GROUP BY with HAVING clause.
/// </summary>
[TestClass]
public class GroupByHavingE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_orders (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    customer_name VARCHAR(100) NOT NULL,
                    product VARCHAR(100) NOT NULL,
                    quantity INT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_orders (
                    id BIGSERIAL PRIMARY KEY,
                    customer_name VARCHAR(100) NOT NULL,
                    product VARCHAR(100) NOT NULL,
                    quantity INT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_orders (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    customer_name NVARCHAR(100) NOT NULL,
                    product NVARCHAR(100) NOT NULL,
                    quantity INT NOT NULL,
                    amount DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_orders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    customer_name TEXT NOT NULL,
                    product TEXT NOT NULL,
                    quantity INTEGER NOT NULL,
                    amount REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var orders = new[]
        {
            ("Alice", "Laptop", 1, 1000m),
            ("Alice", "Mouse", 2, 50m),
            ("Bob", "Laptop", 2, 2000m),
            ("Bob", "Keyboard", 1, 100m),
            ("Charlie", "Monitor", 1, 300m),
        };

        foreach (var (customer, product, quantity, amount) in orders)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_orders (customer_name, product, quantity, amount) VALUES (@customer, @product, @quantity, @amount)";
            AddParameter(cmd, "@customer", customer);
            AddParameter(cmd, "@product", product);
            AddParameter(cmd, "@quantity", quantity);
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

    // ==================== HAVING with COUNT Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("MySQL")]
    public async Task MySQL_HavingCount_FiltersGroups()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Get customers with more than 1 order
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, COUNT(*) as order_count
            FROM test_orders
            GROUP BY customer_name
            HAVING COUNT(*) > 1
            ORDER BY customer_name";

        var results = new List<(string Customer, long Count)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert - Alice and Bob have 2 orders each
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0].Customer);
        Assert.AreEqual(2, results[0].Count);
        Assert.AreEqual("Bob", results[1].Customer);
        Assert.AreEqual(2, results[1].Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_HavingCount_FiltersGroups()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, COUNT(*) as order_count
            FROM test_orders
            GROUP BY customer_name
            HAVING COUNT(*) > 1
            ORDER BY customer_name";

        var results = new List<(string Customer, long Count)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_HavingCount_FiltersGroups()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, COUNT(*) as order_count
            FROM test_orders
            GROUP BY customer_name
            HAVING COUNT(*) > 1
            ORDER BY customer_name";

        var results = new List<(string Customer, int Count)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("SQLite")]
    public async Task SQLite_HavingCount_FiltersGroups()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, COUNT(*) as order_count
            FROM test_orders
            GROUP BY customer_name
            HAVING COUNT(*) > 1
            ORDER BY customer_name";

        var results = new List<(string Customer, long Count)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    // ==================== HAVING with SUM Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("MySQL")]
    public async Task MySQL_HavingSum_FiltersAggregates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Get customers with total amount > 500
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, SUM(amount) as total_amount
            FROM test_orders
            GROUP BY customer_name
            HAVING SUM(amount) > 500
            ORDER BY total_amount DESC";

        var results = new List<(string Customer, decimal Total)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert - Bob (2100) and Alice (1050)
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Customer);
        Assert.AreEqual(2100m, results[0].Total);
        Assert.AreEqual("Alice", results[1].Customer);
        Assert.AreEqual(1050m, results[1].Total);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("SQLite")]
    public async Task SQLite_HavingSum_FiltersAggregates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, SUM(amount) as total_amount
            FROM test_orders
            GROUP BY customer_name
            HAVING SUM(amount) > 500
            ORDER BY total_amount DESC";

        var results = new List<(string Customer, double Total)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Customer);
    }

    // ==================== HAVING with AVG Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("MySQL")]
    public async Task MySQL_HavingAvg_FiltersAverages()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Get customers with average order > 500
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, AVG(amount) as avg_amount
            FROM test_orders
            GROUP BY customer_name
            HAVING AVG(amount) > 500
            ORDER BY avg_amount DESC";

        var results = new List<(string Customer, decimal Avg)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert - Bob (1050 avg)
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Bob", results[0].Customer);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("SQLite")]
    public async Task SQLite_HavingAvg_FiltersAverages()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, AVG(amount) as avg_amount
            FROM test_orders
            GROUP BY customer_name
            HAVING AVG(amount) > 500
            ORDER BY avg_amount DESC";

        var results = new List<(string Customer, double Avg)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Bob", results[0].Customer);
    }

    // ==================== Multiple HAVING Conditions Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("GroupByHaving")]
    [TestCategory("SQLite")]
    public async Task SQLite_HavingMultipleConditions_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Get customers with count > 1 AND total > 1000
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT customer_name, COUNT(*) as order_count, SUM(amount) as total_amount
            FROM test_orders
            GROUP BY customer_name
            HAVING COUNT(*) > 1 AND SUM(amount) > 1000
            ORDER BY customer_name";

        var results = new List<(string Customer, long Count, double Total)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1), reader.GetDouble(2)));
        }

        // Assert - Only Bob (2 orders, 2100 total) and Alice (2 orders, 1050 total)
        Assert.AreEqual(2, results.Count);
    }
}
