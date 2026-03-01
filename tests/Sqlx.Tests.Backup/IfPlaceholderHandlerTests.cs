// <copyright file="IfPlaceholderHandlerTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests;

/// <summary>
/// Tests for IfPlaceholderHandler covering all conditional branches.
/// </summary>
[TestClass]
public class IfPlaceholderHandlerTests
{
    private IfPlaceholderHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = IfPlaceholderHandler.Instance;
    }

    [TestMethod]
    public void Name_ReturnsIf()
    {
        // Act
        var name = _handler.Name;

        // Assert
        Assert.AreEqual("if", name);
    }

    [TestMethod]
    public void ClosingTagName_ReturnsSlashIf()
    {
        // Act
        var closingTag = _handler.ClosingTagName;

        // Assert
        Assert.AreEqual("/if", closingTag);
    }

    [TestMethod]
    public void GetType_ReturnsDynamic()
    {
        // Act
        var type = _handler.GetType("notnull=param");

        // Assert
        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Process_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());

        // Act
        _handler.Process(context, "notnull=param");
    }

    [TestMethod]
    public void Render_ReturnsEmptyString()
    {
        // Arrange
        var context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());
        var parameters = new Dictionary<string, object?>();

        // Act
        var result = _handler.Render(context, "notnull=param", parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    // NotNull condition tests
    [TestMethod]
    public void ProcessBlock_NotNull_WithNonNullValue_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name = @name";

        // Act
        var result = _handler.ProcessBlock("notnull=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_NotNull_WithNullValue_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        var blockContent = "AND name = @name";

        // Act
        var result = _handler.ProcessBlock("notnull=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ProcessBlock_NotNull_WithMissingParameter_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>();
        var blockContent = "AND name = @name";

        // Act
        var result = _handler.ProcessBlock("notnull=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    // Null condition tests
    [TestMethod]
    public void ProcessBlock_Null_WithNullValue_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        var blockContent = "AND name IS NULL";

        // Act
        var result = _handler.ProcessBlock("null=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_Null_WithNonNullValue_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name IS NULL";

        // Act
        var result = _handler.ProcessBlock("null=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ProcessBlock_Null_WithMissingParameter_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>();
        var blockContent = "AND name IS NULL";

        // Act
        var result = _handler.ProcessBlock("null=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    // NotEmpty condition tests
    [TestMethod]
    public void ProcessBlock_NotEmpty_WithNonEmptyString_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name = @name";

        // Act
        var result = _handler.ProcessBlock("notempty=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_NotEmpty_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = "" };
        var blockContent = "AND name = @name";

        // Act
        var result = _handler.ProcessBlock("notempty=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ProcessBlock_NotEmpty_WithNullValue_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        var blockContent = "AND name = @name";

        // Act
        var result = _handler.ProcessBlock("notempty=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ProcessBlock_NotEmpty_WithNonEmptyCollection_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } };
        var blockContent = "AND id IN @ids";

        // Act
        var result = _handler.ProcessBlock("notempty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_NotEmpty_WithEmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
        var blockContent = "AND id IN @ids";

        // Act
        var result = _handler.ProcessBlock("notempty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    // Empty condition tests
    [TestMethod]
    public void ProcessBlock_Empty_WithEmptyString_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = "" };
        var blockContent = "AND name IS NULL";

        // Act
        var result = _handler.ProcessBlock("empty=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_Empty_WithNonEmptyString_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name IS NULL";

        // Act
        var result = _handler.ProcessBlock("empty=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ProcessBlock_Empty_WithNullValue_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        var blockContent = "AND name IS NULL";

        // Act
        var result = _handler.ProcessBlock("empty=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_Empty_WithEmptyCollection_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
        var blockContent = "AND 1=0";

        // Act
        var result = _handler.ProcessBlock("empty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_Empty_WithNonEmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } };
        var blockContent = "AND 1=0";

        // Act
        var result = _handler.ProcessBlock("empty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ProcessBlock_Empty_WithEnumerable_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["items"] = EmptyEnumerable() };
        var blockContent = "AND 1=0";

        // Act
        var result = _handler.ProcessBlock("empty=items", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    public void ProcessBlock_NotEmpty_WithEnumerable_ReturnsBlockContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["items"] = NonEmptyEnumerable() };
        var blockContent = "AND id IN @items";

        // Act
        var result = _handler.ProcessBlock("notempty=items", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ProcessBlock_InvalidCondition_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>();
        var blockContent = "content";

        // Act
        _handler.ProcessBlock("invalid=param", blockContent, parameters);
    }

    [TestMethod]
    public void ProcessBlock_WithNullParameters_HandlesGracefully()
    {
        // Arrange
        var blockContent = "content";

        // Act
        var result = _handler.ProcessBlock("notnull=param", blockContent, null);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    private static IEnumerable<int> EmptyEnumerable()
    {
        yield break;
    }

    private static IEnumerable<int> NonEmptyEnumerable()
    {
        yield return 1;
        yield return 2;
    }
}
