// -----------------------------------------------------------------------
// <copyright file="ValuesPlaceholderHandlerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for ValuesPlaceholderHandler.
/// </summary>
[TestClass]
public class ValuesPlaceholderHandlerTests
{
    [TestMethod]
    public void Values_BasicUsage_GeneratesParameterList()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "");

        // Assert
        Assert.IsTrue(result.Contains("@id"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@email"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_WithExclude_ExcludesColumns()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--exclude id");

        // Assert
        Assert.IsFalse(result.Contains("@id"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@email"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_WithMultipleExcludes_ExcludesAllSpecified()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--exclude id,email");

        // Assert
        Assert.IsFalse(result.Contains("@id"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
        Assert.IsFalse(result.Contains("@email"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_WithInlineExpression_UsesExpression()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--inline CreatedAt=CURRENT_TIMESTAMP");

        // Assert
        // Should still generate parameters for id, name, email since CreatedAt is not in our columns
        Assert.IsTrue(result.Contains("@id"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@email"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_WithParam_GeneratesSingleParameter()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--param ids");

        // Assert
        Assert.AreEqual("@ids", result);
    }

    [TestMethod]
    public void Values_WithParamAndCollection_ExpandsToMultipleParameters()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();
        var parameters = new Dictionary<string, object?>
        {
            ["ids"] = new List<int> { 1, 2, 3 }
        };

        // Act
        var result = handler.Render(context, "--param ids", parameters);

        // Assert
        Assert.IsTrue(result.Contains("@ids0"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@ids1"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@ids2"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_WithParamAndEmptyCollection_ReturnsNull()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();
        var parameters = new Dictionary<string, object?>
        {
            ["ids"] = new List<int>()
        };

        // Act
        var result = handler.Render(context, "--param ids", parameters);

        // Assert
        Assert.AreEqual("NULL", result);
    }

    [TestMethod]
    public void Values_WithParamAndNonCollection_ReturnsSingleParameter()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();
        var parameters = new Dictionary<string, object?>
        {
            ["id"] = 42
        };

        // Act
        var result = handler.Render(context, "--param id", parameters);

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Values_WithParamAndString_ReturnsSingleParameter()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();
        var parameters = new Dictionary<string, object?>
        {
            ["name"] = "John"
        };

        // Act
        var result = handler.Render(context, "--param name", parameters);

        // Assert
        Assert.AreEqual("@name", result);
    }

    [TestMethod]
    public void Values_WithParamAndArray_ExpandsToMultipleParameters()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();
        var parameters = new Dictionary<string, object?>
        {
            ["tags"] = new[] { "tag1", "tag2", "tag3" }
        };

        // Act
        var result = handler.Render(context, "--param tags", parameters);

        // Assert
        Assert.IsTrue(result.Contains("@tags0"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@tags1"), $"Result: {result}");
        Assert.IsTrue(result.Contains("@tags2"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_WithParamButNoParameters_FallsBackToStatic()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Render(context, "--param ids", null);

        // Assert
        Assert.IsTrue(result.Contains("@id") || result.Contains("@name"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_GetType_WithParam_ReturnsDynamic()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var type = handler.GetType("--param ids");

        // Assert
        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    public void Values_GetType_WithoutParam_ReturnsStatic()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var type = handler.GetType("");

        // Assert
        Assert.AreEqual(PlaceholderType.Static, type);
    }

    [TestMethod]
    public void Values_HandlerName_ReturnsValues()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var name = handler.Name;

        // Assert
        Assert.AreEqual("values", name);
    }

    [TestMethod]
    public void Values_MySqlDialect_UsesCorrectParameterPrefix()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.MySql);

        // Act
        var result = handler.Process(context, "");

        // Assert
        Assert.IsTrue(result.Contains("@"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_PostgreSqlDialect_UsesCorrectParameterPrefix()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.PostgreSql);

        // Act
        var result = handler.Process(context, "");

        // Assert
        Assert.IsTrue(result.Contains("$"), $"Result: {result}");
    }

    [TestMethod]
    public void Values_OracleDialect_UsesCorrectParameterPrefix()
    {
        // Arrange
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.Oracle);

        // Act
        var result = handler.Process(context, "");

        // Assert
        Assert.IsTrue(result.Contains(":"), $"Result: {result}");
    }

    private static PlaceholderContext CreateContext(SqlDialect? dialect = null)
    {
        var columns = new List<ColumnMeta>
        {
            new("id", "Id", DbType.Int64, false),
            new("name", "Name", DbType.String, false),
            new("email", "Email", DbType.String, false)
        };
        return new PlaceholderContext(dialect ?? SqlDefine.SQLite, "users", columns);
    }
}
