// <copyright file="CoalesceFunctionsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.CoalesceFunctions;

/// <summary>
/// E2E tests for COALESCE and null-related functions.
/// </summary>
[TestClass]
public class CoalesceFunctionsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_products (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    description VARCHAR(200) NULL,
                    price DECIMAL(10, 2) NULL,
                    discount DECIMAL(10, 2) NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    description VARCHAR(200) NULL,
                    price DECIMAL(10, 2) NULL,
                    discount DECIMAL(10, 2) NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_products (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    description NVARCHAR(200) NULL,
                    price DECIMAL(10, 2) NULL,
                    discount DECIMAL(10, 2) NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    price REAL NULL,
                    discount REAL NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var products = new[]
        {
            ("Product A", "Description A", (decimal?)100m, (decimal?)10m),
            ("Product B", (string?)null, (decimal?)200m, (decimal?)null),
            ("Product C", "Description C", (decimal?)null, (decimal?)20m),
        };

        foreach (var (name, desc, price, discount) in products)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_products (name, description, price, discount) VALUES (@name, @desc, @price, @discount)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@desc", desc);
            AddParameter(cmd, "@price", price);
            AddParameter(cmd, "@discount", discount);
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

    // ==================== COALESCE Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   COALESCE(description, 'No description') as desc_value
            FROM test_products
            ORDER BY name";

        var results = new List<(string Name, string Desc)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Description A", results[0].Desc);
        Assert.AreEqual("No description", results[1].Desc); // Product B has null
        Assert.AreEqual("Description C", results[2].Desc);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   COALESCE(description, 'No description') as desc_value
            FROM test_products
            ORDER BY name";

        var results = new List<(string Name, string Desc)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("No description", results[1].Desc);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   COALESCE(description, 'No description') as desc_value
            FROM test_products
            ORDER BY name";

        var results = new List<(string Name, string Desc)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("No description", results[1].Desc);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   COALESCE(description, 'No description') as desc_value
            FROM test_products
            ORDER BY name";

        var results = new List<(string Name, string Desc)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("No description", results[1].Desc);
    }

    // ==================== COALESCE with Multiple Values Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_CoalesceMultiple_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   COALESCE(price, discount, 0) as final_value
            FROM test_products
            ORDER BY name";

        var results = new List<(string Name, decimal Value)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(100m, results[0].Value); // Product A has price
        Assert.AreEqual(200m, results[1].Value); // Product B has price
        Assert.AreEqual(20m, results[2].Value);  // Product C has no price, uses discount
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_CoalesceMultiple_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   COALESCE(price, discount, 0) as final_value
            FROM test_products
            ORDER BY name";

        var results = new List<(string Name, double Value)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(100.0, results[0].Value, 0.01);
        Assert.AreEqual(200.0, results[1].Value, 0.01);
        Assert.AreEqual(20.0, results[2].Value, 0.01);
    }

    // ==================== NULLIF Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_NullIf_ReturnsNullWhenEqual()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   NULLIF(discount, 10) as adjusted_discount
            FROM test_products
            WHERE discount IS NOT NULL
            ORDER BY name";

        var results = new List<(string Name, decimal? Discount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var discount = reader.IsDBNull(1) ? (decimal?)null : reader.GetDecimal(1);
            results.Add((reader.GetString(0), discount));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsNull(results[0].Discount); // Product A has discount 10, NULLIF returns null
        Assert.AreEqual(20m, results[1].Discount); // Product C has discount 20
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CoalesceFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_NullIf_ReturnsNullWhenEqual()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   NULLIF(discount, 10) as adjusted_discount
            FROM test_products
            WHERE discount IS NOT NULL
            ORDER BY name";

        var results = new List<(string Name, double? Discount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var discount = reader.IsDBNull(1) ? (double?)null : reader.GetDouble(1);
            results.Add((reader.GetString(0), discount));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsNull(results[0].Discount);
        Assert.IsNotNull(results[1].Discount);
        if (results[1].Discount.HasValue)
        {
            Assert.AreEqual(20.0, results[1].Discount!.Value, 0.01);
        }
    }
}
