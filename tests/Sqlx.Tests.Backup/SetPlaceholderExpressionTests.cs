// <copyright file="SetPlaceholderExpressionTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for {{set --param}} placeholder with Expression&lt;Func&lt;T, T&gt;&gt; parameters.
/// </summary>
[TestClass]
public class SetPlaceholderExpressionTests
{
    private readonly PlaceholderContext _context;

    public SetPlaceholderExpressionTests()
    {
        _context = new PlaceholderContext(
            dialect: SqlDefine.SQLite,
            tableName: "users",
            columns: new List<ColumnMeta>
            {
                new("id", "Id", System.Data.DbType.Int32, false),
                new("name", "Name", System.Data.DbType.String, false),
                new("email", "Email", System.Data.DbType.String, false),
                new("age", "Age", System.Data.DbType.Int32, false),
                new("version", "Version", System.Data.DbType.Int32, false),
            });
    }

    [TestMethod]
    public void SetPlaceholder_WithParamOption_IsDynamic()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";

        // Act
        var type = handler.GetType(options);

        // Assert
        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    public void SetPlaceholder_WithParamOption_ReturnsEmptyForStaticProcessing()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";

        // Act
        var result = handler.Process(_context, options);

        // Assert - 静态处理时应该返回空字符串，因为需要动态渲染
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void SetPlaceholder_WithExpression_RendersSingleFieldUpdate()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";
        
        // Expression: u => new User { Name = "John" }
        var updatesSql = "[name] = @p0";
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = updatesSql
        };

        // Act
        var result = handler.Render(_context, options, parameters);

        // Assert
        Assert.AreEqual("[name] = @p0", result);
    }

    [TestMethod]
    public void SetPlaceholder_WithExpression_RendersMultipleFieldsUpdate()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";
        
        // Expression: u => new User { Name = "John", Email = "john@example.com" }
        var updatesSql = "[name] = @p0, [email] = @p1";
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = updatesSql
        };

        // Act
        var result = handler.Render(_context, options, parameters);

        // Assert
        Assert.AreEqual("[name] = @p0, [email] = @p1", result);
    }

    [TestMethod]
    public void SetPlaceholder_WithExpression_RendersIncrementExpression()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";
        
        // Expression: u => new User { Version = u.Version + 1 }
        var updatesSql = "[version] = ([version] + @p0)";
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = updatesSql
        };

        // Act
        var result = handler.Render(_context, options, parameters);

        // Assert
        Assert.AreEqual("[version] = ([version] + @p0)", result);
    }

    [TestMethod]
    public void SetPlaceholder_WithExpression_RendersMixedUpdate()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";
        
        // Expression: u => new User { Name = "John", Version = u.Version + 1 }
        var updatesSql = "[name] = @p0, [version] = ([version] + @p1)";
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = updatesSql
        };

        // Act
        var result = handler.Render(_context, options, parameters);

        // Assert
        Assert.AreEqual("[name] = @p0, [version] = ([version] + @p1)", result);
    }

    [TestMethod]
    public void SetPlaceholder_WithParamOption_ThrowsWhenParameterMissing()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";
        var parameters = new Dictionary<string, object?>();

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            handler.Render(_context, options, parameters));
        Assert.IsTrue(ex.Message.Contains("updates"));
    }

    [TestMethod]
    public void SetPlaceholder_WithParamOption_HandlesNullParameter()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--param updates";
        var parameters = new Dictionary<string, object?>
        {
            ["updates"] = null
        };

        // Act
        var result = handler.Render(_context, options, parameters);

        // Assert - null 应该返回空字符串
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void SetPlaceholder_WithoutParamOption_RemainsStatic()
    {
        // Arrange
        var handler = Placeholders.SetPlaceholderHandler.Instance;
        var options = "--exclude Id";

        // Act
        var type = handler.GetType(options);

        // Assert
        Assert.AreEqual(PlaceholderType.Static, type);
    }
}
