// -----------------------------------------------------------------------
// <copyright file="UtilityClassesFunctionalTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Functional tests for utility classes like IndentedStringBuilder, NameMapper, etc.
/// </summary>
[TestClass]
public class UtilityClassesFunctionalTests
{
    #region IndentedStringBuilder Tests

    /// <summary>
    /// Tests IndentedStringBuilder indentation functionality with real code generation scenarios.
    /// </summary>
    [TestMethod]
    public void IndentedStringBuilder_GeneratesCorrectIndentedCode()
    {
        // Arrange
        var builder = new IndentedStringBuilder(string.Empty);

        // Act - simulate generating a method with nested blocks
        builder.AppendLine("public partial class TestClass");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("public partial void TestMethod()");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("using var __cmd__ = _connection.CreateCommand();");
        builder.AppendLine("__cmd__.CommandText = \"SELECT * FROM Users\";");
        builder.AppendLine("using var __reader__ = __cmd__.ExecuteReader();");
        builder.AppendLine("if (__reader__.Read())");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("return __reader__.GetInt32(0);");
        builder.PopIndent();
        builder.AppendLine("}");
        builder.AppendLine("return 0;");
        builder.PopIndent();
        builder.AppendLine("}");
        builder.PopIndent();
        builder.AppendLine("}");

        var result = builder.ToString();

        // Assert - verify proper indentation structure
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.IsTrue(lines.Any(l => l.StartsWith("public partial class")), "Class declaration should not be indented");
        Assert.IsTrue(lines.Any(l => l.StartsWith("    public partial void")), "Method should be indented once");
        Assert.IsTrue(lines.Any(l => l.StartsWith("        using var __cmd__")), "Method body should be indented twice");
        Assert.IsTrue(lines.Any(l => l.StartsWith("            return __reader__")), "Inner block should be indented three times");

        // Verify the generated code is syntactically valid C#
        var syntaxTree = CSharpSyntaxTree.ParseText(result);
        var diagnostics = syntaxTree.GetDiagnostics();
        var syntaxErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(syntaxErrors.Any(), $"Generated code should be syntactically valid. Errors: {string.Join(", ", syntaxErrors.Select(e => e.ToString()))}");
    }

    #endregion

    #region CSharpGenerator Tests

    /// <summary>
    /// Tests that CSharpGenerator class has correct structure and implements required interfaces.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_Class_HasCorrectStructure()
    {
        // Arrange
        var classType = typeof(CSharpGenerator);
        var iSourceGeneratorType = typeof(Microsoft.CodeAnalysis.ISourceGenerator);

        // Act & Assert
        Assert.IsNotNull(classType, "CSharpGenerator class should exist");
        Assert.IsTrue(classType.IsPublic, "CSharpGenerator should be public");
        Assert.IsFalse(classType.IsAbstract, "CSharpGenerator should not be abstract");
        Assert.IsFalse(classType.IsSealed, "CSharpGenerator should not be sealed");
        Assert.AreEqual("Sqlx", classType.Namespace, "CSharpGenerator should be in Sqlx namespace");
        Assert.AreEqual(typeof(AbstractGenerator), classType.BaseType, "CSharpGenerator should inherit from AbstractGenerator");

        // Test interface implementation
        var interfaces = classType.GetInterfaces();
        Assert.IsTrue(interfaces.Contains(iSourceGeneratorType), "CSharpGenerator should implement ISourceGenerator");

        // Test methods
        var initializeMethod = classType.GetMethod("Initialize", 
            BindingFlags.Public | BindingFlags.Instance);
        var executeMethod = classType.GetMethod("Execute", 
            BindingFlags.Public | BindingFlags.Instance);

        Assert.IsNotNull(initializeMethod, "CSharpGenerator should have Initialize method");
        Assert.IsNotNull(executeMethod, "CSharpGenerator should have Execute method");

        // Test instantiation
        var generator = new CSharpGenerator();
        Assert.IsNotNull(generator);
        Assert.IsTrue(generator is CSharpGenerator);
        Assert.IsTrue(generator is AbstractGenerator);
        Assert.IsTrue(generator is Microsoft.CodeAnalysis.ISourceGenerator);
    }

    /// <summary>
    /// Tests that AbstractGenerator class has correct structure and implements required interfaces.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_Class_HasCorrectStructure()
    {
        // Arrange
        var classType = typeof(AbstractGenerator);
        var iSourceGeneratorType = typeof(Microsoft.CodeAnalysis.ISourceGenerator);

        // Act & Assert
        Assert.IsNotNull(classType, "AbstractGenerator class should exist");
        Assert.IsTrue(classType.IsPublic, "AbstractGenerator should be public");
        Assert.IsTrue(classType.IsAbstract, "AbstractGenerator should be abstract");
        Assert.IsFalse(classType.IsSealed, "AbstractGenerator should not be sealed");
        Assert.AreEqual("Sqlx", classType.Namespace, "AbstractGenerator should be in Sqlx namespace");

        // Test interface implementation
        var interfaces = classType.GetInterfaces();
        Assert.IsTrue(interfaces.Contains(iSourceGeneratorType), "AbstractGenerator should implement ISourceGenerator");

        // Test methods
        var initializeMethod = classType.GetMethod("Initialize", 
            BindingFlags.Public | BindingFlags.Instance);
        var executeMethod = classType.GetMethod("Execute", 
            BindingFlags.Public | BindingFlags.Instance);

        Assert.IsNotNull(initializeMethod, "AbstractGenerator should have Initialize method");
        Assert.IsNotNull(executeMethod, "AbstractGenerator should have Execute method");
    }

    #endregion

    #region Helper Methods

    private static bool IsValidCSharpIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;

        // Check if it's a valid C# identifier by trying to parse it
        var code = $"var {identifier} = 1;";
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var diagnostics = syntaxTree.GetDiagnostics();
        return !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
    }

    #endregion
}
