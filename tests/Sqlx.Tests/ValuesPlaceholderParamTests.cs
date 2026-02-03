// <copyright file="ValuesPlaceholderParamTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;

/// <summary>
/// Tests for {{values --param}} placeholder functionality.
/// </summary>
[TestClass]
public class ValuesPlaceholderParamTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, false),
    };

    [TestMethod]
    public void ValuesPlaceholder_WithParamOption_GeneratesSingleParameterPlaceholder()
    {
        // Arrange
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var result = handler.Process(context, "--param ids");

        // Assert
        Assert.AreEqual("@ids", result);
    }

    [TestMethod]
    public void ValuesPlaceholder_WithParamOption_PostgreSQL_GeneratesPostgreSQLParameter()
    {
        // Arrange
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var result = handler.Process(context, "--param ids");

        // Assert
        Assert.AreEqual("$ids", result);
    }

    [TestMethod]
    public void ValuesPlaceholder_WithParamOption_MySQL_GeneratesMySQLParameter()
    {
        // Arrange
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var result = handler.Process(context, "--param ids");

        // Assert
        Assert.AreEqual("@ids", result);
    }

    [TestMethod]
    public void ValuesPlaceholder_WithoutParamOption_GeneratesEntityPropertyList()
    {
        // Arrange
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var result = handler.Process(context, "");

        // Assert
        Assert.AreEqual("@id, @name, @email", result);
    }

    [TestMethod]
    public void ValuesPlaceholder_WithExcludeOption_GeneratesFilteredPropertyList()
    {
        // Arrange
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var handler = ValuesPlaceholderHandler.Instance;

        // Act
        var result = handler.Process(context, "--exclude Id");

        // Assert
        Assert.AreEqual("@name, @email", result);
    }

    [TestMethod]
    public void ValuesPlaceholder_WithParamAndExcludeOptions_ParamTakesPrecedence()
    {
        // Arrange
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var handler = ValuesPlaceholderHandler.Instance;

        // Act - when --param is present, --exclude should be ignored
        var result = handler.Process(context, "--param ids --exclude Id");

        // Assert
        Assert.AreEqual("@ids", result);
    }
}
