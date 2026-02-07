// -----------------------------------------------------------------------
// <copyright file="ArgPlaceholderHandlerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for ArgPlaceholderHandler.
/// </summary>
[TestClass]
public class ArgPlaceholderHandlerTests
{
    [TestMethod]
    public void Arg_WithParam_GeneratesParameter()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--param id");

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Arg_WithParamAndName_UsesNameAlias()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--param userId --name id");

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Arg_WithoutParam_ThrowsException()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            handler.Process(context, "");
        });
    }

    [TestMethod]
    public void Arg_MySqlDialect_UsesCorrectPrefix()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.MySql);

        // Act
        var result = handler.Process(context, "--param id");

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Arg_PostgreSqlDialect_UsesCorrectPrefix()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.PostgreSql);

        // Act
        var result = handler.Process(context, "--param id");

        // Assert
        Assert.AreEqual("$id", result);
    }

    [TestMethod]
    public void Arg_OracleDialect_UsesCorrectPrefix()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.Oracle);

        // Act
        var result = handler.Process(context, "--param id");

        // Assert
        Assert.AreEqual(":id", result);
    }

    [TestMethod]
    public void Arg_SqlServerDialect_UsesCorrectPrefix()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.SqlServer);

        // Act
        var result = handler.Process(context, "--param id");

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Arg_SQLiteDialect_UsesCorrectPrefix()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.SQLite);

        // Act
        var result = handler.Process(context, "--param id");

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Arg_DB2Dialect_UsesCorrectPrefix()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.DB2);

        // Act
        var result = handler.Process(context, "--param id");

        // Assert
        Assert.AreEqual("?id", result);
    }

    [TestMethod]
    public void Arg_Render_ReturnsProcessedResult()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext();
        var parameters = new Dictionary<string, object?> { ["id"] = 42 };

        // Act
        var result = handler.Render(context, "--param id", parameters);

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Arg_RenderWithNullParameters_ReturnsProcessedResult()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Render(context, "--param id", null);

        // Assert
        Assert.AreEqual("@id", result);
    }

    [TestMethod]
    public void Arg_GetType_ReturnsStatic()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;

        // Act
        var type = handler.GetType("--param id");

        // Assert
        Assert.AreEqual(PlaceholderType.Static, type);
    }

    [TestMethod]
    public void Arg_HandlerName_ReturnsArg()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;

        // Act
        var name = handler.Name;

        // Assert
        Assert.AreEqual("arg", name);
    }

    [TestMethod]
    public void Arg_ComplexParameterName_GeneratesCorrectly()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--param user_id");

        // Assert
        Assert.AreEqual("@user_id", result);
    }

    [TestMethod]
    public void Arg_WithNameContainingSpecialChars_GeneratesCorrectly()
    {
        // Arrange
        var handler = ArgPlaceholderHandler.Instance;
        var context = CreateContext();

        // Act
        var result = handler.Process(context, "--param userId --name user_id");

        // Assert
        Assert.AreEqual("@user_id", result);
    }

    private static PlaceholderContext CreateContext(SqlDialect? dialect = null)
    {
        var columns = new List<ColumnMeta>
        {
            new("id", "Id", DbType.Int64, false),
            new("name", "Name", DbType.String, false)
        };
        return new PlaceholderContext(dialect ?? SqlDefine.SQLite, "users", columns);
    }
}
