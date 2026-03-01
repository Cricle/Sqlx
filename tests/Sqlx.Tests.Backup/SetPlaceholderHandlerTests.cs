// <copyright file="SetPlaceholderHandlerTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SetPlaceholderHandler covering all SET clause generation branches.
/// </summary>
[TestClass]
public class SetPlaceholderHandlerTests
{
    private SetPlaceholderHandler _handler = null!;
    private SqlServerDialect _dialect = null!;
    private List<ColumnMeta> _columns = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = SetPlaceholderHandler.Instance;
        _dialect = new SqlServerDialect();
        _columns = new List<ColumnMeta>
        {
            new ColumnMeta("id", "Id", System.Data.DbType.Int32, false),
            new ColumnMeta("name", "Name", System.Data.DbType.String, false),
            new ColumnMeta("email", "Email", System.Data.DbType.String, false),
            new ColumnMeta("version", "Version", System.Data.DbType.Int32, false),
            new ColumnMeta("priority", "Priority", System.Data.DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", System.Data.DbType.DateTime, false)
        };
    }

    [TestMethod]
    public void Name_ReturnsSet()
    {
        // Act
        var name = _handler.Name;

        // Assert
        Assert.AreEqual("set", name);
    }

    [TestMethod]
    public void GetType_WithoutParamOption_ReturnsStatic()
    {
        // Act
        var type = _handler.GetType("");

        // Assert
        Assert.AreEqual(PlaceholderType.Static, type);
    }

    [TestMethod]
    public void GetType_WithParamOption_ReturnsDynamic()
    {
        // Act
        var type = _handler.GetType("--param updates");

        // Assert
        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    public void Process_WithNoOptions_GeneratesAllColumns()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "");

        // Assert
        Assert.IsTrue(result.Contains("[id] = @id"));
        Assert.IsTrue(result.Contains("[name] = @name"));
        Assert.IsTrue(result.Contains("[email] = @email"));
        Assert.IsTrue(result.Contains("[version] = @version"));
        Assert.IsTrue(result.Contains("[priority] = @priority"));
        Assert.IsTrue(result.Contains("[updated_at] = @updated_at"));
    }

    [TestMethod]
    public void Process_WithExcludeOption_ExcludesSpecifiedColumns()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--exclude Id,Version");

        // Assert
        Assert.IsFalse(result.Contains("[id]"));
        Assert.IsFalse(result.Contains("[version]"));
        Assert.IsTrue(result.Contains("[name] = @name"));
        Assert.IsTrue(result.Contains("[email] = @email"));
        Assert.IsTrue(result.Contains("[priority] = @priority"));
        Assert.IsTrue(result.Contains("[updated_at] = @updated_at"));
    }

    [TestMethod]
    public void Process_WithInlineExpression_UsesExpression()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--inline Version=Version+1");

        // Assert
        Assert.IsTrue(result.Contains("[version] = [version]+1"));
        Assert.IsTrue(result.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Process_WithMultipleInlineExpressions_UsesAllExpressions()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--inline Version=Version+1,Priority=Priority*2");

        // Assert
        Assert.IsTrue(result.Contains("[version] = [version]+1"));
        Assert.IsTrue(result.Contains("[priority] = [priority]*2"));
        Assert.IsTrue(result.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Process_WithExcludeAndInline_CombinesBothOptions()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--exclude Id --inline Version=Version+1");

        // Assert
        Assert.IsFalse(result.Contains("[id]"));
        Assert.IsTrue(result.Contains("[version] = [version]+1"));
        Assert.IsTrue(result.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Process_WithParamOption_ReturnsEmptyString()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--param updates");

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Render_WithoutParamOption_CallsProcess()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);
        var parameters = new Dictionary<string, object?>();

        // Act
        var result = _handler.Render(context, "", parameters);

        // Assert
        Assert.IsTrue(result.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Render_WithParamOption_ReturnsParameterValue()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = "[name] = @name, [email] = @email"
        };

        // Act
        var result = _handler.Render(context, "--param updates", parameters);

        // Assert
        Assert.AreEqual("[name] = @name, [email] = @email", result);
    }

    [TestMethod]
    public void Render_WithParamOption_NullParameter_ReturnsEmptyString()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = null
        };

        // Act
        var result = _handler.Render(context, "--param updates", parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_WithParamOption_MissingParameter_ThrowsException()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);
        var parameters = new Dictionary<string, object?>();

        // Act
        _handler.Render(context, "--param updates", parameters);
    }

    [TestMethod]
    public void Render_WithParamOption_ExpressionValue_ReturnsExpression()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = "[priority] = [priority] + 1, [updated_at] = @updatedAt"
        };

        // Act
        var result = _handler.Render(context, "--param updates", parameters);

        // Assert
        Assert.AreEqual("[priority] = [priority] + 1, [updated_at] = @updatedAt", result);
    }

    [TestMethod]
    public void Process_WithDifferentDialects_UsesDialectSpecificWrapping()
    {
        // Arrange - MySQL uses backticks
        var mysqlDialect = new MySqlDialect();
        var context = new PlaceholderContext(mysqlDialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--exclude Id");

        // Assert
        Assert.IsTrue(result.Contains("`name` = @name"));
        Assert.IsTrue(result.Contains("`email` = @email"));
    }

    [TestMethod]
    public void Process_WithPostgreSqlDialect_UsesDoubleQuotes()
    {
        // Arrange - PostgreSQL uses double quotes
        var pgDialect = new PostgreSqlDialect();
        var context = new PlaceholderContext(pgDialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--exclude Id");

        // Assert
        Assert.IsTrue(result.Contains("\"name\" = @name"));
        Assert.IsTrue(result.Contains("\"email\" = @email"));
    }

    [TestMethod]
    public void Process_WithEmptyColumns_ReturnsEmptyString()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", new List<ColumnMeta>());

        // Act
        var result = _handler.Process(context, "");

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Process_WithSingleColumn_ReturnsOneSetClause()
    {
        // Arrange
        var singleColumn = new List<ColumnMeta> { new ColumnMeta("name", "Name", System.Data.DbType.String, false) };
        var context = new PlaceholderContext(_dialect, "users", singleColumn);

        // Act
        var result = _handler.Process(context, "");

        // Assert
        Assert.AreEqual("[name] = @name", result);
    }

    [TestMethod]
    public void Process_WithInlineExpression_ComplexExpression_HandlesCorrectly()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--inline Priority=Priority+Version*2");

        // Assert
        Assert.IsTrue(result.Contains("[priority] = [priority]+[version]*2"));
    }

    [TestMethod]
    public void Process_WithInlineExpression_CaseInsensitivePropertyMatch()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        var result = _handler.Process(context, "--inline version=Version+1");

        // Assert
        Assert.IsTrue(result.Contains("[version] = [version]+1"));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_WithNullParameters_ThrowsException()
    {
        // Arrange
        var context = new PlaceholderContext(_dialect, "users", _columns);

        // Act
        _handler.Render(context, "--param updates", null);
    }
}
