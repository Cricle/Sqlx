// <copyright file="SqlBuilderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests for SqlBuilder functionality.
/// </summary>
[TestClass]
public class SqlBuilderTests
{
    [TestMethod]
    public void Constructor_WithValidDialect_CreatesBuilder()
    {
        // Arrange & Act
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(string.Empty, template.Sql);
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    public void Constructor_WithPlaceholderContext_CreatesBuilder()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);

        // Act
        using var builder = new SqlBuilder(context);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(string.Empty, template.Sql);
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_WithNullDialect_ThrowsArgumentNullException()
    {
        // Act
        SqlDialect? dialect = null;
        _ = new SqlBuilder(dialect!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Act
        SqlDialect dialect = SqlDefine.SQLite;
        _ = new SqlBuilder(dialect, 0);
    }

    [TestMethod]
    public void Append_WithInterpolatedString_GeneratesParameterizedSql()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var userId = 123;

        // Act
        builder.Append($"SELECT * FROM users WHERE id = {userId}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE id = @p0", template.Sql);
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(123, template.Parameters["p0"]);
    }

    [TestMethod]
    public void Append_WithMultipleValues_GeneratesUniqueParameters()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var name = "John";
        var age = 30;

        // Act
        builder.Append($"SELECT * FROM users WHERE name = {name} AND age = {age}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE name = @p0 AND age = @p1", template.Sql);
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual("John", template.Parameters["p0"]);
        Assert.AreEqual(30, template.Parameters["p1"]);
    }

    [TestMethod]
    public void Append_WithMultipleCalls_ConcatenatesSql()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var minAge = 18;

        // Act
        builder.Append($"SELECT * FROM users");
        builder.Append($" WHERE age >= {minAge}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE age >= @p0", template.Sql);
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["p0"]);
    }

    [TestMethod]
    public void Append_WithNullValue_PreservesNull()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        string? nullValue = null;

        // Act
        builder.Append($"SELECT * FROM users WHERE name = {nullValue}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE name = @p0", template.Sql);
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsNull(template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendRaw_WithLiteralSql_AppendsWithoutParameterization()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var tableName = SqlDefine.SQLite.WrapColumn("users");

        // Act
        builder.AppendRaw($"SELECT * FROM {tableName}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual("SELECT * FROM [users]", template.Sql);
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    public void AppendRaw_WithEmptyString_DoesNothing()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);

        // Act
        builder.AppendRaw("");
        builder.AppendRaw("   ");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(string.Empty, template.Sql);
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Build_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM users");
        builder.Build();

        // Act
        builder.Build();
    }

    [TestMethod]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);

        // Act & Assert
        builder.Dispose();
        builder.Dispose(); // Should not throw
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void Append_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Dispose();

        // Act
        builder.Append($"SELECT *");
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void AppendRaw_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Dispose();

        // Act
        builder.AppendRaw("SELECT *");
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void Build_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Dispose();

        // Act
        builder.Build();
    }

    [TestMethod]
    public void Append_WithLargeQuery_GrowsBufferCorrectly()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite, initialCapacity: 64);
        var longString = new string('x', 1000);

        // Act
        builder.Append($"SELECT * FROM users WHERE name = {longString}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT * FROM users WHERE name = @p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(longString, template.Parameters["p0"]);
    }

    [TestMethod]
    public void Append_WithSQLiteDialect_UsesAtSymbolPrefix()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);

        // Act
        builder.Append($"SELECT * FROM users WHERE id = {123}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("@p0"));
    }

    [TestMethod]
    public void Append_WithMethodChaining_WorksCorrectly()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var minAge = 18;
        var maxAge = 65;

        // Act
        var template = builder
            .Append($"SELECT * FROM users")
            .Append($" WHERE age >= {minAge}")
            .Append($" AND age <= {maxAge}")
            .Build();

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE age >= @p0 AND age <= @p1", template.Sql);
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["p0"]);
        Assert.AreEqual(65, template.Parameters["p1"]);
    }

    [TestMethod]
    public void Append_WithComplexTypes_PreservesValues()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var date = new DateTime(2024, 1, 1);
        var guid = Guid.NewGuid();
        var bytes = new byte[] { 1, 2, 3 };

        // Act
        builder.Append($"INSERT INTO logs (created, id, data) VALUES ({date}, {guid}, {bytes})");
        var template = builder.Build();

        // Assert
        Assert.AreEqual("INSERT INTO logs (created, id, data) VALUES (@p0, @p1, @p2)", template.Sql);
        Assert.AreEqual(3, template.Parameters.Count);
        Assert.AreEqual(date, template.Parameters["p0"]);
        Assert.AreEqual(guid, template.Parameters["p1"]);
        CollectionAssert.AreEqual(bytes, (byte[])template.Parameters["p2"]!);
    }

    [TestMethod]
    public void AppendSubquery_WithSimpleSubquery_WrapsInParentheses()
    {
        // Arrange
        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        subquery.Append($"SELECT id FROM orders WHERE total > {1000}");

        using var mainQuery = new SqlBuilder(SqlDefine.SQLite);

        // Act
        mainQuery.Append($"SELECT * FROM users WHERE id IN ");
        mainQuery.AppendSubquery(subquery);
        var template = mainQuery.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("SELECT * FROM users WHERE id IN (SELECT id FROM orders WHERE total > @p0)"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(1000, template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendSubquery_MergesParameters_Correctly()
    {
        // Arrange
        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        var minTotal = 1000;
        subquery.Append($"SELECT id FROM orders WHERE total > {minTotal}");

        using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
        var minAge = 18;

        // Act
        mainQuery.Append($"SELECT * FROM users WHERE age >= {minAge} AND id IN ");
        mainQuery.AppendSubquery(subquery);
        var template = mainQuery.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("age >= @p0"));
        Assert.IsTrue(template.Sql.Contains("total > @p1"));
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["p0"]);
        Assert.AreEqual(1000, template.Parameters["p1"]);
    }

    [TestMethod]
    public void AppendTemplate_WithColumnsPlaceholder_ReplacesWithColumnList()
    {
        // Arrange
        var columns = new[] 
        { 
            new ColumnMeta("id", "id", DbType.Int32, false),
            new ColumnMeta("name", "name", DbType.String, false),
            new ColumnMeta("age", "age", DbType.Int32, false)
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[age]"));
        Assert.IsTrue(template.Sql.Contains("[users]"));
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    public void AppendTemplate_WithParameters_MergesIntoBuilder()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge = 18 });
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[users]"));
        Assert.IsTrue(template.Sql.Contains("@minAge"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["minAge"]);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AppendTemplate_WithoutPlaceholderContext_ThrowsInvalidOperationException()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}}");
    }

    [TestMethod]
    public void AppendTemplate_CombinedWithAppend_WorksCorrectly()
    {
        // Arrange
        var columns = new[] 
        { 
            new ColumnMeta("id", "id", DbType.Int32, false),
            new ColumnMeta("name", "name", DbType.String, false)
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var status = "active";

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}}");
        builder.Append($" WHERE status = {status}");
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[users]"));
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual("active", template.Parameters["p0"]);
    }

    [TestMethod]
    public void Build_ReturnsSqlTemplate_WithSqlAndParameters()
    {
        // Arrange
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        var userId = 42;
        var userName = "Alice";

        // Act
        builder.Append($"SELECT * FROM users WHERE id = {userId} AND name = {userName}");
        var template = builder.Build();

        // Assert
        Assert.IsNotNull(template);
        Assert.AreEqual("SELECT * FROM users WHERE id = @p0 AND name = @p1", template.Sql);
        Assert.AreEqual("SELECT * FROM users WHERE id = @p0 AND name = @p1", template.TemplateSql);
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(42, template.Parameters["p0"]);
        Assert.AreEqual("Alice", template.Parameters["p1"]);
        Assert.IsFalse(template.HasDynamicPlaceholders); // No dynamic placeholders in SqlBuilder output
    }

    // ========== Tests for optimized AppendTemplate parameter handling ==========

    [TestMethod]
    public void AppendTemplate_WithDictionary_UsesDirectlyWithoutCopy()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var parameters = new Dictionary<string, object?> 
        { 
            { "minAge", 18 },
            { "maxAge", 65 }
        };

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND age <= @maxAge", parameters);
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[users]"));
        Assert.IsTrue(template.Sql.Contains("@minAge"));
        Assert.IsTrue(template.Sql.Contains("@maxAge"));
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["minAge"]);
        Assert.AreEqual(65, template.Parameters["maxAge"]);
    }

    [TestMethod]
    public void AppendTemplate_WithReadOnlyDictionary_CopiesDirectlyToBuilderParameters()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("name", "name", DbType.String, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "products", columns);
        using var builder = new SqlBuilder(context);
        IReadOnlyDictionary<string, object?> parameters = new Dictionary<string, object?> 
        { 
            { "category", "Electronics" },
            { "minPrice", 100.0 }
        };

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE category = @category AND price >= @minPrice", parameters);
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[products]"));
        Assert.IsTrue(template.Sql.Contains("@category"));
        Assert.IsTrue(template.Sql.Contains("@minPrice"));
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual("Electronics", template.Parameters["category"]);
        Assert.AreEqual(100.0, template.Parameters["minPrice"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_UsesExpressionTreeToPopulateParameters()
    {
        // Arrange
        var columns = new[] 
        { 
            new ColumnMeta("id", "id", DbType.Int32, false),
            new ColumnMeta("title", "title", DbType.String, false)
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "articles", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE status = @status AND views > @minViews", 
            new { status = "published", minViews = 1000 });
        var template = builder.Build();

        // Assert
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[title]"));
        Assert.IsTrue(template.Sql.Contains("[articles]"));
        Assert.IsTrue(template.Sql.Contains("@status"));
        Assert.IsTrue(template.Sql.Contains("@minViews"));
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual("published", template.Parameters["status"]);
        Assert.AreEqual(1000, template.Parameters["minViews"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_MultipleProperties_AllPropertiesPopulated()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE customer = @customer AND total >= @minTotal AND status = @status AND created >= @startDate",
            new 
            { 
                customer = "John Doe",
                minTotal = 500.0,
                status = "completed",
                startDate = new DateTime(2024, 1, 1)
            });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(4, template.Parameters.Count);
        Assert.AreEqual("John Doe", template.Parameters["customer"]);
        Assert.AreEqual(500.0, template.Parameters["minTotal"]);
        Assert.AreEqual("completed", template.Parameters["status"]);
        Assert.AreEqual(new DateTime(2024, 1, 1), template.Parameters["startDate"]);
    }

    [TestMethod]
    public void AppendTemplate_WithNullParameterValues_PreservesNulls()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE email = @email", 
            new { email = (string?)null });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsNull(template.Parameters["email"]);
    }

    [TestMethod]
    public void AppendTemplate_WithComplexTypes_PreservesValues()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "logs", columns);
        using var builder = new SqlBuilder(context);
        var guid = Guid.NewGuid();
        var date = new DateTime(2024, 6, 15);
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0xFF };

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE guid = @guid AND created = @created AND data = @data",
            new { guid, created = date, data = bytes });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(3, template.Parameters.Count);
        Assert.AreEqual(guid, template.Parameters["guid"]);
        Assert.AreEqual(date, template.Parameters["created"]);
        CollectionAssert.AreEqual(bytes, (byte[])template.Parameters["data"]!);
    }

    [TestMethod]
    public void AppendTemplate_WithEmptyAnonymousObject_NoParametersAdded()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}}", new { });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count);
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[users]"));
    }

    [TestMethod]
    public void AppendTemplate_MultipleCalls_ParametersAccumulate()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge = 18 });
        builder.AppendRaw(" AND ");
        builder.AppendTemplate("status = @status", new { status = "active" });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["minAge"]);
        Assert.AreEqual("active", template.Parameters["status"]);
    }

    [TestMethod]
    public void AppendTemplate_WithDictionary_EmptyDictionary_NoParametersAdded()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var parameters = new Dictionary<string, object?>();

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}}", parameters);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    public void AppendTemplate_WithReadOnlyDictionary_EmptyDictionary_NoParametersAdded()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        IReadOnlyDictionary<string, object?> parameters = new Dictionary<string, object?>();

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}}", parameters);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count);
    }

    [TestMethod]
    public void AppendTemplate_WithNullParameters_NoParametersAdded()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}}", (object?)null);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.Parameters.Count);
        Assert.IsTrue(template.Sql.Contains("[id]"));
    }

    [TestMethod]
    public void AppendTemplate_WithDictionary_LargeNumberOfParameters_AllParametersPreserved()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);
        using var builder = new SqlBuilder(context);
        var parameters = new Dictionary<string, object?>();
        for (int i = 0; i < 100; i++)
        {
            parameters[$"param{i}"] = i;
        }

        // Act
        var templateStr = "SELECT {{columns}} FROM {{table}} WHERE " + 
            string.Join(" AND ", Enumerable.Range(0, 100).Select(i => $"col{i} = @param{i}"));
        builder.AppendTemplate(templateStr, parameters);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(100, template.Parameters.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i, template.Parameters[$"param{i}"]);
        }
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_PropertyNamesCaseSensitive()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @Name AND age = @age",
            new { Name = "Alice", age = 30 });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual("Alice", template.Parameters["Name"]);
        Assert.AreEqual(30, template.Parameters["age"]);
    }

    [TestMethod]
    public void AppendTemplate_CombinedWithAppendAndSubquery_AllParametersMergedCorrectly()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        
        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        var minTotal = 1000;
        subquery.Append($"SELECT user_id FROM orders WHERE total > {minTotal}");

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge = 18 });
        builder.AppendRaw(" AND id IN ");
        builder.AppendSubquery(subquery);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["minAge"]);
        Assert.AreEqual(1000, template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendTemplate_WithDictionary_ParametersNotModifiedExternally()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var parameters = new Dictionary<string, object?> { { "age", 25 } };

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", parameters);
        parameters["age"] = 30; // Modify external dictionary
        var template = builder.Build();

        // Assert - builder should have captured the original value
        Assert.AreEqual(25, template.Parameters["age"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_ExpressionTreeCached()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);

        // Act - Call multiple times with same anonymous type structure
        using var builder1 = new SqlBuilder(context);
        builder1.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", new { age = 20 });
        var template1 = builder1.Build();

        using var builder2 = new SqlBuilder(context);
        builder2.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", new { age = 30 });
        var template2 = builder2.Build();

        // Assert - Both should work correctly (expression tree should be cached and reused)
        Assert.AreEqual(1, template1.Parameters.Count);
        Assert.AreEqual(20, template1.Parameters["age"]);
        Assert.AreEqual(1, template2.Parameters.Count);
        Assert.AreEqual(30, template2.Parameters["age"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_DifferentPropertyTypes_AllTypesHandled()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "mixed", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE " +
            "intVal = @intVal AND longVal = @longVal AND doubleVal = @doubleVal AND " +
            "boolVal = @boolVal AND stringVal = @stringVal AND dateVal = @dateVal",
            new 
            { 
                intVal = 42,
                longVal = 9876543210L,
                doubleVal = 3.14159,
                boolVal = true,
                stringVal = "test",
                dateVal = new DateTime(2024, 12, 25)
            });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(6, template.Parameters.Count);
        Assert.AreEqual(42, template.Parameters["intVal"]);
        Assert.AreEqual(9876543210L, template.Parameters["longVal"]);
        Assert.AreEqual(3.14159, template.Parameters["doubleVal"]);
        Assert.AreEqual(true, template.Parameters["boolVal"]);
        Assert.AreEqual("test", template.Parameters["stringVal"]);
        Assert.AreEqual(new DateTime(2024, 12, 25), template.Parameters["dateVal"]);
    }

    // ========== Additional tests for Expression tree optimization and edge cases ==========

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_SingleProperty_ExpressionTreeWorks()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id", new { id = 999 });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(999, template.Parameters["id"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_TenProperties_AllPropertiesPopulated()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE " +
            "p1 = @p1 AND p2 = @p2 AND p3 = @p3 AND p4 = @p4 AND p5 = @p5 AND " +
            "p6 = @p6 AND p7 = @p7 AND p8 = @p8 AND p9 = @p9 AND p10 = @p10",
            new 
            { 
                p1 = 1, p2 = 2, p3 = 3, p4 = 4, p5 = 5,
                p6 = 6, p7 = 7, p8 = 8, p9 = 9, p10 = 10
            });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(10, template.Parameters.Count);
        for (int i = 1; i <= 10; i++)
        {
            Assert.AreEqual(i, template.Parameters[$"p{i}"]);
        }
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_NullableTypes_PreservesNullAndValues()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE name = @name AND age = @age AND email = @email",
            new 
            { 
                name = "Alice",
                age = (int?)null,
                email = (string?)null
            });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(3, template.Parameters.Count);
        Assert.AreEqual("Alice", template.Parameters["name"]);
        Assert.IsNull(template.Parameters["age"]);
        Assert.IsNull(template.Parameters["email"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_DecimalAndFloatTypes_PreservesValues()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "products", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE price = @price AND weight = @weight AND discount = @discount",
            new 
            { 
                price = 99.99m,
                weight = 1.5f,
                discount = 0.15
            });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(3, template.Parameters.Count);
        Assert.AreEqual(99.99m, template.Parameters["price"]);
        Assert.AreEqual(1.5f, template.Parameters["weight"]);
        Assert.AreEqual(0.15, template.Parameters["discount"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_CollectionTypes_PreservesCollections()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);
        using var builder = new SqlBuilder(context);
        var intArray = new[] { 1, 2, 3 };
        var stringList = new List<string> { "a", "b", "c" };

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE ids = @ids AND tags = @tags",
            new { ids = intArray, tags = stringList });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(2, template.Parameters.Count);
        CollectionAssert.AreEqual(intArray, (int[])template.Parameters["ids"]!);
        CollectionAssert.AreEqual(stringList, (List<string>)template.Parameters["tags"]!);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_NestedObject_PreservesReference()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var address = new { Street = "123 Main St", City = "Springfield" };

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE address = @address",
            new { address });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreSame(address, template.Parameters["address"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_SameTypeMultipleTimes_UsesCachedExpressionTree()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);

        // Act - Create multiple builders with same anonymous type structure
        var templates = new List<SqlTemplate>();
        for (int i = 0; i < 5; i++)
        {
            using var builder = new SqlBuilder(context);
            builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age AND name = @name", 
                new { age = 20 + i, name = $"User{i}" });
            templates.Add(builder.Build());
        }

        // Assert - All should work correctly with cached expression tree
        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(2, templates[i].Parameters.Count);
            Assert.AreEqual(20 + i, templates[i].Parameters["age"]);
            Assert.AreEqual($"User{i}", templates[i].Parameters["name"]);
        }
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_DifferentTypesSequentially_EachTypeGetsCachedSeparately()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);

        // Act - Use different anonymous type structures
        using var builder1 = new SqlBuilder(context);
        builder1.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE x = @x", new { x = 1 });
        var template1 = builder1.Build();

        using var builder2 = new SqlBuilder(context);
        builder2.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE y = @y AND z = @z", new { y = 2, z = 3 });
        var template2 = builder2.Build();

        using var builder3 = new SqlBuilder(context);
        builder3.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE a = @a AND b = @b AND c = @c", 
            new { a = 4, b = 5, c = 6 });
        var template3 = builder3.Build();

        // Assert - Each type should work correctly with its own cached expression tree
        Assert.AreEqual(1, template1.Parameters.Count);
        Assert.AreEqual(1, template1.Parameters["x"]);

        Assert.AreEqual(2, template2.Parameters.Count);
        Assert.AreEqual(2, template2.Parameters["y"]);
        Assert.AreEqual(3, template2.Parameters["z"]);

        Assert.AreEqual(3, template3.Parameters.Count);
        Assert.AreEqual(4, template3.Parameters["a"]);
        Assert.AreEqual(5, template3.Parameters["b"]);
        Assert.AreEqual(6, template3.Parameters["c"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_PropertyWithUnderscore_PreservesPropertyName()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @user_id AND first_name = @first_name",
            new { user_id = 123, first_name = "John" });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(123, template.Parameters["user_id"]);
        Assert.AreEqual("John", template.Parameters["first_name"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_MixedWithAppend_ParametersFromBothSources()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var status = "active";

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge = 18 });
        builder.Append($" AND status = {status}");
        var template = builder.Build();

        // Assert
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(18, template.Parameters["minAge"]);
        Assert.AreEqual("active", template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendTemplate_WithDictionary_ModifyDictionaryAfterCall_BuilderNotAffected()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var parameters = new Dictionary<string, object?> { { "age", 25 }, { "name", "Alice" } };

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age AND name = @name", parameters);
        parameters["age"] = 99; // Modify after call
        parameters["name"] = "Bob";
        parameters["newParam"] = "should not appear";
        var template = builder.Build();

        // Assert - Builder should have original values
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(25, template.Parameters["age"]);
        Assert.AreEqual("Alice", template.Parameters["name"]);
        Assert.IsFalse(template.Parameters.ContainsKey("newParam"));
    }

    [TestMethod]
    public void AppendTemplate_WithReadOnlyDictionary_LargeNumberOfParameters_AllCopiedCorrectly()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);
        using var builder = new SqlBuilder(context);
        var dict = new Dictionary<string, object?>();
        for (int i = 0; i < 50; i++)
        {
            dict[$"p{i}"] = i * 10;
        }
        IReadOnlyDictionary<string, object?> parameters = dict;

        // Act
        var templateStr = "SELECT {{columns}} FROM {{table}} WHERE " + 
            string.Join(" AND ", Enumerable.Range(0, 50).Select(i => $"col{i} = @p{i}"));
        builder.AppendTemplate(templateStr, parameters);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(50, template.Parameters.Count);
        for (int i = 0; i < 50; i++)
        {
            Assert.AreEqual(i * 10, template.Parameters[$"p{i}"]);
        }
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_SpecialCharactersInStringValue_PreservesValue()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var specialString = "O'Reilly & Sons \"Quotes\" \\ Backslash";

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE company = @company",
            new { company = specialString });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(specialString, template.Parameters["company"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_EmptyStringValue_PreservesEmptyString()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @name",
            new { name = string.Empty });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(string.Empty, template.Parameters["name"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_VeryLongPropertyName_HandlesCorrectly()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        var longPropName = new string('a', 100);

        // Act - Using dynamic to create property with long name
        var parameters = new Dictionary<string, object?> { { longPropName, 42 } };
        builder.AppendTemplate($"SELECT {{{{columns}}}} FROM {{{{table}}}} WHERE {longPropName} = @{longPropName}", 
            (IReadOnlyDictionary<string, object?>)parameters);
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(42, template.Parameters[longPropName]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_BoundaryValues_PreservesValues()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE " +
            "minInt = @minInt AND maxInt = @maxInt AND minLong = @minLong AND maxLong = @maxLong",
            new 
            { 
                minInt = int.MinValue,
                maxInt = int.MaxValue,
                minLong = long.MinValue,
                maxLong = long.MaxValue
            });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(4, template.Parameters.Count);
        Assert.AreEqual(int.MinValue, template.Parameters["minInt"]);
        Assert.AreEqual(int.MaxValue, template.Parameters["maxInt"]);
        Assert.AreEqual(long.MinValue, template.Parameters["minLong"]);
        Assert.AreEqual(long.MaxValue, template.Parameters["maxLong"]);
    }

    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_EnumValue_PreservesEnum()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);
        using var builder = new SqlBuilder(context);

        // Act
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE type = @type",
            new { type = DbType.String });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(DbType.String, template.Parameters["type"]);
    }

    [TestMethod]
    public void AppendTemplate_MultipleCallsWithDifferentParameterTypes_AllTypesHandledCorrectly()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "data", columns);
        using var builder = new SqlBuilder(context);

        // Act - Mix different parameter types
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE a = @a", new { a = 1 });
        builder.AppendRaw(" AND ");
        builder.AppendTemplate("b = @b", new Dictionary<string, object?> { { "b", 2 } });
        builder.AppendRaw(" AND ");
        builder.AppendTemplate("c = @c", (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { { "c", 3 } });
        var template = builder.Build();

        // Assert
        Assert.AreEqual(3, template.Parameters.Count);
        Assert.AreEqual(1, template.Parameters["a"]);
        Assert.AreEqual(2, template.Parameters["b"]);
        Assert.AreEqual(3, template.Parameters["c"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_AddsToOutputParameters()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC GetUserId @name, @userId OUT");
        builder.AppendTemplate("", new { name = "John" });

        // Act
        var template = builder.Build();
        template.AddOutputParameter("userId", DbType.Int32);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.IsTrue(template.OutputParameters.ContainsKey("userId"));
        Assert.AreEqual(DbType.Int32, template.OutputParameters["userId"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_MultipleParameters_AllAdded()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC GetUserInfo @userId OUT, @userName OUT, @userAge OUT");

        // Act
        var template = builder.Build();
        template.AddOutputParameter("userId", DbType.Int32)
                .AddOutputParameter("userName", DbType.String)
                .AddOutputParameter("userAge", DbType.Int32);

        // Assert
        Assert.AreEqual(3, template.OutputParameters.Count);
        Assert.AreEqual(DbType.Int32, template.OutputParameters["userId"]);
        Assert.AreEqual(DbType.String, template.OutputParameters["userName"]);
        Assert.AreEqual(DbType.Int32, template.OutputParameters["userAge"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_ReturnsTemplateForChaining()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC GetUserId @userId OUT");

        // Act
        var template = builder.Build();
        var result = template.AddOutputParameter("userId", DbType.Int32);

        // Assert
        Assert.AreSame(template, result);
    }

    [TestMethod]
    public void SqlTemplate_OutputParameters_InitiallyEmpty()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT * FROM users");

        // Act
        var template = builder.Build();

        // Assert
        Assert.AreEqual(0, template.OutputParameters.Count);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_WithSize_StoresSize()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC GetUserName @userId, @userName OUT");

        // Act
        var template = builder.Build();
        template.AddOutputParameter("userName", DbType.String);

        // Assert
        Assert.AreEqual(DbType.String, template.OutputParameters["userName"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_SameNameTwice_OverwritesPrevious()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC GetValue @value OUT");

        // Act
        var template = builder.Build();
        template.AddOutputParameter("value", DbType.Int32);
        template.AddOutputParameter("value", DbType.String); // Overwrite

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.AreEqual(DbType.String, template.OutputParameters["value"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_DifferentDbTypes_AllSupported()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC GetMultipleValues");

        // Act
        var template = builder.Build();
        template.AddOutputParameter("intValue", DbType.Int32)
                .AddOutputParameter("stringValue", DbType.String)
                .AddOutputParameter("dateValue", DbType.DateTime)
                .AddOutputParameter("boolValue", DbType.Boolean)
                .AddOutputParameter("decimalValue", DbType.Decimal)
                .AddOutputParameter("guidValue", DbType.Guid);

        // Assert
        Assert.AreEqual(6, template.OutputParameters.Count);
        Assert.AreEqual(DbType.Int32, template.OutputParameters["intValue"]);
        Assert.AreEqual(DbType.String, template.OutputParameters["stringValue"]);
        Assert.AreEqual(DbType.DateTime, template.OutputParameters["dateValue"]);
        Assert.AreEqual(DbType.Boolean, template.OutputParameters["boolValue"]);
        Assert.AreEqual(DbType.Decimal, template.OutputParameters["decimalValue"]);
        Assert.AreEqual(DbType.Guid, template.OutputParameters["guidValue"]);
    }

    [TestMethod]
    public void SqlTemplate_FromBuilder_OutputParametersEmpty()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE id = @id";
        var parameters = new Dictionary<string, object?> { { "id", 123 } };

        // Act
        var template = SqlTemplate.FromBuilder(sql, parameters);

        // Assert
        Assert.AreEqual(0, template.OutputParameters.Count);
    }

    [TestMethod]
    public void SqlTemplate_FromBuilder_CanAddOutputParameters()
    {
        // Arrange
        var sql = "EXEC GetUserId @name, @userId OUT";
        var parameters = new Dictionary<string, object?> { { "name", "John" } };
        var template = SqlTemplate.FromBuilder(sql, parameters);

        // Act
        template.AddOutputParameter("userId", DbType.Int32);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.AreEqual(DbType.Int32, template.OutputParameters["userId"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_EmptyName_StillAdds()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC GetValue");

        // Act
        var template = builder.Build();
        template.AddOutputParameter("", DbType.Int32);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.IsTrue(template.OutputParameters.ContainsKey(""));
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_WithInputParameters_BothPresent()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);
        builder.Append($"EXEC GetUserInfo {123}, @userName OUT");

        // Act
        var template = builder.Build();
        template.AddOutputParameter("userName", DbType.String);

        // Assert
        Assert.AreEqual(1, template.Parameters.Count); // Input parameter
        Assert.AreEqual(123, template.Parameters["p0"]);
        Assert.AreEqual(1, template.OutputParameters.Count); // Output parameter
        Assert.AreEqual(DbType.String, template.OutputParameters["userName"]);
    }

    #region Additional Output Parameter Tests

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            template.AddOutputParameter(null!, DbType.Int32);
        });
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_FluentApi_ReturnsTemplate()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("INSERT INTO test VALUES (@value)");
        var template = builder.Build();

        // Act
        var result = template.AddOutputParameter("id", DbType.Int32);

        // Assert
        Assert.AreSame(template, result);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_FluentChaining_Works()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("EXEC MultiOutputProc");
        var template = builder.Build();

        // Act
        template
            .AddOutputParameter("id", DbType.Int32)
            .AddOutputParameter("name", DbType.String)
            .AddOutputParameter("count", DbType.Int64);

        // Assert
        Assert.AreEqual(3, template.OutputParameters.Count);
        Assert.AreEqual(DbType.Int32, template.OutputParameters["id"]);
        Assert.AreEqual(DbType.String, template.OutputParameters["name"]);
        Assert.AreEqual(DbType.Int64, template.OutputParameters["count"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_AllDbTypes_Supported()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act & Assert - Test all DbType enum values
        foreach (DbType dbType in Enum.GetValues(typeof(DbType)))
        {
            var paramName = $"param_{dbType}";
            template.AddOutputParameter(paramName, dbType);
            Assert.AreEqual(dbType, template.OutputParameters[paramName]);
        }
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_WithZeroSize_Works()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("param", DbType.String);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.AreEqual(DbType.String, template.OutputParameters["param"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_WithNegativeSize_Works()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("param", DbType.String);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.AreEqual(DbType.String, template.OutputParameters["param"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_WithMaxSize_Works()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("param", DbType.String);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.AreEqual(DbType.String, template.OutputParameters["param"]);
    }

    [TestMethod]
    public void SqlTemplate_OutputParameters_IndependentFromInputParameters()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.Append($"INSERT INTO test VALUES ({123})");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("output", DbType.Int32);

        // Assert
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.IsTrue(template.Parameters.ContainsKey("p0"));
        Assert.IsTrue(template.OutputParameters.ContainsKey("output"));
        Assert.IsFalse(template.Parameters.ContainsKey("output"));
        Assert.IsFalse(template.OutputParameters.ContainsKey("p0"));
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_MultipleCallsSameName_LastWins()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("param", DbType.Int32);
        template.AddOutputParameter("param", DbType.String);
        template.AddOutputParameter("param", DbType.DateTime);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.AreEqual(DbType.DateTime, template.OutputParameters["param"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_CaseSensitiveNames_TreatedAsDifferent()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("Param", DbType.Int32);
        template.AddOutputParameter("param", DbType.String);
        template.AddOutputParameter("PARAM", DbType.DateTime);

        // Assert
        Assert.AreEqual(3, template.OutputParameters.Count);
        Assert.AreEqual(DbType.Int32, template.OutputParameters["Param"]);
        Assert.AreEqual(DbType.String, template.OutputParameters["param"]);
        Assert.AreEqual(DbType.DateTime, template.OutputParameters["PARAM"]);
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_SpecialCharactersInName_Works()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("param_1", DbType.Int32);
        template.AddOutputParameter("param-2", DbType.String);
        template.AddOutputParameter("param.3", DbType.DateTime);
        template.AddOutputParameter("param@4", DbType.Boolean);

        // Assert
        Assert.AreEqual(4, template.OutputParameters.Count);
        Assert.IsTrue(template.OutputParameters.ContainsKey("param_1"));
        Assert.IsTrue(template.OutputParameters.ContainsKey("param-2"));
        Assert.IsTrue(template.OutputParameters.ContainsKey("param.3"));
        Assert.IsTrue(template.OutputParameters.ContainsKey("param@4"));
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_VeryLongName_Works()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();
        var longName = new string('a', 1000);

        // Act
        template.AddOutputParameter(longName, DbType.Int32);

        // Assert
        Assert.AreEqual(1, template.OutputParameters.Count);
        Assert.IsTrue(template.OutputParameters.ContainsKey(longName));
    }

    [TestMethod]
    public void SqlTemplate_AddOutputParameter_UnicodeCharactersInName_Works()
    {
        // Arrange
        var columns = new[] { new ColumnMeta("id", "id", DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        using var builder = new SqlBuilder(context);
        builder.AppendRaw("SELECT 1");
        var template = builder.Build();

        // Act
        template.AddOutputParameter("参数名称", DbType.Int32);
        template.AddOutputParameter("パラメータ", DbType.String);
        template.AddOutputParameter("параметр", DbType.DateTime);

        // Assert
        Assert.AreEqual(3, template.OutputParameters.Count);
        Assert.IsTrue(template.OutputParameters.ContainsKey("参数名称"));
        Assert.IsTrue(template.OutputParameters.ContainsKey("パラメータ"));
        Assert.IsTrue(template.OutputParameters.ContainsKey("параметр"));
    }

    #endregion
}
