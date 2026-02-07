// -----------------------------------------------------------------------
// <copyright file="IfPlaceholderHandlerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System.Collections.Generic;

namespace Sqlx.Tests;

/// <summary>
/// Tests for IfPlaceholderHandler conditional logic.
/// </summary>
[TestClass]
public class IfPlaceholderHandlerTests
{
    [TestMethod]
    public void If_NotNull_WithNonNullValue_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name = @name";

        // Act
        var result = handler.ProcessBlock("notnull=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when parameter is not null");
    }

    [TestMethod]
    public void If_NotNull_WithNullValue_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        var blockContent = "AND name = @name";

        // Act
        var result = handler.ProcessBlock("notnull=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when parameter is null");
    }

    [TestMethod]
    public void If_NotNull_WithMissingParameter_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?>();
        var blockContent = "AND name = @name";

        // Act
        var result = handler.ProcessBlock("notnull=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when parameter is missing");
    }

    [TestMethod]
    public void If_Null_WithNullValue_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        var blockContent = "AND name IS NULL";

        // Act
        var result = handler.ProcessBlock("null=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when parameter is null");
    }

    [TestMethod]
    public void If_Null_WithNonNullValue_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name IS NULL";

        // Act
        var result = handler.ProcessBlock("null=name", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when parameter is not null");
    }

    [TestMethod]
    public void If_NotEmpty_WithNonEmptyString_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "test" };
        var blockContent = "AND description LIKE @search";

        // Act
        var result = handler.ProcessBlock("notempty=search", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when string is not empty");
    }

    [TestMethod]
    public void If_NotEmpty_WithEmptyString_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "" };
        var blockContent = "AND description LIKE @search";

        // Act
        var result = handler.ProcessBlock("notempty=search", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when string is empty");
    }

    [TestMethod]
    public void If_NotEmpty_WithNullValue_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = null };
        var blockContent = "AND description LIKE @search";

        // Act
        var result = handler.ProcessBlock("notempty=search", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when value is null");
    }

    [TestMethod]
    public void If_Empty_WithEmptyString_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "" };
        var blockContent = "-- No search filter";

        // Act
        var result = handler.ProcessBlock("empty=search", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when string is empty");
    }

    [TestMethod]
    public void If_Empty_WithNullValue_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = null };
        var blockContent = "-- No search filter";

        // Act
        var result = handler.ProcessBlock("empty=search", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when value is null");
    }

    [TestMethod]
    public void If_Empty_WithNonEmptyString_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "test" };
        var blockContent = "-- No search filter";

        // Act
        var result = handler.ProcessBlock("empty=search", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when string is not empty");
    }

    [TestMethod]
    public void If_NotEmpty_WithNonEmptyList_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } };
        var blockContent = "AND id IN (@ids)";

        // Act
        var result = handler.ProcessBlock("notempty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when list is not empty");
    }

    [TestMethod]
    public void If_NotEmpty_WithEmptyList_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
        var blockContent = "AND id IN (@ids)";

        // Act
        var result = handler.ProcessBlock("notempty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when list is empty");
    }

    [TestMethod]
    public void If_Empty_WithEmptyList_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
        var blockContent = "-- No ID filter";

        // Act
        var result = handler.ProcessBlock("empty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when list is empty");
    }

    [TestMethod]
    public void If_Empty_WithNonEmptyList_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } };
        var blockContent = "-- No ID filter";

        // Act
        var result = handler.ProcessBlock("empty=ids", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when list is not empty");
    }

    [TestMethod]
    public void If_NotEmpty_WithNonEmptyArray_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["tags"] = new[] { "tag1", "tag2" } };
        var blockContent = "AND tags IN (@tags)";

        // Act
        var result = handler.ProcessBlock("notempty=tags", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when array is not empty");
    }

    [TestMethod]
    public void If_NotEmpty_WithEmptyArray_ExcludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["tags"] = new string[0] };
        var blockContent = "AND tags IN (@tags)";

        // Act
        var result = handler.ProcessBlock("notempty=tags", blockContent, parameters);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should exclude content when array is empty");
    }

    [TestMethod]
    public void If_InvalidCondition_ThrowsException()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name = @name";

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            handler.ProcessBlock("invalid=name", blockContent, parameters);
        }, "Should throw exception for invalid condition");
    }

    [TestMethod]
    public void If_NoCondition_ThrowsException()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        var blockContent = "AND name = @name";

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            handler.ProcessBlock("", blockContent, parameters);
        }, "Should throw exception when no condition is provided");
    }

    [TestMethod]
    public void If_NullParameters_TreatsAsEmpty()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var blockContent = "AND name = @name";

        // Act
        var result = handler.ProcessBlock("notnull=name", blockContent, null);

        // Assert
        Assert.AreEqual(string.Empty, result, "Should treat null parameters as empty");
    }

    [TestMethod]
    public void If_NotNull_WithZeroValue_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["count"] = 0 };
        var blockContent = "AND count = @count";

        // Act
        var result = handler.ProcessBlock("notnull=count", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when value is 0 (not null)");
    }

    [TestMethod]
    public void If_NotNull_WithFalseValue_IncludesContent()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["active"] = false };
        var blockContent = "AND active = @active";

        // Act
        var result = handler.ProcessBlock("notnull=active", blockContent, parameters);

        // Assert
        Assert.AreEqual(blockContent, result, "Should include content when value is false (not null)");
    }

    [TestMethod]
    public void If_HandlerName_ReturnsIf()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;

        // Act
        var name = handler.Name;

        // Assert
        Assert.AreEqual("if", name, "Handler name should be 'if'");
    }

    [TestMethod]
    public void If_ClosingTagName_ReturnsSlashIf()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;

        // Act
        var closingTag = handler.ClosingTagName;

        // Assert
        Assert.AreEqual("/if", closingTag, "Closing tag name should be '/if'");
    }

    [TestMethod]
    public void If_GetType_ReturnsDynamic()
    {
        // Arrange
        var handler = IfPlaceholderHandler.Instance;

        // Act
        var type = handler.GetType("notnull=name");

        // Assert
        Assert.AreEqual(PlaceholderType.Dynamic, type, "Should return Dynamic placeholder type");
    }
}
