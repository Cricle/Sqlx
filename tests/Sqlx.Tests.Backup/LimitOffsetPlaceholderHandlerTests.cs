// <copyright file="LimitOffsetPlaceholderHandlerTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests;

/// <summary>
/// Tests for LimitPlaceholderHandler and OffsetPlaceholderHandler covering all branches.
/// </summary>
[TestClass]
public class LimitOffsetPlaceholderHandlerTests
{
    private LimitPlaceholderHandler _limitHandler = null!;
    private OffsetPlaceholderHandler _offsetHandler = null!;
    private PlaceholderContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _limitHandler = LimitPlaceholderHandler.Instance;
        _offsetHandler = OffsetPlaceholderHandler.Instance;
        _context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());
    }

    // LimitPlaceholderHandler tests
    [TestMethod]
    public void Limit_Name_ReturnsLimit()
    {
        // Act
        var name = _limitHandler.Name;

        // Assert
        Assert.AreEqual("limit", name);
    }

    [TestMethod]
    public void Limit_GetType_ReturnsStatic()
    {
        // Act
        var type = _limitHandler.GetType("--count 10");

        // Assert
        Assert.AreEqual(PlaceholderType.Static, type);
    }

    [TestMethod]
    public void Limit_Process_WithCount_ReturnsLimitClause()
    {
        // Act
        var result = _limitHandler.Process(_context, "--count 10");

        // Assert
        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void Limit_Process_WithParam_ReturnsParameterizedLimitClause()
    {
        // Act
        var result = _limitHandler.Process(_context, "--param pageSize");

        // Assert
        Assert.AreEqual("LIMIT @pageSize", result);
    }

    [TestMethod]
    public void Limit_Process_WithoutOptions_ReturnsEmpty()
    {
        // Act
        var result = _limitHandler.Process(_context, "");

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Limit_Render_WithParam_ReturnsLimitClause()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["pageSize"] = 20 };

        // Act
        var result = _limitHandler.Render(_context, "--param pageSize", parameters);

        // Assert
        Assert.AreEqual("LIMIT 20", result);
    }

    [TestMethod]
    public void Limit_Render_WithNullValue_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["pageSize"] = null };

        // Act
        var result = _limitHandler.Render(_context, "--param pageSize", parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Limit_Render_WithoutParam_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>();

        // Act
        _limitHandler.Render(_context, "--count 10", parameters);
    }

    // OffsetPlaceholderHandler tests
    [TestMethod]
    public void Offset_Name_ReturnsOffset()
    {
        // Act
        var name = _offsetHandler.Name;

        // Assert
        Assert.AreEqual("offset", name);
    }

    [TestMethod]
    public void Offset_GetType_ReturnsStatic()
    {
        // Act
        var type = _offsetHandler.GetType("--count 20");

        // Assert
        Assert.AreEqual(PlaceholderType.Static, type);
    }

    [TestMethod]
    public void Offset_Process_WithCount_ReturnsOffsetClause()
    {
        // Act
        var result = _offsetHandler.Process(_context, "--count 20");

        // Assert
        Assert.AreEqual("OFFSET 20", result);
    }

    [TestMethod]
    public void Offset_Process_WithParam_ReturnsParameterizedOffsetClause()
    {
        // Act
        var result = _offsetHandler.Process(_context, "--param skip");

        // Assert
        Assert.AreEqual("OFFSET @skip", result);
    }

    [TestMethod]
    public void Offset_Process_WithoutOptions_ReturnsEmpty()
    {
        // Act
        var result = _offsetHandler.Process(_context, "");

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Offset_Render_WithParam_ReturnsOffsetClause()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["skip"] = 40 };

        // Act
        var result = _offsetHandler.Render(_context, "--param skip", parameters);

        // Assert
        Assert.AreEqual("OFFSET 40", result);
    }

    [TestMethod]
    public void Offset_Render_WithNullValue_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["skip"] = null };

        // Act
        var result = _offsetHandler.Render(_context, "--param skip", parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Offset_Render_WithoutParam_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>();

        // Act
        _offsetHandler.Render(_context, "--count 20", parameters);
    }

    // Combined usage tests
    [TestMethod]
    public void LimitAndOffset_Process_WithCounts_ReturnsBothClauses()
    {
        // Act
        var limitResult = _limitHandler.Process(_context, "--count 10");
        var offsetResult = _offsetHandler.Process(_context, "--count 20");

        // Assert
        Assert.AreEqual("LIMIT 10", limitResult);
        Assert.AreEqual("OFFSET 20", offsetResult);
    }

    [TestMethod]
    public void LimitAndOffset_Process_WithParams_ReturnsBothParameterizedClauses()
    {
        // Act
        var limitResult = _limitHandler.Process(_context, "--param pageSize");
        var offsetResult = _offsetHandler.Process(_context, "--param offset");

        // Assert
        Assert.AreEqual("LIMIT @pageSize", limitResult);
        Assert.AreEqual("OFFSET @offset", offsetResult);
    }

    [TestMethod]
    public void Limit_Process_WithDifferentDialects_UsesCorrectParameterPrefix()
    {
        // Arrange
        var mysqlContext = new PlaceholderContext(new MySqlDialect(), "test_table", new List<ColumnMeta>());
        var postgresContext = new PlaceholderContext(new PostgreSqlDialect(), "test_table", new List<ColumnMeta>());

        // Act
        var mysqlResult = _limitHandler.Process(mysqlContext, "--param limit");
        var postgresResult = _limitHandler.Process(postgresContext, "--param limit");

        // Assert
        Assert.IsTrue(mysqlResult.Contains("@") || mysqlResult.Contains("?"));
        Assert.IsTrue(postgresResult.Contains("@") || postgresResult.Contains("$"));
    }

    [TestMethod]
    public void Offset_Process_WithDifferentDialects_UsesCorrectParameterPrefix()
    {
        // Arrange
        var mysqlContext = new PlaceholderContext(new MySqlDialect(), "test_table", new List<ColumnMeta>());
        var postgresContext = new PlaceholderContext(new PostgreSqlDialect(), "test_table", new List<ColumnMeta>());

        // Act
        var mysqlResult = _offsetHandler.Process(mysqlContext, "--param offset");
        var postgresResult = _offsetHandler.Process(postgresContext, "--param offset");

        // Assert
        Assert.IsTrue(mysqlResult.Contains("@") || mysqlResult.Contains("?"));
        Assert.IsTrue(postgresResult.Contains("@") || postgresResult.Contains("$"));
    }
}
