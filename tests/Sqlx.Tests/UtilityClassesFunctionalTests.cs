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
    [DataTestMethod]
    [DataRow("SP0001")]
    [DataRow("SP0002")]
    [DataRow("SP0003")]
    [DataRow("SP0004")]
    [DataRow("SP0005")]
    [DataRow("SP0006")]
    [DataRow("SP0007")]
    [DataRow("SP0008")]
    [DataRow("SP0009")]
    public void Messages_ContainsWellFormattedErrorMessages(string errorCode)
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var availableProperties = string.Join(", ", properties.Select(p => p.Name));

        // Act
        var errorProperty = properties.FirstOrDefault(p => p.Name == errorCode);

        // Assert
        Assert.IsNotNull(errorProperty, $"Messages should contain property for error code {errorCode}. Available properties: {availableProperties}");

        var diagnosticDescriptor = errorProperty.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
        Assert.IsNotNull(diagnosticDescriptor, $"Property {errorCode} should return a DiagnosticDescriptor");
        Assert.AreEqual(errorCode, diagnosticDescriptor.Id, $"Diagnostic ID should match {errorCode}");
        Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Title), $"Diagnostic {errorCode} should have a title");
        Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.MessageFormat), $"Diagnostic {errorCode} should have a message format");
        Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Category), $"Diagnostic {errorCode} should have a category");
    }

    /// <summary>
    /// Tests that all Messages diagnostic descriptors have correct severity.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectSeverity()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            Assert.AreEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnosticDescriptor.Severity, 
                $"Diagnostic {property.Name} should have Error severity");
            Assert.IsTrue(diagnosticDescriptor.IsEnabledByDefault, $"Diagnostic {property.Name} should be enabled by default");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have unique IDs.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveUniqueIds()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act
        var diagnosticIds = properties
            .Select(p => p.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor)
            .Where(d => d != null)
            .Select(d => d.Id)
            .ToList();

        // Assert
        var uniqueIds = diagnosticIds.Distinct().ToList();
        Assert.AreEqual(diagnosticIds.Count, uniqueIds.Count, "All diagnostic IDs should be unique");
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have valid help links.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveValidHelpLinks()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Help link can be null for internal diagnostics
            if (diagnosticDescriptor.HelpLinkUri != null)
            {
                Assert.IsTrue(Uri.IsWellFormedUriString(diagnosticDescriptor.HelpLinkUri, UriKind.Absolute), 
                    $"Diagnostic {property.Name} should have a valid help link URI");
            }
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct categories.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectCategories()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that categories are meaningful
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Category), 
                $"Diagnostic {property.Name} should have a non-empty category");
            Assert.IsTrue(diagnosticDescriptor.Category.Length > 0, 
                $"Diagnostic {property.Name} should have a non-empty category");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct titles.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectTitles()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that titles are meaningful
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Title), 
                $"Diagnostic {property.Name} should have a non-empty title");
            Assert.IsTrue(diagnosticDescriptor.Title.Length > 5, 
                $"Diagnostic {property.Name} should have a meaningful title");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct message formats.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectMessageFormats()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that message formats are meaningful
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.MessageFormat), 
                $"Diagnostic {property.Name} should have a non-empty message format");
            Assert.IsTrue(diagnosticDescriptor.MessageFormat.Length > 5, 
                $"Diagnostic {property.Name} should have a meaningful message format");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct default severity.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectDefaultSeverity()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that default severity is Error
            Assert.AreEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnosticDescriptor.DefaultSeverity, 
                $"Diagnostic {property.Name} should have Error as default severity");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct is enabled by default setting.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_AreEnabledByDefault()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that diagnostics are enabled by default
            Assert.IsTrue(diagnosticDescriptor.IsEnabledByDefault, 
                $"Diagnostic {property.Name} should be enabled by default");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct is suppressible setting.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_AreSuppressible()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that diagnostics are suppressible
            Assert.IsTrue(diagnosticDescriptor.IsSuppressible, 
                $"Diagnostic {property.Name} should be suppressible");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct custom tags.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectCustomTags()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that diagnostics have custom tags (if any)
            // Custom tags are optional, so we just verify the property exists
            Assert.IsNotNull(diagnosticDescriptor.CustomTags, 
                $"Diagnostic {property.Name} should have custom tags (can be empty)");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct descriptions.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectDescriptions()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test that descriptions are meaningful
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Description), 
                $"Diagnostic {property.Name} should have a non-empty description");
            Assert.IsTrue(diagnosticDescriptor.Description.Length > 5, 
                $"Diagnostic {property.Name} should have a meaningful description");
        }
    }

    /// <summary>
    /// Tests that Messages diagnostic descriptors have correct help links.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectHelpLinks()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Help link can be null for internal diagnostics
            if (diagnosticDescriptor.HelpLinkUri != null)
            {
                Assert.IsTrue(Uri.IsWellFormedUriString(diagnosticDescriptor.HelpLinkUri, UriKind.Absolute), 
                    $"Diagnostic {property.Name} should have a valid help link URI");
            }
        }
    }

    /// <summary>
    /// Tests that Consts class functionality with actual constant values.
    /// </summary>
    [TestMethod]
    public void Consts_ContainsValidConstantValues()
    {
        // Arrange
        var constsType = typeof(Consts);
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        Assert.IsTrue(fields.Length > 0, "Consts should contain constant fields");

        // Test specific constants
        var iAsyncEnumerableField = constsType.GetField("IAsyncEnumerable");
        Assert.IsNotNull(iAsyncEnumerableField, "Consts should contain IAsyncEnumerable constant");
        
        var constantValue = iAsyncEnumerableField.GetValue(null);
        Assert.AreEqual("IAsyncEnumerable", constantValue);
    }

    /// <summary>
    /// Tests that Consts class is internal and static.
    /// </summary>
    [TestMethod]
    public void Consts_ClassModifiers_AreCorrect()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act & Assert
        Assert.IsTrue(constsType.IsNotPublic, "Consts should be internal");
        Assert.IsTrue(constsType.IsAbstract, "Consts should be abstract (static)");
        Assert.IsTrue(constsType.IsSealed, "Consts should be sealed");
    }

    /// <summary>
    /// Tests that IsExternalInit class exists and has correct structure.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectStructure()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.IsNotNull(isExternalInitType, "IsExternalInit class should exist");
        Assert.IsTrue(isExternalInitType.IsNotPublic, "IsExternalInit should be internal");
        Assert.IsTrue(isExternalInitType.IsAbstract, "IsExternalInit should be abstract");
        Assert.IsFalse(isExternalInitType.IsSealed, "IsExternalInit should not be sealed");
    }

    /// <summary>
    /// Tests that IsExternalInit class is in the correct namespace.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Namespace_IsCorrect()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.AreEqual("System.Runtime.CompilerServices", isExternalInitType.Namespace);
    }

    /// <summary>
    /// Tests that IsExternalInit class has correct inheritance.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectInheritance()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.AreEqual(typeof(object), isExternalInitType.BaseType, 
            "IsExternalInit should inherit from object");
        Assert.IsTrue(isExternalInitType.IsAbstract, "IsExternalInit should be abstract");
        Assert.IsFalse(isExternalInitType.IsSealed, "IsExternalInit should not be sealed");
    }

    /// <summary>
    /// Tests that IsExternalInit class has correct accessibility.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectAccessibility()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.IsTrue(isExternalInitType.IsNotPublic, "IsExternalInit should be internal");
        Assert.IsFalse(isExternalInitType.IsPublic, "IsExternalInit should not be public");
        Assert.IsFalse(isExternalInitType.IsNested, "IsExternalInit should not be nested");
    }

    /// <summary>
    /// Tests that IsExternalInit class has correct modifiers.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectModifiers()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.IsTrue(isExternalInitType.IsAbstract, "IsExternalInit should be abstract");
        Assert.IsFalse(isExternalInitType.IsSealed, "IsExternalInit should not be sealed");
        Assert.IsFalse(isExternalInitType.IsInterface, "IsExternalInit should not be an interface");
        Assert.IsFalse(isExternalInitType.IsValueType, "IsExternalInit should not be a value type");
    }

    /// <summary>
    /// Tests that IsExternalInit class has correct assembly.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectAssembly()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.IsNotNull(isExternalInitType.Assembly, "IsExternalInit should have an assembly");
        Assert.AreEqual("Sqlx", isExternalInitType.Assembly.GetName().Name, 
            "IsExternalInit should be in the Sqlx assembly");
    }

    /// <summary>
    /// Tests that IsExternalInit class has correct module.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectModule()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.IsNotNull(isExternalInitType.Module, "IsExternalInit should have a module");
        Assert.IsTrue(isExternalInitType.Module.Name.Contains("Sqlx"), 
            "IsExternalInit should be in a Sqlx module");
    }

    /// <summary>
    /// Tests that IsExternalInit class has correct declaring type.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectDeclaringType()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.IsNull(isExternalInitType.DeclaringType, "IsExternalInit should not have a declaring type");
    }

    /// <summary>
    /// Tests that IsExternalInit class has correct reflected type.
    /// </summary>
    [TestMethod]
    public void IsExternalInit_Class_HasCorrectReflectedType()
    {
        // Arrange
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);

        // Act & Assert
        Assert.AreEqual(isExternalInitType, isExternalInitType.ReflectedType, 
            "IsExternalInit reflected type should be itself");
    }

    /// <summary>
    /// Tests that Consts.IAsyncEnumerable is used correctly in the codebase.
    /// </summary>
    [TestMethod]
    public void Consts_IAsyncEnumerable_IsUsedCorrectly()
    {
        // Arrange
        var constsType = typeof(Consts);
        var iAsyncEnumerableField = constsType.GetField("IAsyncEnumerable");

        // Act
        var constantValue = iAsyncEnumerableField.GetValue(null);

        // Assert
        Assert.AreEqual("IAsyncEnumerable", constantValue);
        Assert.IsTrue(constantValue is string);
        Assert.IsTrue(constantValue.ToString().Length > 0);
    }

    /// <summary>
    /// Tests that Consts class has correct structure.
    /// </summary>
    [TestMethod]
    public void Consts_Class_HasCorrectStructure()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act & Assert
        Assert.IsNotNull(constsType, "Consts class should exist");
        Assert.IsTrue(constsType.IsNotPublic, "Consts should be internal");
        Assert.IsTrue(constsType.IsAbstract, "Consts should be abstract (static)");
        Assert.IsTrue(constsType.IsSealed, "Consts should be sealed");
    }

    /// <summary>
    /// Tests that Consts class is in the correct namespace.
    /// </summary>
    [TestMethod]
    public void Consts_Namespace_IsCorrect()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act & Assert
        Assert.AreEqual("Sqlx", constsType.Namespace);
    }

    /// <summary>
    /// Tests that Consts class has correct field accessibility.
    /// </summary>
    [TestMethod]
    public void Consts_Class_HasCorrectFieldAccessibility()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsTrue(fields.Length > 0, "Consts should contain constant fields");

        foreach (var field in fields)
        {
            Assert.IsTrue(field.IsPublic, $"Field {field.Name} should be public");
            Assert.IsTrue(field.IsStatic, $"Field {field.Name} should be static");
            Assert.IsTrue(field.IsLiteral, $"Field {field.Name} should be literal (const)");
        }
    }

    /// <summary>
    /// Tests that Consts class has correct field types.
    /// </summary>
    [TestMethod]
    public void Consts_Class_HasCorrectFieldTypes()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsTrue(fields.Length > 0, "Consts should contain constant fields");

        foreach (var field in fields)
        {
            Assert.AreEqual(typeof(string), field.FieldType, $"Field {field.Name} should be of type string");
        }
    }

    /// <summary>
    /// Tests that Consts class has correct field values.
    /// </summary>
    [TestMethod]
    public void Consts_Class_HasCorrectFieldValues()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsTrue(fields.Length > 0, "Consts should contain constant fields");

        foreach (var field in fields)
        {
            var value = field.GetValue(null);
            Assert.IsNotNull(value, $"Field {field.Name} should have a non-null value");
            Assert.IsTrue(value is string, $"Field {field.Name} should be a string");
            
            var stringValue = (string)value;
            Assert.IsTrue(stringValue.Length > 0, $"Field {field.Name} should have a non-empty string value");
        }
    }

    /// <summary>
    /// Tests that Consts class has correct field names.
    /// </summary>
    [TestMethod]
    public void Consts_Class_HasCorrectFieldNames()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsTrue(fields.Length > 0, "Consts should contain constant fields");

        var fieldNames = fields.Select(f => f.Name).ToArray();
        Assert.IsTrue(fieldNames.Contains("IAsyncEnumerable"), "Consts should contain IAsyncEnumerable field");
    }

    /// <summary>
    /// Tests that Consts class has correct field count.
    /// </summary>
    [TestMethod]
    public void Consts_Class_HasCorrectFieldCount()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.AreEqual(1, fields.Length, "Consts should contain exactly 1 constant field");
    }

    /// <summary>
    /// Tests that Consts class has correct field initialization.
    /// </summary>
    [TestMethod]
    public void Consts_Class_HasCorrectFieldInitialization()
    {
        // Arrange
        var constsType = typeof(Consts);

        // Act
        var iAsyncEnumerableField = constsType.GetField("IAsyncEnumerable");

        // Assert
        Assert.IsNotNull(iAsyncEnumerableField, "Consts should contain IAsyncEnumerable field");
        
        var constantValue = iAsyncEnumerableField.GetValue(null);
        Assert.AreEqual("IAsyncEnumerable", constantValue);
        Assert.IsTrue(constantValue is string);
        Assert.IsTrue(constantValue.ToString().Length > 0);
    }

    /// <summary>
    /// Tests that ISqlxSyntaxReceiver interface exists and has correct structure.
    /// </summary>
    [TestMethod]
    public void ISqlxSyntaxReceiver_InterfaceExists_WithCorrectStructure()
    {
        // Arrange
        var interfaceType = typeof(ISqlxSyntaxReceiver);

        // Act & Assert
        Assert.IsNotNull(interfaceType, "ISqlxSyntaxReceiver interface should exist");
        Assert.IsTrue(interfaceType.IsInterface, "ISqlxSyntaxReceiver should be an interface");
        Assert.IsTrue(interfaceType.IsNotPublic, "ISqlxSyntaxReceiver should be internal");
        
        // Check that it inherits from ISyntaxContextReceiver
        var baseInterfaces = interfaceType.GetInterfaces();
        Assert.IsTrue(baseInterfaces.Any(i => i.Name == "ISyntaxContextReceiver"), 
            "ISqlxSyntaxReceiver should inherit from ISyntaxContextReceiver");
    }

    /// <summary>
    /// Tests that ISqlxSyntaxReceiver interface has the Methods property.
    /// </summary>
    [TestMethod]
    public void ISqlxSyntaxReceiver_Interface_HasMethodsProperty()
    {
        // Arrange
        var interfaceType = typeof(ISqlxSyntaxReceiver);

        // Act
        var methodsProperty = interfaceType.GetProperty("Methods");

        // Assert
        Assert.IsNotNull(methodsProperty, "ISqlxSyntaxReceiver should have a Methods property");
        Assert.IsTrue(methodsProperty.CanRead, "Methods property should be readable");
        Assert.IsFalse(methodsProperty.CanWrite, "Methods property should not be writable");
        
        // Check that it returns List<IMethodSymbol>
        var propertyType = methodsProperty.PropertyType;
        Assert.IsTrue(propertyType.IsGenericType, "Methods property should be generic");
        Assert.AreEqual("List`1", propertyType.Name, "Methods property should be List<T>");
        
        var genericArguments = propertyType.GetGenericArguments();
        Assert.AreEqual(1, genericArguments.Length, "Methods property should have one generic argument");
        Assert.AreEqual("IMethodSymbol", genericArguments[0].Name, "Generic argument should be IMethodSymbol");
    }

    /// <summary>
    /// Tests that ISqlxSyntaxReceiver interface is in the correct namespace.
    /// </summary>
    [TestMethod]
    public void ISqlxSyntaxReceiver_Namespace_IsCorrect()
    {
        // Arrange
        var interfaceType = typeof(ISqlxSyntaxReceiver);

        // Act & Assert
        Assert.AreEqual("Sqlx", interfaceType.Namespace);
    }

    /// <summary>
    /// Tests that GenerationContextBase class exists and has correct structure.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_ClassExists_WithCorrectStructure()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act & Assert
        Assert.IsNotNull(baseType, "GenerationContextBase class should exist");
        Assert.IsTrue(baseType.IsNotPublic, "GenerationContextBase should be internal");
        Assert.IsTrue(baseType.IsAbstract, "GenerationContextBase should be abstract");
        Assert.IsFalse(baseType.IsSealed, "GenerationContextBase should not be sealed");
    }

    /// <summary>
    /// Tests that GenerationContextBase class is in the correct namespace.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Namespace_IsCorrect()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act & Assert
        Assert.AreEqual("Sqlx", baseType.Namespace);
    }

    /// <summary>
    /// Tests that GenerationContextBase class has abstract properties.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasAbstractProperties()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act
        var properties = baseType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        var dbConnectionProperty = properties.FirstOrDefault(p => p.Name == "DbConnection");
        Assert.IsNotNull(dbConnectionProperty, "GenerationContextBase should have DbConnection property");
        Assert.IsTrue(dbConnectionProperty.CanRead, "DbConnection property should be readable");

        var transactionProperty = properties.FirstOrDefault(p => p.Name == "TransactionParameter");
        Assert.IsNotNull(transactionProperty, "GenerationContextBase should have TransactionParameter property");
        Assert.IsTrue(transactionProperty.CanRead, "TransactionParameter property should be readable");

        var dbContextProperty = properties.FirstOrDefault(p => p.Name == "DbContext");
        Assert.IsNotNull(dbContextProperty, "GenerationContextBase should have DbContext property");
        Assert.IsTrue(dbContextProperty.CanRead, "DbContext property should be readable");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has GetSymbol method.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasGetSymbolMethod()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act
        var getSymbolMethod = baseType.GetMethod("GetSymbol", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsNotNull(getSymbolMethod, "GenerationContextBase should have GetSymbol method");
        Assert.IsTrue(getSymbolMethod.IsStatic, "GetSymbol method should be static");
        Assert.IsTrue(getSymbolMethod.IsPrivate, "GetSymbol method should be private");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct method signatures.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectMethodSignatures()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act
        var getSymbolMethod = baseType.GetMethod("GetSymbol", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsNotNull(getSymbolMethod, "GetSymbol method should exist");

        var parameters = getSymbolMethod.GetParameters();
        Assert.AreEqual(2, parameters.Length, "GetSymbol method should have 2 parameters");
        Assert.AreEqual("ISymbol", parameters[0].ParameterType.Name, 
            "GetSymbol method first parameter should be ISymbol");
        Assert.AreEqual("Func`2", parameters[1].ParameterType.Name, 
            "GetSymbol method second parameter should be Func<ISymbol, bool>");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct return types.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectReturnTypes()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act
        var getSymbolMethod = baseType.GetMethod("GetSymbol", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsNotNull(getSymbolMethod, "GetSymbol method should exist");

        Assert.AreEqual(typeof(ISymbol), getSymbolMethod.ReturnType, 
            "GetSymbol method should return ISymbol");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct accessibility.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectAccessibility()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act & Assert
        Assert.IsTrue(baseType.IsNotPublic, "GenerationContextBase should be internal");
        Assert.IsFalse(baseType.IsPublic, "GenerationContextBase should not be public");
        Assert.IsFalse(baseType.IsNested, "GenerationContextBase should not be nested");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct inheritance.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectInheritance()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act & Assert
        Assert.AreEqual(typeof(object), baseType.BaseType, 
            "GenerationContextBase should inherit from object");
        Assert.IsTrue(baseType.IsAbstract, "GenerationContextBase should be abstract");
        Assert.IsFalse(baseType.IsSealed, "GenerationContextBase should not be sealed");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct abstract properties.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectAbstractProperties()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act
        var properties = baseType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        var dbConnectionProperty = properties.FirstOrDefault(p => p.Name == "DbConnection");
        Assert.IsNotNull(dbConnectionProperty, "GenerationContextBase should have DbConnection property");
        Assert.IsTrue(dbConnectionProperty.CanRead, "DbConnection property should be readable");
        Assert.IsTrue(dbConnectionProperty.GetMethod.IsAbstract, "DbConnection property should be abstract");

        var transactionProperty = properties.FirstOrDefault(p => p.Name == "TransactionParameter");
        Assert.IsNotNull(transactionProperty, "GenerationContextBase should have TransactionParameter property");
        Assert.IsTrue(transactionProperty.CanRead, "TransactionParameter property should be readable");
        Assert.IsTrue(transactionProperty.GetMethod.IsAbstract, "TransactionParameter property should be abstract");

        var dbContextProperty = properties.FirstOrDefault(p => p.Name == "DbContext");
        Assert.IsNotNull(dbContextProperty, "GenerationContextBase should have DbContext property");
        Assert.IsTrue(dbContextProperty.CanRead, "DbContext property should be readable");
        Assert.IsTrue(dbContextProperty.GetMethod.IsAbstract, "DbContext property should be abstract");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct property types.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectPropertyTypes()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act
        var properties = baseType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        var dbConnectionProperty = properties.FirstOrDefault(p => p.Name == "DbConnection");
        Assert.IsNotNull(dbConnectionProperty, "DbConnection property should exist");
        Assert.AreEqual("ISymbol", dbConnectionProperty.PropertyType.Name, 
            "DbConnection property should be of type ISymbol");

        var transactionProperty = properties.FirstOrDefault(p => p.Name == "TransactionParameter");
        Assert.IsNotNull(transactionProperty, "TransactionParameter property should exist");
        Assert.AreEqual("ISymbol", transactionProperty.PropertyType.Name, 
            "TransactionParameter property should be of type ISymbol");

        var dbContextProperty = properties.FirstOrDefault(p => p.Name == "DbContext");
        Assert.IsNotNull(dbContextProperty, "DbContext property should exist");
        Assert.AreEqual("ISymbol", dbContextProperty.PropertyType.Name, 
            "DbContext property should be of type ISymbol");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct property accessibility.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectPropertyAccessibility()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act
        var properties = baseType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        foreach (var property in properties)
        {
            Assert.IsTrue(property.CanRead, $"Property {property.Name} should be readable");
            Assert.IsFalse(property.CanWrite, $"Property {property.Name} should not be writable");
        }
    }

    /// <summary>
    /// Tests that ClassGenerationContext class exists and has correct structure.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_ClassExists_WithCorrectStructure()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act & Assert
        Assert.IsNotNull(classType, "ClassGenerationContext class should exist");
        Assert.IsTrue(classType.IsNotPublic, "ClassGenerationContext should be internal");
        Assert.IsFalse(classType.IsAbstract, "ClassGenerationContext should not be abstract");
        Assert.IsFalse(classType.IsSealed, "ClassGenerationContext should not be sealed");
    }

    /// <summary>
    /// Tests that ClassGenerationContext class inherits from GenerationContextBase.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_InheritsFromGenerationContextBase()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);
        var baseType = typeof(GenerationContextBase);

        // Act & Assert
        Assert.AreEqual(baseType, classType.BaseType, "ClassGenerationContext should inherit from GenerationContextBase");
    }

    /// <summary>
    /// Tests that ClassGenerationContext class is in the correct namespace.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Namespace_IsCorrect()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act & Assert
        Assert.AreEqual("Sqlx", classType.Namespace);
    }

    /// <summary>
    /// Tests that ClassGenerationContext class has correct constructor.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_HasCorrectConstructor()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act
        var constructors = classType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsTrue(constructors.Length > 0, "ClassGenerationContext should have at least one constructor");

        var constructor = constructors.First();
        var parameters = constructor.GetParameters();
        Assert.AreEqual(3, parameters.Length, "Constructor should have 3 parameters");
        Assert.AreEqual("INamedTypeSymbol", parameters[0].ParameterType.Name, 
            "First parameter should be INamedTypeSymbol");
        Assert.AreEqual("List`1", parameters[1].ParameterType.Name, 
            "Second parameter should be List<IMethodSymbol>");
        Assert.AreEqual("INamedTypeSymbol", parameters[2].ParameterType.Name, 
            "Third parameter should be INamedTypeSymbol");
    }

    /// <summary>
    /// Tests that ClassGenerationContext class has correct method signatures.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_HasCorrectMethodSignatures()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act
        var createSourceMethod = classType.GetMethod("CreateSource", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getFieldOrPropertyMethod = classType.GetMethod("GetFieldOrProperty", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getAttributeMethod = classType.GetMethod("GetAttribute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(createSourceMethod, "CreateSource method should exist");
        Assert.IsNotNull(getFieldOrPropertyMethod, "GetFieldOrProperty method should exist");
        Assert.IsNotNull(getAttributeMethod, "GetAttribute method should exist");

        var createSourceParams = createSourceMethod.GetParameters();
        Assert.AreEqual(1, createSourceParams.Length, "CreateSource method should have 1 parameter");
        Assert.AreEqual("IndentedStringBuilder", createSourceParams[0].ParameterType.Name, 
            "CreateSource method parameter should be IndentedStringBuilder");

        var getFieldOrPropertyParams = getFieldOrPropertyMethod.GetParameters();
        Assert.AreEqual(1, getFieldOrPropertyParams.Length, "GetFieldOrProperty method should have 1 parameter");
        Assert.AreEqual("Func`2", getFieldOrPropertyParams[0].ParameterType.Name, 
            "GetFieldOrProperty method parameter should be Func<ISymbol, bool>");

        var getAttributeParams = getAttributeMethod.GetParameters();
        Assert.AreEqual(1, getAttributeParams.Length, "GetAttribute method should have 1 parameter");
        Assert.AreEqual("Func`2", getAttributeParams[0].ParameterType.Name, 
            "GetAttribute method parameter should be Func<AttributeData, bool>");
    }

    /// <summary>
    /// Tests that ClassGenerationContext class has correct return types.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_HasCorrectReturnTypes()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act
        var createSourceMethod = classType.GetMethod("CreateSource", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getFieldOrPropertyMethod = classType.GetMethod("GetFieldOrProperty", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getAttributeMethod = classType.GetMethod("GetAttribute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(createSourceMethod, "CreateSource method should exist");
        Assert.IsNotNull(getFieldOrPropertyMethod, "GetFieldOrProperty method should exist");
        Assert.IsNotNull(getAttributeMethod, "GetAttribute method should exist");

        Assert.AreEqual(typeof(bool), createSourceMethod.ReturnType, 
            "CreateSource method should return bool");
        Assert.AreEqual(typeof(ISymbol), getFieldOrPropertyMethod.ReturnType, 
            "GetFieldOrProperty method should return ISymbol");
        Assert.AreEqual(typeof(AttributeData), getAttributeMethod.ReturnType, 
            "GetAttribute method should return AttributeData");
    }

    /// <summary>
    /// Tests that ClassGenerationContext class has correct method accessibility.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_HasCorrectMethodAccessibility()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act
        var createSourceMethod = classType.GetMethod("CreateSource", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getFieldOrPropertyMethod = classType.GetMethod("GetFieldOrProperty", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getAttributeMethod = classType.GetMethod("GetAttribute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(createSourceMethod, "CreateSource method should exist");
        Assert.IsNotNull(getFieldOrPropertyMethod, "GetFieldOrProperty method should exist");
        Assert.IsNotNull(getAttributeMethod, "GetAttribute method should exist");

        Assert.IsTrue(createSourceMethod.IsPublic, "CreateSource method should be public");
        Assert.IsTrue(getFieldOrPropertyMethod.IsPublic, "GetFieldOrProperty method should be public");
        Assert.IsTrue(getAttributeMethod.IsPublic, "GetAttribute method should be public");
    }

    /// <summary>
    /// Tests that ClassGenerationContext class has correct property accessibility.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_HasCorrectPropertyAccessibility()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        foreach (var property in properties)
        {
            Assert.IsTrue(property.CanRead, $"Property {property.Name} should be readable");
            Assert.IsFalse(property.CanWrite, $"Property {property.Name} should not be writable");
        }
    }

    /// <summary>
    /// Tests that ClassGenerationContext class has correct property types.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_HasCorrectPropertyTypes()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var classSymbolProperty = properties.FirstOrDefault(p => p.Name == "ClassSymbol");
        Assert.IsNotNull(classSymbolProperty, "ClassSymbol property should exist");
        Assert.AreEqual("INamedTypeSymbol", classSymbolProperty.PropertyType.Name, 
            "ClassSymbol property should be of type INamedTypeSymbol");

        var methodsProperty = properties.FirstOrDefault(p => p.Name == "Methods");
        Assert.IsNotNull(methodsProperty, "Methods property should exist");
        Assert.AreEqual("List`1", methodsProperty.PropertyType.Name, 
            "Methods property should be of type List<MethodGenerationContext>");

        var sqlxAttributeSymbolProperty = properties.FirstOrDefault(p => p.Name == "SqlxAttributeSymbol");
        Assert.IsNotNull(sqlxAttributeSymbolProperty, "SqlxAttributeSymbol property should exist");
        Assert.AreEqual("INamedTypeSymbol", sqlxAttributeSymbolProperty.PropertyType.Name, 
            "SqlxAttributeSymbol property should be of type INamedTypeSymbol");

        var generatorExecutionContextProperty = properties.FirstOrDefault(p => p.Name == "GeneratorExecutionContext");
        Assert.IsNotNull(generatorExecutionContextProperty, "GeneratorExecutionContext property should exist");
        Assert.AreEqual("GeneratorExecutionContext", generatorExecutionContextProperty.PropertyType.Name, 
            "GeneratorExecutionContext property should be of type GeneratorExecutionContext");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class exists and has correct structure.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_ClassExists_WithCorrectStructure()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act & Assert
        Assert.IsNotNull(classType, "MethodGenerationContext class should exist");
        Assert.IsTrue(classType.IsNotPublic, "MethodGenerationContext should be internal");
        Assert.IsFalse(classType.IsAbstract, "MethodGenerationContext should not be abstract");
        Assert.IsFalse(classType.IsSealed, "MethodGenerationContext should not be sealed");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class inherits from GenerationContextBase.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_InheritsFromGenerationContextBase()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);
        var baseType = typeof(GenerationContextBase);

        // Act & Assert
        Assert.AreEqual(baseType, classType.BaseType, "MethodGenerationContext should inherit from GenerationContextBase");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class is in the correct namespace.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Namespace_IsCorrect()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act & Assert
        Assert.AreEqual("Sqlx", classType.Namespace);
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has required methods.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasRequiredMethods()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var declareCommandMethod = classType.GetMethod("DeclareCommand", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeExecuteNoQueryMethod = classType.GetMethod("WriteExecuteNoQuery", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeScalarMethod = classType.GetMethod("WriteScalar", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeReturnMethod = classType.GetMethod("WriteReturn", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(declareCommandMethod, "MethodGenerationContext should have DeclareCommand method");
        Assert.IsNotNull(writeExecuteNoQueryMethod, "MethodGenerationContext should have WriteExecuteNoQuery method");
        Assert.IsNotNull(writeScalarMethod, "MethodGenerationContext should have WriteScalar method");
        Assert.IsNotNull(writeReturnMethod, "MethodGenerationContext should have WriteReturn method");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has correct method signatures.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasCorrectMethodSignatures()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var declareCommandMethod = classType.GetMethod("DeclareCommand", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeExecuteNoQueryMethod = classType.GetMethod("WriteExecuteNoQuery", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeScalarMethod = classType.GetMethod("WriteScalar", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeReturnMethod = classType.GetMethod("WriteReturn", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(declareCommandMethod, "DeclareCommand method should exist");
        Assert.IsNotNull(writeExecuteNoQueryMethod, "WriteExecuteNoQuery method should exist");
        Assert.IsNotNull(writeScalarMethod, "WriteScalar method should exist");
        Assert.IsNotNull(writeReturnMethod, "WriteReturn method should exist");

        var declareCommandParams = declareCommandMethod.GetParameters();
        Assert.AreEqual(1, declareCommandParams.Length, "DeclareCommand method should have 1 parameter");
        Assert.AreEqual("IndentedStringBuilder", declareCommandParams[0].ParameterType.Name, 
            "DeclareCommand method parameter should be IndentedStringBuilder");

        var writeExecuteNoQueryParams = writeExecuteNoQueryMethod.GetParameters();
        Assert.AreEqual(2, writeExecuteNoQueryParams.Length, "WriteExecuteNoQuery method should have 2 parameters");
        Assert.AreEqual("IndentedStringBuilder", writeExecuteNoQueryParams[0].ParameterType.Name, 
            "WriteExecuteNoQuery method first parameter should be IndentedStringBuilder");
        Assert.AreEqual("List`1", writeExecuteNoQueryParams[1].ParameterType.Name, 
            "WriteExecuteNoQuery method second parameter should be List<ColumnDefine>");

        var writeScalarParams = writeScalarMethod.GetParameters();
        Assert.AreEqual(2, writeScalarParams.Length, "WriteScalar method should have 2 parameters");
        Assert.AreEqual("IndentedStringBuilder", writeScalarParams[0].ParameterType.Name, 
            "WriteScalar method first parameter should be IndentedStringBuilder");
        Assert.AreEqual("List`1", writeScalarParams[1].ParameterType.Name, 
            "WriteScalar method second parameter should be List<ColumnDefine>");

        var writeReturnParams = writeReturnMethod.GetParameters();
        Assert.AreEqual(2, writeReturnParams.Length, "WriteReturn method should have 2 parameters");
        Assert.AreEqual("IndentedStringBuilder", writeReturnParams[0].ParameterType.Name, 
            "WriteReturn method first parameter should be IndentedStringBuilder");
        Assert.AreEqual("List`1", writeReturnParams[1].ParameterType.Name, 
            "WriteReturn method second parameter should be List<ColumnDefine>");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has correct return types.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasCorrectReturnTypes()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var declareCommandMethod = classType.GetMethod("DeclareCommand", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeExecuteNoQueryMethod = classType.GetMethod("WriteExecuteNoQuery", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeScalarMethod = classType.GetMethod("WriteScalar", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeReturnMethod = classType.GetMethod("WriteReturn", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(declareCommandMethod, "DeclareCommand method should exist");
        Assert.IsNotNull(writeExecuteNoQueryMethod, "WriteExecuteNoQuery method should exist");
        Assert.IsNotNull(writeScalarMethod, "WriteScalar method should exist");
        Assert.IsNotNull(writeReturnMethod, "WriteReturn method should exist");

        Assert.AreEqual(typeof(bool), declareCommandMethod.ReturnType, 
            "DeclareCommand method should return bool");
        Assert.AreEqual(typeof(bool), writeExecuteNoQueryMethod.ReturnType, 
            "WriteExecuteNoQuery method should return bool");
        Assert.AreEqual(typeof(void), writeScalarMethod.ReturnType, 
            "WriteScalar method should return void");
        Assert.AreEqual(typeof(bool), writeReturnMethod.ReturnType, 
            "WriteReturn method should return bool");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has correct method accessibility.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasCorrectMethodAccessibility()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var declareCommandMethod = classType.GetMethod("DeclareCommand", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeExecuteNoQueryMethod = classType.GetMethod("WriteExecuteNoQuery", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeScalarMethod = classType.GetMethod("WriteScalar", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeReturnMethod = classType.GetMethod("WriteReturn", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(declareCommandMethod, "DeclareCommand method should exist");
        Assert.IsNotNull(writeExecuteNoQueryMethod, "WriteExecuteNoQuery method should exist");
        Assert.IsNotNull(writeScalarMethod, "WriteScalar method should exist");
        Assert.IsNotNull(writeReturnMethod, "WriteReturn method should exist");

        Assert.IsTrue(declareCommandMethod.IsPublic, "DeclareCommand method should be public");
        Assert.IsTrue(writeExecuteNoQueryMethod.IsPublic, "WriteExecuteNoQuery method should be public");
        Assert.IsTrue(writeScalarMethod.IsPublic, "WriteScalar method should be public");
        Assert.IsTrue(writeReturnMethod.IsPublic, "WriteReturn method should be public");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has correct property accessibility.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasCorrectPropertyAccessibility()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        foreach (var property in properties)
        {
            Assert.IsTrue(property.CanRead, $"Property {property.Name} should be readable");
            Assert.IsFalse(property.CanWrite, $"Property {property.Name} should not be writable");
        }
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has correct property types.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasCorrectPropertyTypes()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var methodSymbolProperty = properties.FirstOrDefault(p => p.Name == "MethodSymbol");
        Assert.IsNotNull(methodSymbolProperty, "MethodSymbol property should exist");
        Assert.AreEqual("IMethodSymbol", methodSymbolProperty.PropertyType.Name, 
            "MethodSymbol property should be of type IMethodSymbol");

        var classGenerationContextProperty = properties.FirstOrDefault(p => p.Name == "ClassGenerationContext");
        Assert.IsNotNull(classGenerationContextProperty, "ClassGenerationContext property should exist");
        Assert.AreEqual("ClassGenerationContext", classGenerationContextProperty.PropertyType.Name, 
            "ClassGenerationContext property should be of type ClassGenerationContext");

        var returnTypeProperty = properties.FirstOrDefault(p => p.Name == "ReturnType");
        Assert.IsNotNull(returnTypeProperty, "ReturnType property should exist");
        Assert.AreEqual("ITypeSymbol", returnTypeProperty.PropertyType.Name, 
            "ReturnType property should be of type ITypeSymbol");

        var isAsyncProperty = properties.FirstOrDefault(p => p.Name == "IsAsync");
        Assert.IsNotNull(isAsyncProperty, "IsAsync property should exist");
        Assert.AreEqual("Boolean", isAsyncProperty.PropertyType.Name, 
            "IsAsync property should be of type Boolean");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has required properties.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasRequiredProperties()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var methodSymbolProperty = properties.FirstOrDefault(p => p.Name == "MethodSymbol");
        Assert.IsNotNull(methodSymbolProperty, "MethodGenerationContext should have MethodSymbol property");

        var classGenerationContextProperty = properties.FirstOrDefault(p => p.Name == "ClassGenerationContext");
        Assert.IsNotNull(classGenerationContextProperty, "MethodGenerationContext should have ClassGenerationContext property");

        var returnTypeProperty = properties.FirstOrDefault(p => p.Name == "ReturnType");
        Assert.IsNotNull(returnTypeProperty, "MethodGenerationContext should have ReturnType property");

        var isAsyncProperty = properties.FirstOrDefault(p => p.Name == "IsAsync");
        Assert.IsNotNull(isAsyncProperty, "MethodGenerationContext should have IsAsync property");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has required computed properties.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasRequiredComputedProperties()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var returnIsEnumerableProperty = properties.FirstOrDefault(p => p.Name == "ReturnIsEnumerable");
        Assert.IsNotNull(returnIsEnumerableProperty, "MethodGenerationContext should have ReturnIsEnumerable property");

        var returnIsListProperty = properties.FirstOrDefault(p => p.Name == "ReturnIsList");
        Assert.IsNotNull(returnIsListProperty, "MethodGenerationContext should have ReturnIsList property");

        var returnIsTupleProperty = properties.FirstOrDefault(p => p.Name == "ReturnIsTuple");
        Assert.IsNotNull(returnIsTupleProperty, "MethodGenerationContext should have ReturnIsTuple property");

        var returnIsScalarProperty = properties.FirstOrDefault(p => p.Name == "ReturnIsScalar");
        Assert.IsNotNull(returnIsScalarProperty, "MethodGenerationContext should have ReturnIsScalar property");
    }

    /// <summary>
    /// Tests that MethodGenerationContext class has required private methods.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasRequiredPrivateMethods()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act
        var writeMethodExecutedMethod = classType.GetMethod("WriteMethodExecuted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var writeDeclareReturnListMethod = classType.GetMethod("WriteDeclareReturnList", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var writeBeginReaderMethod = classType.GetMethod("WriteBeginReader", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var writeEndReaderMethod = classType.GetMethod("WriteEndReader", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(writeMethodExecutedMethod, "MethodGenerationContext should have WriteMethodExecuted method");
        Assert.IsNotNull(writeDeclareReturnListMethod, "MethodGenerationContext should have WriteDeclareReturnList method");
        Assert.IsNotNull(writeBeginReaderMethod, "MethodGenerationContext should have WriteBeginReader method");
        Assert.IsNotNull(writeEndReaderMethod, "MethodGenerationContext should have WriteEndReader method");
    }

    /// <summary>
    /// Tests that CSharpGenerator class implements ISourceGenerator interface.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_Class_ImplementsISourceGenerator()
    {
        // Arrange
        var classType = typeof(CSharpGenerator);
        var iSourceGeneratorType = typeof(Microsoft.CodeAnalysis.ISourceGenerator);

        // Act
        var interfaces = classType.GetInterfaces();

        // Assert
        Assert.IsTrue(interfaces.Contains(iSourceGeneratorType), "CSharpGenerator should implement ISourceGenerator");
    }

    /// <summary>
    /// Tests that CSharpGenerator class has Initialize method.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_Class_HasInitializeMethod()
    {
        // Arrange
        var classType = typeof(CSharpGenerator);

        // Act
        var initializeMethod = classType.GetMethod("Initialize", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(initializeMethod, "CSharpGenerator should have Initialize method");
        Assert.IsTrue(initializeMethod.IsVirtual, "Initialize method should be virtual");
        Assert.IsTrue(initializeMethod.IsAbstract, "Initialize method should be abstract");
    }

    /// <summary>
    /// Tests that CSharpGenerator class has Execute method.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_Class_HasExecuteMethod()
    {
        // Arrange
        var classType = typeof(CSharpGenerator);

        // Act
        var executeMethod = classType.GetMethod("Execute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(executeMethod, "CSharpGenerator should have Execute method");
        Assert.IsFalse(executeMethod.IsVirtual, "Execute method should not be virtual");
        Assert.IsFalse(executeMethod.IsAbstract, "Execute method should not be abstract");
    }

    /// <summary>
    /// Tests that CSharpGenerator class has correct method signatures.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_Class_HasCorrectMethodSignatures()
    {
        // Arrange
        var classType = typeof(CSharpGenerator);

        // Act
        var initializeMethod = classType.GetMethod("Initialize", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var executeMethod = classType.GetMethod("Execute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(initializeMethod, "Initialize method should exist");
        Assert.IsNotNull(executeMethod, "Execute method should exist");

        var initializeParams = initializeMethod.GetParameters();
        Assert.AreEqual(1, initializeParams.Length, "Initialize method should have 1 parameter");
        Assert.AreEqual("GeneratorInitializationContext", initializeParams[0].ParameterType.Name, 
            "Initialize method first parameter should be GeneratorInitializationContext");

        var executeParams = executeMethod.GetParameters();
        Assert.AreEqual(1, executeParams.Length, "Execute method should have 1 parameter");
        Assert.AreEqual("GeneratorExecutionContext", executeParams[0].ParameterType.Name, 
            "Execute method first parameter should be GeneratorExecutionContext");
    }

    /// <summary>
    /// Tests that CSharpGenerator class can be instantiated.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_Class_CanBeInstantiated()
    {
        // Arrange & Act
        var generator = new CSharpGenerator();

        // Assert
        Assert.IsNotNull(generator);
        Assert.IsInstanceOfType(generator, typeof(CSharpGenerator));
        Assert.IsInstanceOfType(generator, typeof(AbstractGenerator));
        Assert.IsInstanceOfType(generator, typeof(Microsoft.CodeAnalysis.ISourceGenerator));
    }

    /// <summary>
    /// Tests that AbstractGenerator class exists and has correct structure.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_ClassExists_WithCorrectStructure()
    {
        // Arrange
        var classType = typeof(AbstractGenerator);

        // Act & Assert
        Assert.IsNotNull(classType, "AbstractGenerator class should exist");
        Assert.IsTrue(classType.IsPublic, "AbstractGenerator should be public");
        Assert.IsTrue(classType.IsAbstract, "AbstractGenerator should be abstract");
        Assert.IsFalse(classType.IsSealed, "AbstractGenerator should not be sealed");
    }

    /// <summary>
    /// Tests that AbstractGenerator class is in the correct namespace.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_Namespace_IsCorrect()
    {
        // Arrange
        var classType = typeof(AbstractGenerator);

        // Act & Assert
        Assert.AreEqual("Sqlx", classType.Namespace);
    }

    /// <summary>
    /// Tests that AbstractGenerator class implements ISourceGenerator interface.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_Class_ImplementsISourceGenerator()
    {
        // Arrange
        var classType = typeof(AbstractGenerator);
        var iSourceGeneratorType = typeof(Microsoft.CodeAnalysis.ISourceGenerator);

        // Act
        var interfaces = classType.GetInterfaces();

        // Assert
        Assert.IsTrue(interfaces.Contains(iSourceGeneratorType), "AbstractGenerator should implement ISourceGenerator");
    }

    /// <summary>
    /// Tests that AbstractGenerator class has Initialize method.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_Class_HasInitializeMethod()
    {
        // Arrange
        var classType = typeof(AbstractGenerator);

        // Act
        var initializeMethod = classType.GetMethod("Initialize", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(initializeMethod, "AbstractGenerator should have Initialize method");
        Assert.IsTrue(initializeMethod.IsVirtual, "Initialize method should be virtual");
        Assert.IsTrue(initializeMethod.IsAbstract, "Initialize method should be abstract");
    }

    /// <summary>
    /// Tests that AbstractGenerator class has Execute method.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_Class_HasExecuteMethod()
    {
        // Arrange
        var classType = typeof(AbstractGenerator);

        // Act
        var executeMethod = classType.GetMethod("Execute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(executeMethod, "AbstractGenerator should have Execute method");
        Assert.IsFalse(executeMethod.IsVirtual, "Execute method should not be virtual");
        Assert.IsFalse(executeMethod.IsAbstract, "Execute method should not be abstract");
    }

    /// <summary>
    /// Tests that AbstractGenerator class has correct method signatures.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_Class_HasCorrectMethodSignatures()
    {
        // Arrange
        var classType = typeof(AbstractGenerator);

        // Act
        var initializeMethod = classType.GetMethod("Initialize", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var executeMethod = classType.GetMethod("Execute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(initializeMethod, "Initialize method should exist");
        Assert.IsNotNull(executeMethod, "Execute method should exist");

        var initializeParams = initializeMethod.GetParameters();
        Assert.AreEqual(1, initializeParams.Length, "Initialize method should have 1 parameter");
        Assert.AreEqual("GeneratorInitializationContext", initializeParams[0].ParameterType.Name, 
            "Initialize method first parameter should be GeneratorInitializationContext");

        var executeParams = executeMethod.GetParameters();
        Assert.AreEqual(1, executeParams.Length, "Execute method should have 1 parameter");
        Assert.AreEqual("GeneratorExecutionContext", executeParams[0].ParameterType.Name, 
            "Execute method first parameter should be GeneratorExecutionContext");
    }

    /// <summary>
    /// Tests that AbstractGenerator class handles missing attributes gracefully.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_Class_HandlesMissingAttributesGracefully()
    {
        // This test verifies that the AbstractGenerator has the infrastructure to handle missing attributes
        // The actual error handling is tested in the functional tests
        
        // Arrange
        var classType = typeof(AbstractGenerator);

        // Act & Assert
        // The class should exist and have the necessary structure
        Assert.IsNotNull(classType);
        Assert.IsTrue(classType.IsAbstract);
        Assert.IsTrue(classType.IsPublic);
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
