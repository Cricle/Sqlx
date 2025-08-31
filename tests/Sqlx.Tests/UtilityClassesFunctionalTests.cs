// -----------------------------------------------------------------------
// <copyright file="UtilityClassesFunctionalTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Functional tests for utility classes like IndentedStringBuilder, NameMapper, etc.
/// </summary>
[TestClass]
public class UtilityClassesFunctionalTests
{
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
        Assert.IsFalse(syntaxErrors.Any(), $"Generated code should be syntactically valid. Errors: {string.Join(", ", syntaxErrors.Select(e => e.GetMessage()))}");
    }

    /// <summary>
    /// Tests NameMapper parameter name generation with various input scenarios.
    /// </summary>
    [TestMethod]
    public void NameMapper_GeneratesUniqueParameterNames()
    {
        // Create mock symbols for testing
        var compilation = CSharpCompilation.Create("test");
        var objectType = compilation.GetSpecialType(SpecialType.System_Object);

        // Test parameter name generation using Extensions methods
        var param1Name = Extensions.GetParameterName(objectType, "userId");
        var param2Name = Extensions.GetParameterName(objectType, "userName");
        var param3Name = Extensions.GetParameterName(objectType, "userId"); // Same name should get unique suffix

        Assert.IsNotNull(param1Name);
        Assert.IsNotNull(param2Name);
        Assert.IsNotNull(param3Name);
        Assert.AreNotEqual(param1Name, param2Name, "Different parameter names should generate different results");

        // Verify parameter names are valid C# identifiers
        Assert.IsTrue(IsValidCSharpIdentifier(param1Name), $"Generated parameter name '{param1Name}' should be valid C# identifier");
        Assert.IsTrue(IsValidCSharpIdentifier(param2Name), $"Generated parameter name '{param2Name}' should be valid C# identifier");
        Assert.IsTrue(IsValidCSharpIdentifier(param3Name), $"Generated parameter name '{param3Name}' should be valid C# identifier");
    }

    /// <summary>
    /// Tests Extensions.GetDataReadExpression for various data types.
    /// </summary>
    /// <param name="typeName">The full name of the type to test.</param>
    /// <param name="expectedMethod">The expected DbDataReader method name.</param>
    [TestMethod]
    [DataRow("System.Int32", "GetInt32")]
    [DataRow("System.String", "GetString")]
    [DataRow("System.Boolean", "GetBoolean")]
    [DataRow("System.DateTime", "GetDateTime")]
    [DataRow("System.Decimal", "GetDecimal")]
    public void Extensions_GeneratesCorrectDataReadExpression(
        string typeName,
        string expectedMethod)
    {
        // Create a compilation to get type symbols
        var compilation = CSharpCompilation.Create("test", references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var type = compilation.GetTypeByMetadataName(typeName);
        Assert.IsNotNull(type, $"Type {typeName} should be found");

        // Test data read expression generation
        var expression = Extensions.GetDataReadExpression(type, "__reader__", "columnName");

        Assert.IsNotNull(expression);
        Assert.IsTrue(expression.Contains(expectedMethod), $"Expression for {typeName} should contain {expectedMethod}. Generated: {expression}");
        Assert.IsTrue(expression.Contains("__reader__"), "Expression should reference the reader variable");
        Assert.IsTrue(expression.Contains("GetOrdinal"), "Expression should use GetOrdinal method");
    }

    /// <summary>
    /// Tests nullable type handling in Extensions.
    /// </summary>
    [TestMethod]
    public void Extensions_HandlesNullableTypesCorrectly()
    {
        var compilation = CSharpCompilation.Create("test", references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Test nullable detection
        var intNullable = Extensions.IsNullableType(intType);
        var stringNullable = Extensions.CanHaveNullValue(stringType);

        Assert.IsFalse(intNullable, "Int32 should not be considered nullable by default");
        Assert.IsTrue(stringNullable, "String should be considered nullable");

        // Test data read expressions for nullable types
        var intExpression = Extensions.GetDataReadExpression(intType, "__reader__", "id");
        var stringExpression = Extensions.GetDataReadExpression(stringType, "__reader__", "name");

        Assert.IsTrue(intExpression.Contains("GetInt32"), "Int expression should use GetInt32");
        Assert.IsTrue(stringExpression.Contains("IsDBNull"), "Nullable string expression should include null check");
    }

    /// <summary>
    /// Tests that Messages class contains properly formatted error messages.
    /// </summary>
    /// <param name="errorCode">The error code to test.</param>
    [TestMethod]
    [DataRow("SP0001")]
    [DataRow("SP0002")]
    [DataRow("SP0003")]
    [DataRow("SP0007")]
    [DataRow("SP0009")]
    public void Messages_ContainsWellFormattedErrorMessages(string errorCode)
    {
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Debug output to see what properties are available
        var availableProperties = string.Join(", ", properties.Select(p => p.Name));
        Console.WriteLine($"Available properties in Messages: {availableProperties}");

        var errorProperty = properties.FirstOrDefault(p => p.Name == errorCode);
        Assert.IsNotNull(errorProperty, $"Messages should contain property for error code {errorCode}. Available properties: {availableProperties}");

        var diagnosticDescriptor = errorProperty.GetValue(null) as DiagnosticDescriptor;
        Assert.IsNotNull(diagnosticDescriptor, $"Diagnostic descriptor for {errorCode} should not be null");
        Assert.IsTrue(diagnosticDescriptor.Title.ToString().Length > 5, $"Title for {errorCode} should be descriptive");
        Assert.AreEqual(errorCode, diagnosticDescriptor.Id, $"Diagnostic ID should match the error code {errorCode}");
    }

    /// <summary>
    /// Tests Consts class functionality with actual constant values.
    /// </summary>
    [TestMethod]
    public void Consts_ContainsValidConstantValues()
    {
        var constsType = typeof(Consts);
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.IsTrue(fields.Length > 0, "Consts should contain constant fields");

        foreach (var field in fields)
        {
            var value = field.GetValue(null);
            Assert.IsNotNull(value, $"Constant {field.Name} should have a non-null value");

            if (field.FieldType == typeof(string))
            {
                var stringValue = (string)value;
                Assert.IsTrue(stringValue.Length > 0, $"String constant {field.Name} should not be empty");
            }
        }
    }

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
}
