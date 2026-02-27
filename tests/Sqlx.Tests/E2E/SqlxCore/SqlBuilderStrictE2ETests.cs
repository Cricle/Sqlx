// <copyright file="SqlBuilderStrictE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

/// <summary>
/// 严格测试SqlBuilder在所有数据库上的功能。
/// Strict E2E tests for SqlBuilder functionality across all databases.
/// </summary>
[TestClass]
public class SqlBuilderStrictE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_items (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_items (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_items (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    value INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static SqlDialect GetDialect(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => SqlDefine.MySql,
            DatabaseType.PostgreSQL => SqlDefine.PostgreSql,
            DatabaseType.SqlServer => SqlDefine.SqlServer,
            DatabaseType.SQLite => SqlDefine.SQLite,
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_Append_ParameterizesValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var name = "TestItem";
        var value = 100;

        // Act - 测试Append方法参数化
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT * FROM test_items WHERE name = {name} AND value = {value}");
        var template = builder.Build();

        // Assert
        Assert.IsNotNull(template);
        Assert.IsNotNull(template.Sql);
        Assert.IsTrue(template.Sql.Contains("@") || template.Sql.Contains("?"), "Should contain parameter placeholders");
        Assert.AreEqual(2, template.Parameters.Count, "Should have 2 parameters");
        Assert.IsTrue(template.Parameters.ContainsValue(name));
        Assert.IsTrue(template.Parameters.ContainsValue(value));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_AppendRaw_NoParameterization()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Act - 测试AppendRaw不参数化
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.AppendRaw("SELECT * FROM test_items WHERE value > 50");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count, "AppendRaw should not create parameters");
        Assert.AreEqual("SELECT * FROM test_items WHERE value > 50", template.Sql);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_MultipleAppends_ConcatenatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var name = "Test";
        var value = 100;

        // Act - 测试多次Append
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT * FROM test_items");
        builder.Append($" WHERE name = {name}");
        builder.Append($" AND value >= {value}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT"));
        Assert.IsTrue(template.Sql.Contains("WHERE"));
        Assert.IsTrue(template.Sql.Contains("AND"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_MixedAppendTypes_WorksCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var value = 50;

        // Act - 测试混合使用Append和AppendRaw
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.AppendRaw("SELECT * FROM test_items");
        builder.Append($" WHERE value >= {value}");
        builder.AppendRaw(" ORDER BY name");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT"));
        Assert.IsTrue(template.Sql.Contains("WHERE"));
        Assert.IsTrue(template.Sql.Contains("ORDER BY"));
        Assert.AreEqual(1, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_UniqueParameterNames_Generated()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var value1 = 10;
        var value2 = 20;
        var value3 = 30;

        // Act - 测试生成唯一参数名
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT * FROM test_items WHERE value = {value1} OR value = {value2} OR value = {value3}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(3, template.Parameters.Count);
        var paramNames = template.Parameters.Keys.ToList();
        Assert.AreEqual(3, paramNames.Distinct().Count(), "All parameter names should be unique");
    }

    // ==================== PostgreSQL SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_Append_ParameterizesValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var name = "TestItem";
        var value = 100;

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT * FROM test_items WHERE name = {name} AND value = {value}");
        var template = builder.Build();

        // Assert
        Assert.IsNotNull(template);
        Assert.IsTrue(template.Sql.Contains("$") || template.Sql.Contains("@"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_AppendRaw_NoParameterization()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.AppendRaw("SELECT * FROM test_items WHERE value > 50");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_MultipleAppends_ConcatenatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var name = "Test";
        var value = 100;

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT * FROM test_items");
        builder.Append($" WHERE name = {name}");
        builder.Append($" AND value >= {value}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT"));
        Assert.IsTrue(template.Sql.Contains("WHERE"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_UsesCorrectParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT * FROM test_items WHERE value = {100}");
        var template = builder.Build();

        // Assert - PostgreSQL使用$或@
        Assert.IsTrue(template.Sql.Contains("$") || template.Sql.Contains("@"));
    }

    // ==================== SQL Server SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_Append_ParameterizesValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var name = "TestItem";
        var value = 100;

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"SELECT * FROM test_items WHERE name = {name} AND value = {value}");
        var template = builder.Build();

        // Assert
        Assert.IsNotNull(template);
        Assert.IsTrue(template.Sql.Contains("@"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_AppendRaw_NoParameterization()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.AppendRaw("SELECT * FROM test_items WHERE value > 50");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_MultipleAppends_ConcatenatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var name = "Test";
        var value = 100;

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"SELECT * FROM test_items");
        builder.Append($" WHERE name = {name}");
        builder.Append($" AND value >= {value}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT"));
        Assert.IsTrue(template.Sql.Contains("WHERE"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_UsesAtSignParameterPrefix()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"SELECT * FROM test_items WHERE value = {100}");
        var template = builder.Build();

        // Assert - SQL Server使用@
        Assert.IsTrue(template.Sql.Contains("@"));
    }

    // ==================== SQLite SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_Append_ParameterizesValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var name = "TestItem";
        var value = 100;

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"SELECT * FROM test_items WHERE name = {name} AND value = {value}");
        var template = builder.Build();

        // Assert
        Assert.IsNotNull(template);
        Assert.IsTrue(template.Sql.Contains("?") || template.Sql.Contains("@") || template.Sql.Contains("$"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_AppendRaw_NoParameterization()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.AppendRaw("SELECT * FROM test_items WHERE value > 50");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_MultipleAppends_ConcatenatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var name = "Test";
        var value = 100;

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"SELECT * FROM test_items");
        builder.Append($" WHERE name = {name}");
        builder.Append($" AND value >= {value}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT"));
        Assert.IsTrue(template.Sql.Contains("WHERE"));
        Assert.AreEqual(2, template.Parameters.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_Dispose_ReleasesResources()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // Act
        Sqlx.SqlTemplate template;
        using (var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite)))
        {
            builder.Append($"SELECT * FROM test_items WHERE value = {100}");
            template = builder.Build();
        }

        // Assert - 应该在dispose后仍然可以使用template
        Assert.IsNotNull(template);
        Assert.IsNotNull(template.Sql);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_NullValue_HandlesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        string? nullValue = null;

        // Act - 测试NULL值参数化
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"SELECT * FROM test_items WHERE name = {nullValue}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.ContainsValue(null));
    }
}
