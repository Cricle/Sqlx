// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

/// <summary>
/// Unit tests for the IndentedStringBuilder class.
/// </summary>
[TestClass]
public class IndentedStringBuilderTests
{
    /// <summary>
    /// Tests that the constructor initializes correctly with empty content.
    /// </summary>
    [TestMethod]
    public void Constructor_EmptyString_InitializesCorrectly()
    {
        // Arrange & Act
        var builder = new IndentedStringBuilder(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, builder.ToString());
    }

    /// <summary>
    /// Tests that the constructor initializes correctly with initial content.
    /// </summary>
    [TestMethod]
    public void Constructor_WithContent_PreservesContent()
    {
        // Arrange & Act
        var builder = new IndentedStringBuilder("test");

        // Assert
        Assert.AreEqual("test", builder.ToString());
    }

    /// <summary>
    /// Tests basic append functionality without indentation.
    /// </summary>
    [TestMethod]
    public void Append_WithoutIndent_AppendsDirectly()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.Append("hello");

        // Assert
        Assert.AreEqual("hello", builder.ToString());
    }

    /// <summary>
    /// Tests appending with current indentation.
    /// </summary>
    [TestMethod]
    public void Append_WithCurrentIndent_AppendsWithIndentation()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.Append("hello");
        builder.PushIndent();
        builder.Append("world");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("hello"));
        Assert.IsTrue(result.Contains("    world"));
    }

    /// <summary>
    /// Tests appending a line with newline character.
    /// </summary>
    [TestMethod]
    public void AppendLine_WithContent_AppendsLineWithNewline()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.AppendLine("hello");

        // Assert
        Assert.AreEqual("hello" + Environment.NewLine, builder.ToString());
    }

    /// <summary>
    /// Tests appending an empty line.
    /// </summary>
    [TestMethod]
    public void AppendLine_Empty_AppendsNewlineOnly()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.AppendLine();

        // Assert
        Assert.AreEqual(Environment.NewLine, builder.ToString());
    }

    /// <summary>
    /// Tests increasing indentation level.
    /// </summary>
    [TestMethod]
    public void IncreaseIndent_IncreasesIndentationLevel()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("indented");

        // Assert
        Assert.AreEqual("    indented", builder.ToString());
    }

    /// <summary>
    /// Tests decreasing indentation level.
    /// </summary>
    [TestMethod]
    public void DecreaseIndent_DecreasesIndentationLevel()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("indented");
        builder.PopIndent();
        builder.Append("not indented");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("    indented"));
        Assert.IsTrue(result.Contains("not indented"));
    }

    /// <summary>
    /// Tests nested indentation.
    /// </summary>
    [TestMethod]
    public void NestedIndentation_ProducesCorrectIndents()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.AppendLine("class Test");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("public void Method()");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("Console.WriteLine(\"test\");");
        builder.PopIndent();
        builder.AppendLine("}");
        builder.PopIndent();
        builder.AppendLine("}");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("class Test"));
        Assert.IsTrue(result.Contains("    public void Method()"));
        Assert.IsTrue(result.Contains("        Console.WriteLine"));
    }
}
