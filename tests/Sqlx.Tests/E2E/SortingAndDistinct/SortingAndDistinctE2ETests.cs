// <copyright file="SortingAndDistinctE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.SortingAndDistinct;

/// <summary>
/// E2E tests for ORDER BY sorting and DISTINCT operations.
/// </summary>
[TestClass]
public class SortingAndDistinctE2ETests : E2ETestBase
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
                    stock INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    category VARCHAR(50) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    stock INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_products (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    category NVARCHAR(50) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    stock INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    category TEXT NOT NULL,
                    price REAL NOT NULL,
                    stock INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        var products = new[]
        {
            ("Laptop", "Electronics", 999.99m, 10),
            ("Mouse", "Electronics", 29.99m, 50),
            ("Keyboard", "Electronics", 79.99m, 30),
            ("Desk", "Furniture", 299.99m, 5),
            ("Chair", "Furniture", 199.99m, 15),
            ("Monitor", "Electronics", 399.99m, 20),
            ("Table", "Furniture", 499.99m, 3),
            ("Headphones", "Electronics", 149.99m, 25),
        };

        foreach (var (name, category, price, stock) in products)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_products (name, category, price, stock) VALUES (@name, @category, @price, @stock)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@category", category);
            AddParameter(cmd, "@price", price);
            AddParameter(cmd, "@stock", stock);
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

    // ==================== ORDER BY ASC Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("MySQL")]
    public async Task MySQL_OrderByAsc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Sort by price ascending
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, price FROM test_products ORDER BY price ASC LIMIT 3";

        var results = new List<(string Name, decimal Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert - Cheapest 3 items
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Mouse", results[0].Name);
        Assert.AreEqual(29.99m, results[0].Price);
        Assert.AreEqual("Keyboard", results[1].Name);
        Assert.AreEqual(79.99m, results[1].Price);
        Assert.AreEqual("Headphones", results[2].Name);
        Assert.AreEqual(149.99m, results[2].Price);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_OrderByAsc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, price FROM test_products ORDER BY price ASC LIMIT 3";

        var results = new List<(string Name, decimal Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Mouse", results[0].Name);
        Assert.AreEqual("Keyboard", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OrderByAsc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT TOP 3 name, price FROM test_products ORDER BY price ASC";

        var results = new List<(string Name, decimal Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Mouse", results[0].Name);
        Assert.AreEqual("Keyboard", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("SQLite")]
    public async Task SQLite_OrderByAsc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, price FROM test_products ORDER BY price ASC LIMIT 3";

        var results = new List<(string Name, double Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Mouse", results[0].Name);
        Assert.AreEqual("Keyboard", results[1].Name);
    }

    // ==================== ORDER BY DESC Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("MySQL")]
    public async Task MySQL_OrderByDesc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Sort by price descending
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, price FROM test_products ORDER BY price DESC LIMIT 3";

        var results = new List<(string Name, decimal Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert - Most expensive 3 items
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Laptop", results[0].Name);
        Assert.AreEqual(999.99m, results[0].Price);
        Assert.AreEqual("Table", results[1].Name);
        Assert.AreEqual(499.99m, results[1].Price);
        Assert.AreEqual("Monitor", results[2].Name);
        Assert.AreEqual(399.99m, results[2].Price);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_OrderByDesc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, price FROM test_products ORDER BY price DESC LIMIT 3";

        var results = new List<(string Name, decimal Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Laptop", results[0].Name);
        Assert.AreEqual("Table", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OrderByDesc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT TOP 3 name, price FROM test_products ORDER BY price DESC";

        var results = new List<(string Name, decimal Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Laptop", results[0].Name);
        Assert.AreEqual("Table", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("SQLite")]
    public async Task SQLite_OrderByDesc_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, price FROM test_products ORDER BY price DESC LIMIT 3";

        var results = new List<(string Name, double Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Laptop", results[0].Name);
        Assert.AreEqual("Table", results[1].Name);
    }

    // ==================== Multiple Column Sorting Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("MySQL")]
    public async Task MySQL_OrderByMultipleColumns_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Sort by category ASC, then price DESC
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, category, price FROM test_products ORDER BY category ASC, price DESC";

        var results = new List<(string Name, string Category, decimal Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
        }

        // Assert - Electronics first (sorted by price DESC), then Furniture (sorted by price DESC)
        Assert.AreEqual(8, results.Count);
        Assert.AreEqual("Electronics", results[0].Category);
        Assert.AreEqual("Laptop", results[0].Name); // Most expensive Electronics
        Assert.AreEqual("Furniture", results[5].Category);
        Assert.AreEqual("Table", results[5].Name); // Most expensive Furniture
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Sorting")]
    [TestCategory("SQLite")]
    public async Task SQLite_OrderByMultipleColumns_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, category, price FROM test_products ORDER BY category ASC, price DESC";

        var results = new List<(string Name, string Category, double Price)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetDouble(2)));
        }

        // Assert
        Assert.AreEqual(8, results.Count);
        Assert.AreEqual("Electronics", results[0].Category);
        Assert.AreEqual("Laptop", results[0].Name);
    }

    // ==================== DISTINCT Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("MySQL")]
    public async Task MySQL_Distinct_ReturnsUniqueValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get distinct categories
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT category FROM test_products ORDER BY category";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Only 2 unique categories
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0]);
        Assert.AreEqual("Furniture", results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Distinct_ReturnsUniqueValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT category FROM test_products ORDER BY category";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0]);
        Assert.AreEqual("Furniture", results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Distinct_ReturnsUniqueValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT category FROM test_products ORDER BY category";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0]);
        Assert.AreEqual("Furniture", results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SQLite")]
    public async Task SQLite_Distinct_ReturnsUniqueValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT category FROM test_products ORDER BY category";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0]);
        Assert.AreEqual("Furniture", results[1]);
    }

    // ==================== DISTINCT with COUNT Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Distinct")]
    [TestCategory("SQLite")]
    public async Task SQLite_DistinctCount_CountsUniqueValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act - Count distinct categories
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(DISTINCT category) FROM test_products";
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(2, count);
    }
}
