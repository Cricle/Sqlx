// <copyright file="AggregationE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Aggregation;

/// <summary>
/// E2E tests for SQL aggregation functions (COUNT, SUM, AVG, MIN, MAX, GROUP BY).
/// </summary>
[TestClass]
public class AggregationE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_sales (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    product VARCHAR(100) NOT NULL,
                    category VARCHAR(50) NOT NULL,
                    quantity INT NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    sale_date DATE NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_sales (
                    id BIGSERIAL PRIMARY KEY,
                    product VARCHAR(100) NOT NULL,
                    category VARCHAR(50) NOT NULL,
                    quantity INT NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    sale_date DATE NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_sales (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    product NVARCHAR(100) NOT NULL,
                    category NVARCHAR(50) NOT NULL,
                    quantity INT NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    sale_date DATE NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_sales (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    product TEXT NOT NULL,
                    category TEXT NOT NULL,
                    quantity INTEGER NOT NULL,
                    price REAL NOT NULL,
                    sale_date TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        var testData = new[]
        {
            ("Laptop", "Electronics", 5, 999.99m, "2024-01-15"),
            ("Mouse", "Electronics", 20, 29.99m, "2024-01-16"),
            ("Keyboard", "Electronics", 15, 79.99m, "2024-01-17"),
            ("Desk", "Furniture", 3, 299.99m, "2024-01-18"),
            ("Chair", "Furniture", 8, 199.99m, "2024-01-19"),
            ("Monitor", "Electronics", 10, 399.99m, "2024-01-20"),
            ("Table", "Furniture", 2, 499.99m, "2024-01-21"),
        };

        foreach (var (product, category, quantity, price, saleDate) in testData)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO test_sales (product, category, quantity, price, sale_date)
                VALUES (@product, @category, @quantity, @price, @sale_date)";

            AddParameter(cmd, "@product", product);
            AddParameter(cmd, "@category", category);
            AddParameter(cmd, "@quantity", quantity);
            AddParameter(cmd, "@price", price);
            AddParameter(cmd, "@sale_date", saleDate);

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

    // ==================== COUNT Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("MySQL")]
    public async Task MySQL_Count_ReturnsCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_sales";
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(7, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Count_ReturnsCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_sales";
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(7, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Count_ReturnsCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_sales";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(7, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SQLite")]
    public async Task SQLite_Count_ReturnsCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_sales";
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(7, count);
    }

    // ==================== SUM Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sum_CalculatesCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUM(quantity) FROM test_sales";
        var totalQuantity = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(63, totalQuantity); // 5+20+15+3+8+10+2
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sum_CalculatesCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUM(quantity) FROM test_sales";
        var totalQuantity = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(63, totalQuantity);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sum_CalculatesCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUM(quantity) FROM test_sales";
        var totalQuantity = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(63, totalQuantity);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sum_CalculatesCorrectTotal()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT SUM(quantity) FROM test_sales";
        var totalQuantity = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(63, totalQuantity);
    }

    // ==================== AVG Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("MySQL")]
    public async Task MySQL_Avg_CalculatesCorrectAverage()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT AVG(quantity) FROM test_sales";
        var avgQuantity = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(9.0, avgQuantity, 0.1); // 63/7 = 9
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Avg_CalculatesCorrectAverage()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT AVG(quantity) FROM test_sales";
        var avgQuantity = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(9.0, avgQuantity, 0.1);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Avg_CalculatesCorrectAverage()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT AVG(CAST(quantity AS FLOAT)) FROM test_sales";
        var avgQuantity = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(9.0, avgQuantity, 0.1);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SQLite")]
    public async Task SQLite_Avg_CalculatesCorrectAverage()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT AVG(quantity) FROM test_sales";
        var avgQuantity = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(9.0, avgQuantity, 0.1);
    }

    // ==================== MIN/MAX Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("MySQL")]
    public async Task MySQL_MinMax_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(price), MAX(price) FROM test_sales";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var minPrice = reader.GetDecimal(0);
        var maxPrice = reader.GetDecimal(1);

        // Assert
        Assert.AreEqual(29.99m, minPrice);
        Assert.AreEqual(999.99m, maxPrice);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_MinMax_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(price), MAX(price) FROM test_sales";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var minPrice = reader.GetDecimal(0);
        var maxPrice = reader.GetDecimal(1);

        // Assert
        Assert.AreEqual(29.99m, minPrice);
        Assert.AreEqual(999.99m, maxPrice);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_MinMax_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(price), MAX(price) FROM test_sales";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var minPrice = reader.GetDecimal(0);
        var maxPrice = reader.GetDecimal(1);

        // Assert
        Assert.AreEqual(29.99m, minPrice);
        Assert.AreEqual(999.99m, maxPrice);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SQLite")]
    public async Task SQLite_MinMax_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(price), MAX(price) FROM test_sales";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var minPrice = Convert.ToDecimal(reader.GetDouble(0));
        var maxPrice = Convert.ToDecimal(reader.GetDouble(1));

        // Assert
        Assert.AreEqual(29.99m, minPrice, 0.01m);
        Assert.AreEqual(999.99m, maxPrice, 0.01m);
    }

    // ==================== GROUP BY Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("MySQL")]
    public async Task MySQL_GroupBy_AggregatesByCategory()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT category, COUNT(*), SUM(quantity)
            FROM test_sales
            GROUP BY category
            ORDER BY category";

        var results = new List<(string Category, int Count, int TotalQty)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), Convert.ToInt32(reader.GetValue(1)), Convert.ToInt32(reader.GetValue(2))));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0].Category);
        Assert.AreEqual(4, results[0].Count);
        Assert.AreEqual(50, results[0].TotalQty); // 5+20+15+10
        
        Assert.AreEqual("Furniture", results[1].Category);
        Assert.AreEqual(3, results[1].Count);
        Assert.AreEqual(13, results[1].TotalQty); // 3+8+2
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_GroupBy_AggregatesByCategory()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT category, COUNT(*), SUM(quantity)
            FROM test_sales
            GROUP BY category
            ORDER BY category";

        var results = new List<(string Category, long Count, long TotalQty)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0].Category);
        Assert.AreEqual(4, results[0].Count);
        Assert.AreEqual(50, results[0].TotalQty);
        
        Assert.AreEqual("Furniture", results[1].Category);
        Assert.AreEqual(3, results[1].Count);
        Assert.AreEqual(13, results[1].TotalQty);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_GroupBy_AggregatesByCategory()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT category, COUNT(*), SUM(quantity)
            FROM test_sales
            GROUP BY category
            ORDER BY category";

        var results = new List<(string Category, int Count, int TotalQty)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0].Category);
        Assert.AreEqual(4, results[0].Count);
        Assert.AreEqual(50, results[0].TotalQty);
        
        Assert.AreEqual("Furniture", results[1].Category);
        Assert.AreEqual(3, results[1].Count);
        Assert.AreEqual(13, results[1].TotalQty);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SQLite")]
    public async Task SQLite_GroupBy_AggregatesByCategory()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT category, COUNT(*), SUM(quantity)
            FROM test_sales
            GROUP BY category
            ORDER BY category";

        var results = new List<(string Category, long Count, long TotalQty)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Electronics", results[0].Category);
        Assert.AreEqual(4, results[0].Count);
        Assert.AreEqual(50, results[0].TotalQty);
        
        Assert.AreEqual("Furniture", results[1].Category);
        Assert.AreEqual(3, results[1].Count);
        Assert.AreEqual(13, results[1].TotalQty);
    }

    // ==================== HAVING Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Aggregation")]
    [TestCategory("SQLite")]
    public async Task SQLite_Having_FiltersAggregatedResults()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act - Get categories with more than 3 items
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT category, COUNT(*) as item_count
            FROM test_sales
            GROUP BY category
            HAVING COUNT(*) > 3
            ORDER BY category";

        var results = new List<(string Category, long Count)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert - Only Electronics has more than 3 items
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Electronics", results[0].Category);
        Assert.AreEqual(4, results[0].Count);
    }
}
