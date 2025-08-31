// -----------------------------------------------------------------------
// <copyright file="ExtensionsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for Extensions utility methods.
/// </summary>
[TestClass]
public class ExtensionsTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests IsNullableType extension method with actual type symbols.
    /// </summary>
    [TestMethod]
    public void Extensions_IsNullableType_WorksWithRealTypes()
    {
        // Create a minimal compilation to get real type symbols
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "TestAssembly",
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Test actual nullable type detection
        var intIsNullable = Extensions.IsNullableType(intType);
        var stringCanBeNull = Extensions.CanHaveNullValue(stringType);

        Assert.IsFalse(intIsNullable, "Int32 should not be nullable by default");
        Assert.IsTrue(stringCanBeNull, "String type can have null values");
    }

    /// <summary>
    /// Tests GetDataReadExpression method generates valid C# expressions.
    /// </summary>
    [TestMethod]
    public void Extensions_GetDataReadExpression_GeneratesValidCode()
    {
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "TestAssembly",
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Generate data read expressions
        var intExpression = Extensions.GetDataReadExpression(intType, "reader", "0");
        var stringExpression = Extensions.GetDataReadExpression(stringType, "reader", "1");

        // Verify expressions contain expected patterns
        Assert.IsTrue(intExpression.Contains("GetInt32"), "Int expression should use GetInt32 method");
        Assert.IsTrue(intExpression.Contains("reader"), "Expression should reference reader parameter");
        Assert.IsTrue(intExpression.Contains("0"), "Expression should use column index");

        Assert.IsTrue(stringExpression.Contains("GetString"), "String expression should use GetString method");
        Assert.IsTrue(stringExpression.Contains("IsDBNull"), "Nullable string should check for DBNull");
    }

    /// <summary>
    /// Tests GetParameterName extension method generates unique parameter names.
    /// </summary>
    [TestMethod]
    public void Extensions_GetParameterName_GeneratesUniqueNames()
    {
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "TestAssembly",
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Generate parameter names
        var param1 = Extensions.GetParameterName(intType, "userId");
        var param2 = Extensions.GetParameterName(intType, "userName");
        var param3 = Extensions.GetParameterName(intType, "userId"); // Same base name

        Assert.IsNotNull(param1);
        Assert.IsNotNull(param2);
        Assert.IsNotNull(param3);
        Assert.AreNotEqual(param1, param2, "Different base names should generate different parameter names");

        // Verify they are valid C# identifiers (remove @ prefix for validation)
        Assert.IsTrue(IsValidIdentifier(param1.TrimStart('@')), $"Generated parameter name '{param1}' should be valid");
        Assert.IsTrue(IsValidIdentifier(param2.TrimStart('@')), $"Generated parameter name '{param2}' should be valid");
        Assert.IsTrue(IsValidIdentifier(param3.TrimStart('@')), $"Generated parameter name '{param3}' should be valid");
    }

    private static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;

        return SyntaxFacts.IsValidIdentifier(identifier);
    }
}
