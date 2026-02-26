// <copyright file="InOperationsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.InOperations;

/// <summary>
/// E2E tests for SQL IN and NOT IN operations.
/// </summary>
[TestClass]
public class InOperationsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_products (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    category VARCHAR(50) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    status VARCHAR(20) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    category VARCHAR(50) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    status VARCHAR(20) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_products (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    category NVARCHAR(50) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    status NVARCHAR(20) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    category TEXT NOT NULL,
                    price REAL NOT NULL,
                    status TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        var products = new[]
        {
            ("Laptop", "Electronics", 999.99m, "Active"),
            ("Mouse", "Electronics", 29.99m, "Active"),
            ("Desk", "Furniture", 299.99m, "Discontinued"),
            ("Chair", "Furniture", 199.99m, "Active"),
            ("Monitor", "Electronics", 399.99m, "Active"),
            ("Table", "Furniture", 499.99m, "Discontinued"),
            ("Keyboard", "Electronics", 79.99m, "OutOfStock"),
        };

        foreach (var (name, category, price, status) in products)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_products (name, category, price, status) VALUES (@name, @category, @price, @status)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@category", category);
            AddParameter(cmd, "@price", price);
            AddParameter(cmd, "@status", status);
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

    // ==================== IN with Literal Values Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_InWithLiterals_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get products in specific categories
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE category IN ('Electronics', 'Furniture') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - All 7 products are in these categories
        Assert.AreEqual(7, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_InWithLiterals_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE category IN ('Electronics', 'Furniture') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(7, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_InWithLiterals_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE category IN ('Electronics', 'Furniture') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(7, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_InWithLiterals_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE category IN ('Electronics', 'Furniture') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(7, results.Count);
    }

    // ==================== NOT IN Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_NotIn_ExcludesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get products NOT in discontinued or out of stock
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE status NOT IN ('Discontinued', 'OutOfStock') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Only Active products (4 items)
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Contains("Laptop"));
        Assert.IsTrue(results.Contains("Mouse"));
        Assert.IsTrue(results.Contains("Chair"));
        Assert.IsTrue(results.Contains("Monitor"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_NotIn_ExcludesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE status NOT IN ('Discontinued', 'OutOfStock') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Contains("Laptop"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_NotIn_ExcludesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE status NOT IN ('Discontinued', 'OutOfStock') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Contains("Laptop"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_NotIn_ExcludesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE status NOT IN ('Discontinued', 'OutOfStock') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Contains("Laptop"));
    }

    // ==================== IN with Numbers Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_InWithNumbers_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get products with specific IDs
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE id IN (1, 3, 5) ORDER BY id";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - 3 products
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Laptop", results[0]);
        Assert.AreEqual("Desk", results[1]);
        Assert.AreEqual("Monitor", results[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_InWithNumbers_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE id IN (1, 3, 5) ORDER BY id";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Laptop", results[0]);
        Assert.AreEqual("Desk", results[1]);
        Assert.AreEqual("Monitor", results[2]);
    }

    // ==================== Empty IN List Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_InWithSingleValue_WorksCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act - IN with single value
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_products WHERE category IN ('Electronics') ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - 4 Electronics products
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Contains("Laptop"));
        Assert.IsTrue(results.Contains("Mouse"));
        Assert.IsTrue(results.Contains("Monitor"));
        Assert.IsTrue(results.Contains("Keyboard"));
    }

    // ==================== IN with AND/OR Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("InOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_InWithAnd_CombinesConditions()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act - IN combined with AND
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name FROM test_products 
            WHERE category IN ('Electronics', 'Furniture') 
            AND status = 'Active'
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Only Active products in these categories
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.Contains("Chair"));
        Assert.IsTrue(results.Contains("Laptop"));
        Assert.IsTrue(results.Contains("Monitor"));
        Assert.IsTrue(results.Contains("Mouse"));
    }
}
