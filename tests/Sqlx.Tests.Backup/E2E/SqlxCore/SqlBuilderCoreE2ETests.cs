// <copyright file="SqlBuilderCoreE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// E2E tests for SqlBuilder core functionality.
/// </summary>
[TestClass]
public class SqlBuilderCoreE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_products (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    category VARCHAR(50) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    category VARCHAR(50) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_products (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    price DECIMAL(10, 2) NOT NULL,
                    category NVARCHAR(50) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    price REAL NOT NULL,
                    category TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var products = new[]
        {
            ("Product A", 100m, "Electronics"),
            ("Product B", 200m, "Electronics"),
            ("Product C", 150m, "Books"),
        };

        foreach (var (name, price, category) in products)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_products (name, price, category) VALUES (@name, @price, @category)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@price", price);
            AddParameter(cmd, "@category", category);
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

    // ==================== SqlBuilder Basic Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_InterpolatedString_ParameterizesValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        var minPrice = 150m;

        // Act - Use SqlBuilder with interpolated string
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_products WHERE price >= {minPrice}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("@") || template.Sql.Contains("?"), "Should contain parameter placeholder");
        Assert.AreEqual(1, template.Parameters.Count, "Should have one parameter");
        Assert.IsTrue(template.Parameters.ContainsValue(minPrice), "Parameter should contain the price value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_MultipleParameters_AllParameterized()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        var minPrice = 100m;
        var category = "Electronics";

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"SELECT * FROM test_products WHERE price >= {minPrice} AND category = {category}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(2, template.Parameters.Count, "Should have two parameters");
        Assert.IsTrue(template.Parameters.ContainsValue(minPrice));
        Assert.IsTrue(template.Parameters.ContainsValue(category));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_AppendRaw_NoParameterization()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SqlServer);
        builder.AppendRaw("SELECT * FROM test_products");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count, "AppendRaw should not create parameters");
        Assert.AreEqual("SELECT * FROM test_products", template.Sql);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_Build_CreatesTemplate()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var productName = "Test Product";

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM test_products WHERE name = {productName}");
        var template = builder.Build();

        // Assert
        Assert.IsNotNull(template);
        Assert.IsNotNull(template.Sql);
        Assert.IsNotNull(template.Parameters);
        Assert.IsTrue(template.Sql.Length > 0);
    }

    // ==================== SqlBuilder Chaining Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_MultipleAppends_ConcatenatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var category = "Electronics";
        var minPrice = 100m;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_products");
        builder.Append($" WHERE category = {category}");
        builder.Append($" AND price >= {minPrice}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT"));
        Assert.IsTrue(template.Sql.Contains("WHERE"));
        Assert.IsTrue(template.Sql.Contains("AND"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_MixedAppendTypes_WorksCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var price = 150m;

        // Act - Mix AppendRaw and Append
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.AppendRaw("SELECT * FROM test_products");
        builder.Append($" WHERE price >= {price}");
        builder.AppendRaw(" ORDER BY name");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT"));
        Assert.IsTrue(template.Sql.Contains("WHERE"));
        Assert.IsTrue(template.Sql.Contains("ORDER BY"));
        Assert.AreEqual(1, template.Parameters.Count);
    }

    // ==================== SqlBuilder Disposal Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_Dispose_ReleasesResources()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);

        // Act
        Sqlx.SqlTemplate template;
        using (var builder = new Sqlx.SqlBuilder(SqlDefine.MySql))
        {
            builder.Append($"SELECT * FROM test_products WHERE id = {1}");
            template = builder.Build();
        }

        // Assert - Should not throw after disposal
        Assert.IsNotNull(template);
        Assert.IsNotNull(template.Sql);
    }

    // ==================== SqlBuilder Parameter Naming Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_GeneratesUniqueParameterNames()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);

        var value1 = 100;
        var value2 = 200;
        var value3 = 300;

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"SELECT * FROM test_products WHERE id = {value1} OR id = {value2} OR id = {value3}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(3, template.Parameters.Count, "Should have 3 unique parameters");
        var paramNames = template.Parameters.Keys.ToList();
        Assert.AreEqual(3, paramNames.Distinct().Count(), "All parameter names should be unique");
    }

    // ==================== SqlBuilder Dialect-Specific Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.MySql);
        builder.Append($"SELECT * FROM test_products WHERE id = {1}");
        var template = builder.Build();

        // Assert - MySQL uses ? or @
        Assert.IsTrue(template.Sql.Contains("?") || template.Sql.Contains("@"), 
            "MySQL should use ? or @ for parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.PostgreSql);
        builder.Append($"SELECT * FROM test_products WHERE id = {1}");
        var template = builder.Build();

        // Assert - PostgreSQL uses $1, $2, etc. or @
        Assert.IsTrue(template.Sql.Contains("$") || template.Sql.Contains("@"), 
            "PostgreSQL should use $ or @ for parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SqlServer);
        builder.Append($"SELECT * FROM test_products WHERE id = {1}");
        var template = builder.Build();

        // Assert - SQL Server uses @
        Assert.IsTrue(template.Sql.Contains("@"), "SQL Server should use @ for parameters");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxCore")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);

        // Act
        using var builder = new Sqlx.SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM test_products WHERE id = {1}");
        var template = builder.Build();

        // Assert - SQLite uses ? or @
        Assert.IsTrue(template.Sql.Contains("?") || template.Sql.Contains("@") || template.Sql.Contains("$"), 
            "SQLite should use ?, @, or $ for parameters");
    }
}
