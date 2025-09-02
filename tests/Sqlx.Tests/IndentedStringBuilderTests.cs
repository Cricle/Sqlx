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
    /// Tests appending a character value.
    /// </summary>
    [TestMethod]
    public void Append_Char_AppendsCharacterWithIndentation()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append('X');

        // Assert
        Assert.AreEqual("    X", builder.ToString());
    }

    /// <summary>
    /// Tests the AppendLineIf method with true condition.
    /// </summary>
    [TestMethod]
    public void AppendLineIf_TrueCondition_AppendsTrueValue()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.AppendLineIf(true, "trueValue", "falseValue");

        // Assert
        Assert.AreEqual("trueValue" + Environment.NewLine, builder.ToString());
    }

    /// <summary>
    /// Tests the AppendLineIf method with false condition.
    /// </summary>
    [TestMethod]
    public void AppendLineIf_FalseCondition_AppendsFalseValue()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.AppendLineIf(false, "trueValue", "falseValue");

        // Assert
        Assert.AreEqual("falseValue" + Environment.NewLine, builder.ToString());
    }

    /// <summary>
    /// Tests that PopIndent throws InvalidOperationException when called at depth 0.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void PopIndent_AtDepthZero_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act - This should throw an exception
        builder.PopIndent();
    }

    /// <summary>
    /// Tests that PopIndent works correctly after PushIndent.
    /// </summary>
    [TestMethod]
    public void PopIndent_AfterPushIndent_ReducesIndentation()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("indented");
        builder.PopIndent();
        builder.Append("notIndented");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("    indented"));
        Assert.IsTrue(result.Contains("notIndented"));
    }

    /// <summary>
    /// Tests that multiple indentation levels work correctly.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_MultipleIndentationLevels_WorkCorrectly()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.Append("level0");
        builder.PushIndent();
        builder.Append("level1");
        builder.PushIndent();
        builder.Append("level2");
        builder.PopIndent();
        builder.Append("backToLevel1");
        builder.PopIndent();
        builder.Append("backToLevel0");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("level0"));
        Assert.IsTrue(result.Contains("    level1"));
        Assert.IsTrue(result.Contains("        level2"));
        Assert.IsTrue(result.Contains("    backToLevel1"));
        Assert.IsTrue(result.Contains("backToLevel0"));
    }

    /// <summary>
    /// Tests that indentation is applied to all append operations.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationAppliedToAllAppendOperations()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("string1");
        builder.Append('c');
        builder.AppendLine("string2");
        builder.AppendLine();

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("    string1"));
        Assert.IsTrue(result.Contains("    c"));
        Assert.IsTrue(result.Contains("    string2"));
        Assert.IsTrue(result.Contains(Environment.NewLine));
    }

    /// <summary>
    /// Tests that indentation is not applied to empty strings.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationNotAppliedToEmptyStrings()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("");
        builder.AppendLine("");

        // Assert
        var result = builder.ToString();
        Assert.AreEqual(Environment.NewLine, result);
    }

    /// <summary>
    /// Tests that indentation is not applied to whitespace-only strings.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationNotAppliedToWhitespaceOnlyStrings()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("   ");
        builder.AppendLine("   ");

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("       " + Environment.NewLine, result);
    }

    /// <summary>
    /// Tests that indentation works correctly with mixed content.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationWorksWithMixedContent()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.Append("start");
        builder.PushIndent();
        builder.Append("indented1");
        builder.AppendLine();
        builder.Append("indented2");
        builder.PopIndent();
        builder.Append("end");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("start"));
        Assert.IsTrue(result.Contains("    indented1"));
        Assert.IsTrue(result.Contains("    indented2"));
        Assert.IsTrue(result.Contains("end"));
    }

    /// <summary>
    /// Tests that indentation works correctly with special characters.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationWorksWithSpecialCharacters()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("line1\nline2");
        builder.AppendLine("line3\r\nline4");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("    line1\nline2"));
        Assert.IsTrue(result.Contains("    line3\r\nline4"));
    }

    /// <summary>
    /// Tests that indentation works correctly with Unicode characters.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationWorksWithUnicodeCharacters()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append("café");
        builder.Append('ñ');
        builder.AppendLine("über");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("    café"));
        Assert.IsTrue(result.Contains("    ñ"));
        Assert.IsTrue(result.Contains("    über"));
    }

    /// <summary>
    /// Tests that indentation works correctly with very long strings.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationWorksWithVeryLongStrings()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);
        var longString = new string('x', 1000);

        // Act
        builder.PushIndent();
        builder.Append(longString);

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.StartsWith("    "));
        Assert.IsTrue(result.Contains(longString));
    }

    /// <summary>
    /// Tests that indentation works correctly with null strings (should handle gracefully).
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationWorksWithNullStrings()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append((string)null);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    ", result);
    }

    /// <summary>
    /// Tests that indentation works correctly with null characters (should handle gracefully).
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_IndentationWorksWithNullCharacters()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append('\0');

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    \0", result);
    }

    /// <summary>
    /// Tests that IndentedStringBuilder handles null strings gracefully.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_HandlesNullStrings_Gracefully()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.Append((string)null);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    ", result);
    }

    /// <summary>
    /// Tests that IndentedStringBuilder handles null strings in AppendLine gracefully.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_HandlesNullStringsInAppendLine_Gracefully()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.AppendLine((string)null);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    " + Environment.NewLine, result);
    }

    /// <summary>
    /// Tests that IndentedStringBuilder handles null strings in AppendLineIf gracefully.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_HandlesNullStringsInAppendLineIf_Gracefully()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.AppendLineIf(true, (string)null, "falseValue");

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    " + Environment.NewLine, result);
    }

    /// <summary>
    /// Tests that IndentedStringBuilder handles null strings in AppendLineIf gracefully (false case).
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_HandlesNullStringsInAppendLineIfFalse_Gracefully()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.AppendLineIf(false, "trueValue", (string)null);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    " + Environment.NewLine, result);
    }

    /// <summary>
    /// Tests that IndentedStringBuilder handles null strings in AppendLineIf gracefully (both null).
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_HandlesNullStringsInAppendLineIfBothNull_Gracefully()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.AppendLineIf(true, (string)null, (string)null);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    " + Environment.NewLine, result);
    }

    /// <summary>
    /// Tests that IndentedStringBuilder handles null strings in AppendLineIf gracefully (both null, false case).
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_HandlesNullStringsInAppendLineIfBothNullFalse_Gracefully()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act
        builder.PushIndent();
        builder.AppendLineIf(false, (string)null, (string)null);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("    " + Environment.NewLine, result);
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
