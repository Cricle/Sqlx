// <copyright file="UnionOperationsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.UnionOperations;

/// <summary>
/// E2E tests for SQL UNION and UNION ALL operations.
/// </summary>
[TestClass]
public class UnionOperationsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        var customersTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_customers (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    type VARCHAR(20) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_customers (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    type VARCHAR(20) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_customers (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(100) NOT NULL,
                    type NVARCHAR(20) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_customers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    type TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        var suppliersTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_suppliers (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    country VARCHAR(50) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_suppliers (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    country VARCHAR(50) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_suppliers (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(100) NOT NULL,
                    country NVARCHAR(50) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_suppliers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    country TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        return customersTable + ";\n" + suppliersTable;
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        // Insert customers
        var customers = new[]
        {
            ("Alice Corp", "alice@customer.com", "Premium"),
            ("Bob Inc", "bob@customer.com", "Standard"),
            ("Charlie Ltd", "charlie@customer.com", "Premium"),
        };

        foreach (var (name, email, type) in customers)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_customers (name, email, type) VALUES (@name, @email, @type)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@email", email);
            AddParameter(cmd, "@type", type);
            await cmd.ExecuteNonQueryAsync();
        }

        // Insert suppliers (one with duplicate email)
        var suppliers = new[]
        {
            ("David Supplies", "david@supplier.com", "USA"),
            ("Eve Materials", "eve@supplier.com", "UK"),
            ("Alice Corp", "alice@customer.com", "USA"), // Duplicate email
        };

        foreach (var (name, email, country) in suppliers)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_suppliers (name, email, country) VALUES (@name, @email, @country)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@email", email);
            AddParameter(cmd, "@country", country);
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

    // ==================== UNION Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_Union_RemovesDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - UNION removes duplicates
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - 5 unique emails (alice@customer.com appears in both but counted once)
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.Contains("alice@customer.com"));
        Assert.IsTrue(results.Contains("bob@customer.com"));
        Assert.IsTrue(results.Contains("charlie@customer.com"));
        Assert.IsTrue(results.Contains("david@supplier.com"));
        Assert.IsTrue(results.Contains("eve@supplier.com"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Union_RemovesDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.Contains("alice@customer.com"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Union_RemovesDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.Contains("alice@customer.com"));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_Union_RemovesDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.Contains("alice@customer.com"));
    }

    // ==================== UNION ALL Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("MySQL")]
    public async Task MySQL_UnionAll_KeepsDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - UNION ALL keeps duplicates
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION ALL
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - 6 total emails (alice@customer.com appears twice)
        Assert.AreEqual(6, results.Count);
        var aliceCount = results.Count(e => e == "alice@customer.com");
        Assert.AreEqual(2, aliceCount);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_UnionAll_KeepsDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION ALL
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        var aliceCount = results.Count(e => e == "alice@customer.com");
        Assert.AreEqual(2, aliceCount);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_UnionAll_KeepsDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION ALL
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        var aliceCount = results.Count(e => e == "alice@customer.com");
        Assert.AreEqual(2, aliceCount);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_UnionAll_KeepsDuplicates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT email FROM test_customers
            UNION ALL
            SELECT email FROM test_suppliers
            ORDER BY email";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        var aliceCount = results.Count(e => e == "alice@customer.com");
        Assert.AreEqual(2, aliceCount);
    }

    // ==================== UNION with Multiple Columns Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_UnionMultipleColumns_CombinesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act - UNION with multiple columns
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, email, 'Customer' as source FROM test_customers
            UNION
            SELECT name, email, 'Supplier' as source FROM test_suppliers
            ORDER BY name";

        var results = new List<(string Name, string Email, string Source)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        // Assert - 5 unique combinations (Alice Corp appears in both but with same email, so deduplicated)
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.Any(r => r.Name == "Alice Corp"));
        Assert.IsTrue(results.Any(r => r.Name == "David Supplies" && r.Source == "Supplier"));
    }

    // ==================== UNION with WHERE Clause Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("UnionOperations")]
    [TestCategory("SQLite")]
    public async Task SQLite_UnionWithWhere_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act - UNION with WHERE clauses
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name FROM test_customers WHERE type = 'Premium'
            UNION
            SELECT name FROM test_suppliers WHERE country = 'USA'
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Premium customers (Alice Corp, Charlie Ltd) + USA suppliers (David Supplies, Alice Corp)
        // After deduplication: Alice Corp, Charlie Ltd, David Supplies
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.Contains("Alice Corp"));
        Assert.IsTrue(results.Contains("Charlie Ltd"));
        Assert.IsTrue(results.Contains("David Supplies"));
    }
}
