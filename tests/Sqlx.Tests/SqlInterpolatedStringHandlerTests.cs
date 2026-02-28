// <copyright file="SqlInterpolatedStringHandlerTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

#if NET6_0_OR_GREATER

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

/// <summary>
/// Tests for SqlInterpolatedStringHandler.
/// </summary>
[TestClass]
public class SqlInterpolatedStringHandlerTests
{
    [TestMethod]
    public void Constructor_InitializesWithBuilder()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);

        // Act
        var handler = new SqlInterpolatedStringHandler(10, 2, builder);

        // Assert
        // Constructor should not throw - ref struct cannot be tested with Assert.IsNotNull
    }

    [TestMethod]
    public void AppendLiteral_AppendsLiteralString()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 0, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users");

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("SELECT * FROM Users"));
    }

    [TestMethod]
    public void AppendLiteral_WithNullString_DoesNotAppend()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 0, builder);

        // Act
        handler.AppendLiteral(null!);

        // Assert
        var template = builder.Build();
        Assert.AreEqual(string.Empty, template.Sql);
    }

    [TestMethod]
    public void AppendLiteral_WithEmptyString_DoesNotAppend()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 0, builder);

        // Act
        handler.AppendLiteral(string.Empty);

        // Assert
        var template = builder.Build();
        Assert.AreEqual(string.Empty, template.Sql);
    }

    [TestMethod]
    public void AppendFormatted_AppendsParameterizedValue()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 1, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users WHERE Id = ");
        handler.AppendFormatted(123);

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(123, template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendFormatted_WithStringValue_AppendsParameterizedValue()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 1, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users WHERE Name = ");
        handler.AppendFormatted("John");

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual("John", template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendFormatted_WithNullValue_AppendsParameterizedNull()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 1, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users WHERE Name = ");
        handler.AppendFormatted<string>(null!);

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.IsNull(template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendFormatted_WithFormat_AppendsParameterizedValue()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 1, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users WHERE Id = ");
        handler.AppendFormatted(123, "D5");

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(123, template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendFormatted_WithFormat_IgnoresFormatString()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 1, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users WHERE Price = ");
        handler.AppendFormatted(123.45m, "C2");

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        // Format is ignored, value is used as-is
        Assert.AreEqual(123.45m, template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendFormatted_WithNullFormat_AppendsParameterizedValue()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(10, 1, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users WHERE Id = ");
        handler.AppendFormatted(123, null);

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        Assert.AreEqual(123, template.Parameters["p0"]);
    }

    [TestMethod]
    public void AppendFormatted_MultipleValues_AppendsMultipleParameters()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        var handler = new SqlInterpolatedStringHandler(50, 3, builder);

        // Act
        handler.AppendLiteral("SELECT * FROM Users WHERE Id = ");
        handler.AppendFormatted(123);
        handler.AppendLiteral(" AND Name = ");
        handler.AppendFormatted("John");
        handler.AppendLiteral(" AND Age = ");
        handler.AppendFormatted(30);

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.IsTrue(template.Sql.Contains("@p1"));
        Assert.IsTrue(template.Sql.Contains("@p2"));
        Assert.AreEqual(3, template.Parameters.Count);
        Assert.AreEqual(123, template.Parameters["p0"]);
        Assert.AreEqual("John", template.Parameters["p1"]);
        Assert.AreEqual(30, template.Parameters["p2"]);
    }

    [TestMethod]
    public void InterpolatedString_Integration_WorksCorrectly()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        int id = 123;
        string name = "John";

        // Act
        builder.Append($"SELECT * FROM Users WHERE Id = {id} AND Name = {name}");

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.IsTrue(template.Sql.Contains("@p1"));
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.AreEqual(123, template.Parameters["p0"]);
        Assert.AreEqual("John", template.Parameters["p1"]);
    }

    [TestMethod]
    public void InterpolatedString_WithFormat_Integration_WorksCorrectly()
    {
        // Arrange
        var builder = new SqlBuilder(SqlDefine.SQLite);
        decimal price = 123.456m;

        // Act
        builder.Append($"SELECT * FROM Products WHERE Price = {price:C2}");

        // Assert
        var template = builder.Build();
        Assert.IsTrue(template.Sql.Contains("@p0"));
        Assert.AreEqual(1, template.Parameters.Count);
        // Format is ignored in current implementation
        Assert.AreEqual(123.456m, template.Parameters["p0"]);
    }
}

#endif
