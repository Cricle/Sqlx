// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator.Core;

/// <summary>
/// Tests for IndentedStringBuilder utility class.
/// Tests string building with automatic indentation management.
/// </summary>
[TestClass]
public class IndentedStringBuilderTests
{
    /// <summary>
    /// Tests basic string building functionality.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_Append_AppendsStringCorrectly()
    {
        var builder = new IndentedStringBuilder(null);

        builder.Append("Hello");
        builder.Append(" World");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("Hello"), "Should contain first appended string");
        Assert.IsTrue(result.Contains("World"), "Should contain second appended string");
    }
    private static readonly char[] separator = new[] { '\n', '\r' };

    /// <summary>
    /// Tests AppendLine functionality.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_AppendLine_AddsNewlines()
    {
        var builder = new IndentedStringBuilder(null);

        builder.AppendLine("First line");
        builder.AppendLine("Second line");
        builder.AppendLine(); // Empty line
        builder.AppendLine("Fourth line");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("First line"), "Should contain first line");
        Assert.IsTrue(result.Contains("Second line"), "Should contain second line");
        Assert.IsTrue(result.Contains("Fourth line"), "Should contain fourth line");

        var lines = result.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        Assert.IsTrue(lines.Length >= 3, "Should have multiple lines");
    }

    /// <summary>
    /// Tests indentation functionality.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_PushIndent_IncreasesIndentation()
    {
        var builder = new IndentedStringBuilder(null);

        builder.AppendLine("Level 0");
        builder.PushIndent();
        builder.AppendLine("Level 1");
        builder.PushIndent();
        builder.AppendLine("Level 2");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("Level 0"), "Should contain level 0 content");
        Assert.IsTrue(result.Contains("Level 1"), "Should contain level 1 content");
        Assert.IsTrue(result.Contains("Level 2"), "Should contain level 2 content");

        // Check that indented lines have more leading whitespace
        var lines = result.Split('\n');
        var level0Line = Array.Find(lines, line => line.Contains("Level 0"));
        var level1Line = Array.Find(lines, line => line.Contains("Level 1"));
        var level2Line = Array.Find(lines, line => line.Contains("Level 2"));

        Assert.IsNotNull(level0Line, "Should find level 0 line");
        Assert.IsNotNull(level1Line, "Should find level 1 line");
        Assert.IsNotNull(level2Line, "Should find level 2 line");

        var level0Indent = level0Line.Length - level0Line.TrimStart().Length;
        var level1Indent = level1Line.Length - level1Line.TrimStart().Length;
        var level2Indent = level2Line.Length - level2Line.TrimStart().Length;

        Assert.IsTrue(level1Indent > level0Indent, "Level 1 should be more indented than level 0");
        Assert.IsTrue(level2Indent > level1Indent, "Level 2 should be more indented than level 1");
    }

    /// <summary>
    /// Tests PopIndent functionality.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_PopIndent_DecreasesIndentation()
    {
        var builder = new IndentedStringBuilder(null);

        builder.AppendLine("Level 0");
        builder.PushIndent();
        builder.AppendLine("Level 1");
        builder.PushIndent();
        builder.AppendLine("Level 2");
        builder.PopIndent();
        builder.AppendLine("Back to Level 1");
        builder.PopIndent();
        builder.AppendLine("Back to Level 0");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");

        var lines = result.Split('\n');
        var level0Line = Array.Find(lines, line => line.Contains("Level 0"));
        var level1Line = Array.Find(lines, line => line.Contains("Level 1"));
        var level2Line = Array.Find(lines, line => line.Contains("Level 2"));
        var backToLevel1Line = Array.Find(lines, line => line.Contains("Back to Level 1"));
        var backToLevel0Line = Array.Find(lines, line => line.Contains("Back to Level 0"));

        Assert.IsNotNull(level0Line, "Should find level 0 line");
        Assert.IsNotNull(backToLevel1Line, "Should find back to level 1 line");
        Assert.IsNotNull(backToLevel0Line, "Should find back to level 0 line");

        var level0Indent = level0Line?.Length - level0Line?.TrimStart().Length ?? 0;
        var level1Indent = level1Line?.Length - level1Line?.TrimStart().Length ?? 0;
        var backToLevel1Indent = backToLevel1Line?.Length - backToLevel1Line?.TrimStart().Length ?? 0;
        var backToLevel0Indent = backToLevel0Line?.Length - backToLevel0Line?.TrimStart().Length ?? 0;

        Assert.AreEqual(level1Indent, backToLevel1Indent, "Should return to same indentation level");
        Assert.AreEqual(level0Indent, backToLevel0Indent, "Should return to original indentation level");
    }

    /// <summary>
    /// Tests initialization with content.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_InitializeWithContent_PreservesInitialContent()
    {
        var initialContent = "Initial content line";
        var builder = new IndentedStringBuilder(initialContent);

        builder.AppendLine(" additional content");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("Initial content line"), "Should preserve initial content");
        Assert.IsTrue(result.Contains("additional content"), "Should include additional content");
    }

    /// <summary>
    /// Tests handling of null and empty strings.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_HandleNullAndEmpty_WorksCorrectly()
    {
        var builder = new IndentedStringBuilder(null);

        builder.Append(null);
        builder.Append("");
        builder.AppendLine(null);
        builder.AppendLine("");
        builder.Append("Valid content");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("Valid content"), "Should contain valid content");
    }

    /// <summary>
    /// Tests complex indentation scenarios.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_ComplexIndentation_WorksCorrectly()
    {
        var builder = new IndentedStringBuilder(null);

        // Simulate generating a nested code structure
        builder.AppendLine("public class TestClass");
        builder.AppendLine("{");
        builder.PushIndent();

        builder.AppendLine("public void Method1()");
        builder.AppendLine("{");
        builder.PushIndent();

        builder.AppendLine("if (condition)");
        builder.AppendLine("{");
        builder.PushIndent();

        builder.AppendLine("DoSomething();");

        builder.PopIndent();
        builder.AppendLine("}");

        builder.PopIndent();
        builder.AppendLine("}");

        builder.AppendLine();
        builder.AppendLine("public void Method2()");
        builder.AppendLine("{");
        builder.PushIndent();

        builder.AppendLine("DoSomethingElse();");

        builder.PopIndent();
        builder.AppendLine("}");

        builder.PopIndent();
        builder.AppendLine("}");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("public class TestClass"), "Should contain class declaration");
        Assert.IsTrue(result.Contains("DoSomething()"), "Should contain method calls");
        Assert.IsTrue(result.Contains("DoSomethingElse()"), "Should contain second method call");

        // Verify the structure looks reasonable
        var lines = result.Split('\n');
        Assert.IsTrue(lines.Length > 10, "Should have multiple lines of code");

        // Check that braces and content are properly indented
        var classLine = Array.Find(lines, line => line.Contains("public class TestClass"));
        var methodLine = Array.Find(lines, line => line.Contains("public void Method1()"));
        var doSomethingLine = Array.Find(lines, line => line.Contains("DoSomething()"));

        if (classLine != null && methodLine != null && doSomethingLine != null)
        {
            var classIndent = classLine.Length - classLine.TrimStart().Length;
            var methodIndent = methodLine.Length - methodLine.TrimStart().Length;
            var doSomethingIndent = doSomethingLine.Length - doSomethingLine.TrimStart().Length;

            Assert.IsTrue(methodIndent > classIndent, "Method should be more indented than class");
            Assert.IsTrue(doSomethingIndent > methodIndent, "Method content should be more indented than method declaration");
        }
    }

    /// <summary>
    /// Tests performance with large content.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_LargeContent_PerformsWell()
    {
        var builder = new IndentedStringBuilder(null);

        var startTime = DateTime.UtcNow;

        // Generate a large amount of content with varying indentation
        for (int i = 0; i < 1000; i++)
        {
            builder.AppendLine($"Line {i}");

            if (i % 100 == 0)
            {
                builder.PushIndent();
                builder.AppendLine($"Indented content {i}");
                builder.AppendLine($"More indented content {i}");
                builder.PopIndent();
            }
        }

        var result = builder.ToString();
        var endTime = DateTime.UtcNow;
        var buildTime = endTime - startTime;

        Assert.IsTrue(buildTime.TotalSeconds < 5,
            $"Large content building should be efficient. Took: {buildTime.TotalSeconds} seconds");

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Length > 10000, "Should produce substantial content");
        Assert.IsTrue(result.Contains("Line 0"), "Should contain first line");
        Assert.IsTrue(result.Contains("Line 999"), "Should contain last line");
        Assert.IsTrue(result.Contains("Indented content"), "Should contain indented content");
    }

    /// <summary>
    /// Tests edge case with excessive indentation levels.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_ExcessiveIndentation_HandlesGracefully()
    {
        var builder = new IndentedStringBuilder(null);

        // Push many levels of indentation
        for (int i = 0; i < 20; i++)
        {
            builder.PushIndent();
        }

        builder.AppendLine("Deeply nested content");

        // Pop back to original level
        for (int i = 0; i < 20; i++)
        {
            builder.PopIndent();
        }

        builder.AppendLine("Back to original level");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("Deeply nested content"), "Should contain deeply nested content");
        Assert.IsTrue(result.Contains("Back to original level"), "Should contain content back at original level");

        var lines = result.Split('\n');
        var deepLine = Array.Find(lines, line => line.Contains("Deeply nested content"));
        var originalLine = Array.Find(lines, line => line.Contains("Back to original level"));

        if (deepLine is not null && originalLine is not null)
        {
            var deepIndent = deepLine.Length - deepLine.TrimStart().Length;
            var originalIndent = originalLine.Length - originalLine.TrimStart().Length;

            Assert.IsTrue(deepIndent > originalIndent, "Deeply nested content should be more indented");
            Assert.IsTrue(deepIndent > 50, "Should have substantial indentation for deeply nested content");
        }
    }

    /// <summary>
    /// Tests mixed content and indentation operations.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_MixedOperations_WorksCorrectly()
    {
        var builder = new IndentedStringBuilder("// Initial comment\n");

        builder.Append("namespace TestNamespace");
        builder.AppendLine();
        builder.AppendLine("{");
        builder.PushIndent();

        builder.Append("public class ");
        builder.Append("TestClass");
        builder.AppendLine();
        builder.AppendLine("{");
        builder.PushIndent();

        builder.AppendLine("private int _field;");
        builder.AppendLine();
        builder.AppendLine("public int Property { get; set; }");

        builder.PopIndent();
        builder.AppendLine("}");

        builder.PopIndent();
        builder.AppendLine("}");

        var result = builder.ToString();

        Assert.IsNotNull(result, "Should produce result");
        Assert.IsTrue(result.Contains("// Initial comment"), "Should preserve initial content");
        Assert.IsTrue(result.Contains("namespace TestNamespace"), "Should contain namespace");
        Assert.IsTrue(result.Contains("public class TestClass"), "Should contain class declaration");
        Assert.IsTrue(result.Contains("private int _field"), "Should contain field declaration");
        Assert.IsTrue(result.Contains("public int Property"), "Should contain property declaration");

        // Verify that the generated code looks properly structured
        var lines = result.Split('\n');
        Assert.IsTrue(lines.Length > 5, "Should have multiple lines");
    }
}
