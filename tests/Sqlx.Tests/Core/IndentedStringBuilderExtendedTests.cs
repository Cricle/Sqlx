// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilderExtendedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;

namespace Sqlx.Tests.Core;

[TestClass]
public class IndentedStringBuilderExtendedTests
{
    [TestMethod]
    public void IndentedStringBuilder_Constructor_WithNullContent_CreatesEmptyBuilder()
    {
        // Arrange & Act
        var builder = new IndentedStringBuilder(null);

        // Assert
        Assert.AreEqual(string.Empty, builder.ToString());
    }

    [TestMethod]
    public void IndentedStringBuilder_Constructor_WithContent_InitializesWithContent()
    {
        // Arrange
        var content = "Initial content";

        // Act
        var builder = new IndentedStringBuilder(content);

        // Assert
        Assert.AreEqual(content, builder.ToString());
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendLine_WithoutIndent_AppendsDirectly()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);
        var text = "Hello World";

        // Act
        builder.AppendLine(text);

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains(text));
        Assert.IsTrue(result.EndsWith(Environment.NewLine));
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendLine_WithIndent_AppendsWithSpaces()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);
        var text = "Hello World";

        // Act
        builder.PushIndent();
        builder.AppendLine(text);

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.StartsWith("    ")); // Default indent is 4 spaces
        Assert.IsTrue(result.Contains(text));
    }

    [TestMethod]
    public void IndentedStringBuilder_PushIndent_IncreasesIndentLevel()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.PushIndent();
        builder.AppendLine("Level 1");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.StartsWith("    "));
    }

    [TestMethod]
    public void IndentedStringBuilder_PopIndent_DecreasesIndentLevel()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.PushIndent();
        builder.PushIndent();
        builder.AppendLine("Level 2");
        builder.PopIndent();
        builder.AppendLine("Level 1");

        // Assert
        var result = builder.ToString();
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        Assert.IsTrue(lines[0].StartsWith("        ")); // 8 spaces for level 2
        Assert.IsTrue(lines[1].StartsWith("    ")); // 4 spaces for level 1
    }

    [TestMethod]
    public void IndentedStringBuilder_PopIndent_AtZeroLevel_ThrowsException()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => builder.PopIndent());
    }

    [TestMethod]
    public void IndentedStringBuilder_MultipleIndentLevels_AppliesCorrectSpacing()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.AppendLine("Level 0");
        builder.PushIndent();
        builder.AppendLine("Level 1");
        builder.PushIndent();
        builder.AppendLine("Level 2");
        builder.PopIndent();
        builder.AppendLine("Level 1 Again");
        builder.PopIndent();
        builder.AppendLine("Level 0 Again");

        // Assert
        var result = builder.ToString();
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        Assert.AreEqual("Level 0", lines[0]);
        Assert.AreEqual("    Level 1", lines[1]);
        Assert.AreEqual("        Level 2", lines[2]);
        Assert.AreEqual("    Level 1 Again", lines[3]);
        Assert.AreEqual("Level 0 Again", lines[4]);
    }

    [TestMethod]
    public void IndentedStringBuilder_Append_WithoutNewline_AppendsDirectly()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);
        var text1 = "Hello";
        var text2 = " World";

        // Act
        builder.Append(text1);
        builder.Append(text2);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendChar_Works()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.Append('H');
        builder.Append('i');

        // Assert
        var result = builder.ToString();
        Assert.AreEqual("Hi", result);
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendEmptyLine_AddsNewline()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.AppendLine("First");
        builder.AppendLine();
        builder.AppendLine("Second");

        // Assert
        var result = builder.ToString();
        var lines = result.Split(Environment.NewLine, StringSplitOptions.None);
        
        Assert.AreEqual("First", lines[0]);
        Assert.AreEqual(string.Empty, lines[1]);
        Assert.AreEqual("Second", lines[2]);
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendLine_WithNullString_HandlesGracefully()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);
        string? nullString = null;

        // Act
        builder.AppendLine(nullString);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual(Environment.NewLine, result);
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendLine_WithEmptyString_AddsOnlyNewline()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.AppendLine(string.Empty);

        // Assert
        var result = builder.ToString();
        Assert.AreEqual(Environment.NewLine, result);
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendLineIf_WithTrueCondition_AppendsTrue()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.AppendLineIf(true, "True Value", "False Value");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.Contains("True Value"));
        Assert.IsFalse(result.Contains("False Value"));
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendLineIf_WithFalseCondition_AppendsFalse()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.AppendLineIf(false, "True Value", "False Value");

        // Assert
        var result = builder.ToString();
        Assert.IsFalse(result.Contains("True Value"));
        Assert.IsTrue(result.Contains("False Value"));
    }

    [TestMethod]
    public void IndentedStringBuilder_ComplexCodeGeneration_FormatsCorrectly()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act - Simulate generating a method
        builder.AppendLine("public void TestMethod()");
        builder.AppendLine("{");
        builder.PushIndent();
        
        builder.AppendLine("if (condition)");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("DoSomething();");
        builder.PopIndent();
        builder.AppendLine("}");
        
        builder.AppendLine("else");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("DoSomethingElse();");
        builder.PopIndent();
        builder.AppendLine("}");
        
        builder.PopIndent();
        builder.AppendLine("}");

        // Assert
        var result = builder.ToString();
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        Assert.AreEqual("public void TestMethod()", lines[0]);
        Assert.AreEqual("{", lines[1]);
        Assert.AreEqual("    if (condition)", lines[2]);
        Assert.AreEqual("    {", lines[3]);
        Assert.AreEqual("        DoSomething();", lines[4]);
        Assert.AreEqual("    }", lines[5]);
        Assert.AreEqual("    else", lines[6]);
        Assert.AreEqual("    {", lines[7]);
        Assert.AreEqual("        DoSomethingElse();", lines[8]);
        Assert.AreEqual("    }", lines[9]);
        Assert.AreEqual("}", lines[10]);
    }

    [TestMethod]
    public void IndentedStringBuilder_WithInitialContent_AppendsCorrectly()
    {
        // Arrange
        var initialContent = "Initial";
        var builder = new IndentedStringBuilder(initialContent);

        // Act
        builder.Append(" Content");

        // Assert
        Assert.AreEqual("Initial Content", builder.ToString());
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendWithIndent_HandlesWhitespace()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);

        // Act
        builder.PushIndent();
        builder.Append("   "); // Whitespace string
        builder.Append("Text");

        // Assert
        var result = builder.ToString();
        Assert.IsTrue(result.StartsWith("    ")); // Should have indent
        Assert.IsTrue(result.Contains("   Text"));
    }

    [TestMethod]
    public void IndentedStringBuilder_LargeContent_HandlesEfficiently()
    {
        // Arrange
        var builder = new IndentedStringBuilder(null);
        var iterations = 100; // Reduced for test performance

        // Act
        for (int i = 0; i < iterations; i++)
        {
            builder.AppendLine($"Line {i}");
        }

        // Assert
        var result = builder.ToString();
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(iterations, lines.Length);
        Assert.AreEqual("Line 0", lines[0]);
        Assert.AreEqual($"Line {iterations - 1}", lines[iterations - 1]);
    }
}
