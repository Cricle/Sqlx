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
        Assert.IsFalse(syntaxErrors.Any(), $"Generated code should be syntactically valid. Errors: {string.Join(", ", syntaxErrors.Select(e => e.GetMessage()))}");
    }

    #endregion

    #region NameMapper and Extensions Tests

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

    #endregion

    #region Messages Tests

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
    /// Tests that all Messages diagnostic descriptors have correct properties.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectProperties()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var properties = messagesType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var property in properties)
        {
            var diagnosticDescriptor = property.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            Assert.IsNotNull(diagnosticDescriptor, $"Property {property.Name} should return a DiagnosticDescriptor");
            
            // Test severity
            Assert.AreEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnosticDescriptor.Severity, 
                $"Diagnostic {property.Name} should have Error severity");
            Assert.AreEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnosticDescriptor.DefaultSeverity, 
                $"Diagnostic {property.Name} should have Error as default severity");
            
            // Test other properties
            Assert.IsTrue(diagnosticDescriptor.IsEnabledByDefault, $"Diagnostic {property.Name} should be enabled by default");
            Assert.IsTrue(diagnosticDescriptor.IsSuppressible, $"Diagnostic {property.Name} should be suppressible");
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Title), $"Diagnostic {property.Name} should have a non-empty title");
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.MessageFormat), $"Diagnostic {property.Name} should have a non-empty message format");
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Category), $"Diagnostic {property.Name} should have a non-empty category");
            Assert.IsFalse(string.IsNullOrEmpty(diagnosticDescriptor.Description), $"Diagnostic {property.Name} should have a non-empty description");
            Assert.IsNotNull(diagnosticDescriptor.CustomTags, $"Diagnostic {property.Name} should have custom tags (can be empty)");
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

    #endregion

    #region Consts Tests

    /// <summary>
    /// Tests that Consts class contains valid constant values.
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
    /// Tests that Consts class has correct structure and properties.
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
        Assert.AreEqual("Sqlx", constsType.Namespace, "Consts should be in Sqlx namespace");

        // Test fields
        var fields = constsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.AreEqual(1, fields.Length, "Consts should contain exactly 1 constant field");

        foreach (var field in fields)
        {
            Assert.IsTrue(field.IsPublic, $"Field {field.Name} should be public");
            Assert.IsTrue(field.IsStatic, $"Field {field.Name} should be static");
            Assert.IsTrue(field.IsLiteral, $"Field {field.Name} should be literal (const)");
            Assert.AreEqual(typeof(string), field.FieldType, $"Field {field.Name} should be of type string");
            
            var value = field.GetValue(null);
            Assert.IsNotNull(value, $"Field {field.Name} should have a non-null value");
            Assert.IsTrue(value is string, $"Field {field.Name} should be a string");
            
            var stringValue = (string)value;
            Assert.IsTrue(stringValue.Length > 0, $"Field {field.Name} should have a non-empty string value");
        }
    }

    #endregion

    #region IsExternalInit Tests

    /// <summary>
    /// Tests that IsExternalInit class has correct structure and properties.
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
        Assert.AreEqual("System.Runtime.CompilerServices", isExternalInitType.Namespace, "IsExternalInit should be in correct namespace");
        Assert.AreEqual(typeof(object), isExternalInitType.BaseType, "IsExternalInit should inherit from object");
        Assert.IsFalse(isExternalInitType.IsInterface, "IsExternalInit should not be an interface");
        Assert.IsFalse(isExternalInitType.IsValueType, "IsExternalInit should not be a value type");
        Assert.IsNotNull(isExternalInitType.Assembly, "IsExternalInit should have an assembly");
        Assert.IsNotNull(isExternalInitType.Module, "IsExternalInit should have a module");
        Assert.IsNull(isExternalInitType.DeclaringType, "IsExternalInit should not have a declaring type");
        Assert.AreEqual(isExternalInitType, isExternalInitType.ReflectedType, "IsExternalInit reflected type should be itself");
    }

    #endregion

    #region Interface and Base Class Tests

    /// <summary>
    /// Tests that ISqlxSyntaxReceiver interface has correct structure.
    /// </summary>
    [TestMethod]
    public void ISqlxSyntaxReceiver_Interface_HasCorrectStructure()
    {
        // Arrange
        var interfaceType = typeof(ISqlxSyntaxReceiver);

        // Act & Assert
        Assert.IsNotNull(interfaceType, "ISqlxSyntaxReceiver interface should exist");
        Assert.IsTrue(interfaceType.IsInterface, "ISqlxSyntaxReceiver should be an interface");
        Assert.IsTrue(interfaceType.IsNotPublic, "ISqlxSyntaxReceiver should be internal");
        Assert.AreEqual("Sqlx", interfaceType.Namespace, "ISqlxSyntaxReceiver should be in Sqlx namespace");
        
        // Check that it inherits from ISyntaxContextReceiver
        var baseInterfaces = interfaceType.GetInterfaces();
        Assert.IsTrue(baseInterfaces.Any(i => i.Name == "ISyntaxContextReceiver"), 
            "ISqlxSyntaxReceiver should inherit from ISyntaxContextReceiver");

        // Test Methods property
        var methodsProperty = interfaceType.GetProperty("Methods");
        Assert.IsNotNull(methodsProperty, "ISqlxSyntaxReceiver should have a Methods property");
        Assert.IsTrue(methodsProperty.CanRead, "Methods property should be readable");
        Assert.IsFalse(methodsProperty.CanWrite, "Methods property should not be writable");
        
        var propertyType = methodsProperty.PropertyType;
        Assert.IsTrue(propertyType.IsGenericType, "Methods property should be generic");
        Assert.AreEqual("List`1", propertyType.Name, "Methods property should be List<T>");
        
        var genericArguments = propertyType.GetGenericArguments();
        Assert.AreEqual(1, genericArguments.Length, "Methods property should have one generic argument");
        Assert.AreEqual("IMethodSymbol", genericArguments[0].Name, "Generic argument should be IMethodSymbol");
    }

    /// <summary>
    /// Tests that GenerationContextBase class has correct structure.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_Class_HasCorrectStructure()
    {
        // Arrange
        var baseType = typeof(GenerationContextBase);

        // Act & Assert
        Assert.IsNotNull(baseType, "GenerationContextBase class should exist");
        Assert.IsTrue(baseType.IsNotPublic, "GenerationContextBase should be internal");
        Assert.IsTrue(baseType.IsAbstract, "GenerationContextBase should be abstract");
        Assert.IsFalse(baseType.IsSealed, "GenerationContextBase should not be sealed");
        Assert.AreEqual("Sqlx", baseType.Namespace, "GenerationContextBase should be in Sqlx namespace");
        Assert.AreEqual(typeof(object), baseType.BaseType, "GenerationContextBase should inherit from object");

        // Test abstract properties
        var properties = baseType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var expectedProperties = new[] { "DbConnection", "TransactionParameter", "DbContext" };
        
        foreach (var propertyName in expectedProperties)
        {
            var property = properties.FirstOrDefault(p => p.Name == propertyName);
            Assert.IsNotNull(property, $"GenerationContextBase should have {propertyName} property");
            Assert.IsTrue(property.CanRead, $"{propertyName} property should be readable");
            Assert.IsFalse(property.CanWrite, $"{propertyName} property should not be writable");
            Assert.AreEqual("ISymbol", property.PropertyType.Name, $"{propertyName} property should be of type ISymbol");
            Assert.IsTrue(property.GetMethod.IsAbstract, $"{propertyName} property should be abstract");
        }

        // Test GetSymbol method
        var getSymbolMethod = baseType.GetMethod("GetSymbol", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(getSymbolMethod, "GenerationContextBase should have GetSymbol method");
        Assert.IsTrue(getSymbolMethod.IsStatic, "GetSymbol method should be static");
        Assert.IsTrue(getSymbolMethod.IsPrivate, "GetSymbol method should be private");
        
        var parameters = getSymbolMethod.GetParameters();
        Assert.AreEqual(2, parameters.Length, "GetSymbol method should have 2 parameters");
        Assert.AreEqual("ISymbol", parameters[0].ParameterType.Name, "GetSymbol method first parameter should be ISymbol");
        Assert.AreEqual("Func`2", parameters[1].ParameterType.Name, "GetSymbol method second parameter should be Func<ISymbol, bool>");
        Assert.AreEqual(typeof(ISymbol), getSymbolMethod.ReturnType, "GetSymbol method should return ISymbol");
    }

    #endregion

    #region ClassGenerationContext Tests

    /// <summary>
    /// Tests that ClassGenerationContext class has correct structure and properties.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Class_HasCorrectStructure()
    {
        // Arrange
        var classType = typeof(ClassGenerationContext);

        // Act & Assert
        Assert.IsNotNull(classType, "ClassGenerationContext class should exist");
        Assert.IsTrue(classType.IsNotPublic, "ClassGenerationContext should be internal");
        Assert.IsFalse(classType.IsAbstract, "ClassGenerationContext should not be abstract");
        Assert.IsFalse(classType.IsSealed, "ClassGenerationContext should not be sealed");
        Assert.AreEqual("Sqlx", classType.Namespace, "ClassGenerationContext should be in Sqlx namespace");
        Assert.AreEqual(typeof(GenerationContextBase), classType.BaseType, "ClassGenerationContext should inherit from GenerationContextBase");

        // Test constructor
        var constructors = classType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsTrue(constructors.Length > 0, "ClassGenerationContext should have at least one constructor");

        var constructor = constructors.First();
        var parameters = constructor.GetParameters();
        Assert.AreEqual(3, parameters.Length, "Constructor should have 3 parameters");
        Assert.AreEqual("INamedTypeSymbol", parameters[0].ParameterType.Name, "First parameter should be INamedTypeSymbol");
        Assert.AreEqual("List`1", parameters[1].ParameterType.Name, "Second parameter should be List<IMethodSymbol>");
        Assert.AreEqual("INamedTypeSymbol", parameters[2].ParameterType.Name, "Third parameter should be INamedTypeSymbol");

        // Test methods
        var createSourceMethod = classType.GetMethod("CreateSource", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getFieldOrPropertyMethod = classType.GetMethod("GetFieldOrProperty", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getAttributeMethod = classType.GetMethod("GetAttribute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(createSourceMethod, "CreateSource method should exist");
        Assert.IsNotNull(getFieldOrPropertyMethod, "GetFieldOrProperty method should exist");
        Assert.IsNotNull(getAttributeMethod, "GetAttribute method should exist");

        // Test method signatures
        var createSourceParams = createSourceMethod.GetParameters();
        Assert.AreEqual(1, createSourceParams.Length, "CreateSource method should have 1 parameter");
        Assert.AreEqual("IndentedStringBuilder", createSourceParams[0].ParameterType.Name, "CreateSource method parameter should be IndentedStringBuilder");

        var getFieldOrPropertyParams = getFieldOrPropertyMethod.GetParameters();
        Assert.AreEqual(1, getFieldOrPropertyParams.Length, "GetFieldOrProperty method should have 1 parameter");
        Assert.AreEqual("Func`2", getFieldOrPropertyParams[0].ParameterType.Name, "GetFieldOrProperty method parameter should be Func<ISymbol, bool>");

        var getAttributeParams = getAttributeMethod.GetParameters();
        Assert.AreEqual(1, getAttributeParams.Length, "GetAttribute method should have 1 parameter");
        Assert.AreEqual("Func`2", getAttributeParams[0].ParameterType.Name, "GetAttribute method parameter should be Func<AttributeData, bool>");

        // Test return types
        Assert.AreEqual(typeof(bool), createSourceMethod.ReturnType, "CreateSource method should return bool");
        Assert.AreEqual(typeof(ISymbol), getFieldOrPropertyMethod.ReturnType, "GetFieldOrProperty method should return ISymbol");
        Assert.AreEqual(typeof(AttributeData), getAttributeMethod.ReturnType, "GetAttribute method should return AttributeData");

        // Test method accessibility
        Assert.IsTrue(createSourceMethod.IsPublic, "CreateSource method should be public");
        Assert.IsTrue(getFieldOrPropertyMethod.IsPublic, "GetFieldOrProperty method should be public");
        Assert.IsTrue(getAttributeMethod.IsPublic, "GetAttribute method should be public");

        // Test properties
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var expectedProperties = new[] { "ClassSymbol", "Methods", "SqlxAttributeSymbol", "GeneratorExecutionContext" };
        
        foreach (var propertyName in expectedProperties)
        {
            var property = properties.FirstOrDefault(p => p.Name == propertyName);
            Assert.IsNotNull(property, $"ClassGenerationContext should have {propertyName} property");
            Assert.IsTrue(property.CanRead, $"{propertyName} property should be readable");
            Assert.IsFalse(property.CanWrite, $"{propertyName} property should not be writable");
        }

        // Test specific property types
        var classSymbolProperty = properties.FirstOrDefault(p => p.Name == "ClassSymbol");
        Assert.AreEqual("INamedTypeSymbol", classSymbolProperty.PropertyType.Name, "ClassSymbol property should be of type INamedTypeSymbol");

        var methodsProperty = properties.FirstOrDefault(p => p.Name == "Methods");
        Assert.AreEqual("List`1", methodsProperty.PropertyType.Name, "Methods property should be of type List<MethodGenerationContext>");

        var sqlxAttributeSymbolProperty = properties.FirstOrDefault(p => p.Name == "SqlxAttributeSymbol");
        Assert.AreEqual("INamedTypeSymbol", sqlxAttributeSymbolProperty.PropertyType.Name, "SqlxAttributeSymbol property should be of type INamedTypeSymbol");

        var generatorExecutionContextProperty = properties.FirstOrDefault(p => p.Name == "GeneratorExecutionContext");
        Assert.AreEqual("GeneratorExecutionContext", generatorExecutionContextProperty.PropertyType.Name, "GeneratorExecutionContext property should be of type GeneratorExecutionContext");
    }

    #endregion

    #region MethodGenerationContext Tests

    /// <summary>
    /// Tests that MethodGenerationContext class has correct structure and properties.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_Class_HasCorrectStructure()
    {
        // Arrange
        var classType = typeof(MethodGenerationContext);

        // Act & Assert
        Assert.IsNotNull(classType, "MethodGenerationContext class should exist");
        Assert.IsTrue(classType.IsNotPublic, "MethodGenerationContext should be internal");
        Assert.IsFalse(classType.IsAbstract, "MethodGenerationContext should not be abstract");
        Assert.IsFalse(classType.IsSealed, "MethodGenerationContext should not be sealed");
        Assert.AreEqual("Sqlx", classType.Namespace, "MethodGenerationContext should be in Sqlx namespace");
        Assert.AreEqual(typeof(GenerationContextBase), classType.BaseType, "MethodGenerationContext should inherit from GenerationContextBase");

        // Test public methods
        var declareCommandMethod = classType.GetMethod("DeclareCommand", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeExecuteNoQueryMethod = classType.GetMethod("WriteExecuteNoQuery", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeScalarMethod = classType.GetMethod("WriteScalar", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var writeReturnMethod = classType.GetMethod("WriteReturn", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(declareCommandMethod, "DeclareCommand method should exist");
        Assert.IsNotNull(writeExecuteNoQueryMethod, "WriteExecuteNoQuery method should exist");
        Assert.IsNotNull(writeScalarMethod, "WriteScalar method should exist");
        Assert.IsNotNull(writeReturnMethod, "WriteReturn method should exist");

        // Test method signatures
        var declareCommandParams = declareCommandMethod.GetParameters();
        Assert.AreEqual(1, declareCommandParams.Length, "DeclareCommand method should have 1 parameter");
        Assert.AreEqual("IndentedStringBuilder", declareCommandParams[0].ParameterType.Name, "DeclareCommand method parameter should be IndentedStringBuilder");

        var writeExecuteNoQueryParams = writeExecuteNoQueryMethod.GetParameters();
        Assert.AreEqual(2, writeExecuteNoQueryParams.Length, "WriteExecuteNoQuery method should have 2 parameters");
        Assert.AreEqual("IndentedStringBuilder", writeExecuteNoQueryParams[0].ParameterType.Name, "WriteExecuteNoQuery method first parameter should be IndentedStringBuilder");
        Assert.AreEqual("List`1", writeExecuteNoQueryParams[1].ParameterType.Name, "WriteExecuteNoQuery method second parameter should be List<ColumnDefine>");

        var writeScalarParams = writeScalarMethod.GetParameters();
        Assert.AreEqual(2, writeScalarParams.Length, "WriteScalar method should have 2 parameters");
        Assert.AreEqual("IndentedStringBuilder", writeScalarParams[0].ParameterType.Name, "WriteScalar method first parameter should be IndentedStringBuilder");
        Assert.AreEqual("List`1", writeScalarParams[1].ParameterType.Name, "WriteScalar method second parameter should be List<ColumnDefine>");

        var writeReturnParams = writeReturnMethod.GetParameters();
        Assert.AreEqual(2, writeReturnParams.Length, "WriteReturn method should have 2 parameters");
        Assert.AreEqual("IndentedStringBuilder", writeReturnParams[0].ParameterType.Name, "WriteReturn method first parameter should be IndentedStringBuilder");
        Assert.AreEqual("List`1", writeReturnParams[1].ParameterType.Name, "WriteReturn method second parameter should be List<ColumnDefine>");

        // Test return types
        Assert.AreEqual(typeof(bool), declareCommandMethod.ReturnType, "DeclareCommand method should return bool");
        Assert.AreEqual(typeof(bool), writeExecuteNoQueryMethod.ReturnType, "WriteExecuteNoQuery method should return bool");
        Assert.AreEqual(typeof(void), writeScalarMethod.ReturnType, "WriteScalar method should return void");
        Assert.AreEqual(typeof(bool), writeReturnMethod.ReturnType, "WriteReturn method should return bool");

        // Test method accessibility
        Assert.IsTrue(declareCommandMethod.IsPublic, "DeclareCommand method should be public");
        Assert.IsTrue(writeExecuteNoQueryMethod.IsPublic, "WriteExecuteNoQuery method should be public");
        Assert.IsTrue(writeScalarMethod.IsPublic, "WriteScalar method should be public");
        Assert.IsTrue(writeReturnMethod.IsPublic, "WriteReturn method should be public");

        // Test properties
        var properties = classType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var expectedProperties = new[] { "MethodSymbol", "ClassGenerationContext", "ReturnType", "IsAsync", "ReturnIsEnumerable", "ReturnIsList", "ReturnIsTuple", "ReturnIsScalar" };
        
        foreach (var propertyName in expectedProperties)
        {
            var property = properties.FirstOrDefault(p => p.Name == propertyName);
            Assert.IsNotNull(property, $"MethodGenerationContext should have {propertyName} property");
            Assert.IsTrue(property.CanRead, $"{propertyName} property should be readable");
            Assert.IsFalse(property.CanWrite, $"{propertyName} property should not be writable");
        }

        // Test specific property types
        var methodSymbolProperty = properties.FirstOrDefault(p => p.Name == "MethodSymbol");
        Assert.AreEqual("IMethodSymbol", methodSymbolProperty.PropertyType.Name, "MethodSymbol property should be of type IMethodSymbol");

        var classGenerationContextProperty = properties.FirstOrDefault(p => p.Name == "ClassGenerationContext");
        Assert.AreEqual("ClassGenerationContext", classGenerationContextProperty.PropertyType.Name, "ClassGenerationContext property should be of type ClassGenerationContext");

        var returnTypeProperty = properties.FirstOrDefault(p => p.Name == "ReturnType");
        Assert.AreEqual("ITypeSymbol", returnTypeProperty.PropertyType.Name, "ReturnType property should be of type ITypeSymbol");

        var isAsyncProperty = properties.FirstOrDefault(p => p.Name == "IsAsync");
        Assert.AreEqual("Boolean", isAsyncProperty.PropertyType.Name, "IsAsync property should be of type Boolean");

        // Test private methods
        var writeMethodExecutedMethod = classType.GetMethod("WriteMethodExecuted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var writeDeclareReturnListMethod = classType.GetMethod("WriteDeclareReturnList", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var writeBeginReaderMethod = classType.GetMethod("WriteBeginReader", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var writeEndReaderMethod = classType.GetMethod("WriteEndReader", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(writeMethodExecutedMethod, "MethodGenerationContext should have WriteMethodExecuted method");
        Assert.IsNotNull(writeDeclareReturnListMethod, "MethodGenerationContext should have WriteDeclareReturnList method");
        Assert.IsNotNull(writeBeginReaderMethod, "MethodGenerationContext should have WriteBeginReader method");
        Assert.IsNotNull(writeEndReaderMethod, "MethodGenerationContext should have WriteEndReader method");
    }

    #endregion

    #region Generator Tests

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
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var executeMethod = classType.GetMethod("Execute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(initializeMethod, "CSharpGenerator should have Initialize method");
        Assert.IsNotNull(executeMethod, "CSharpGenerator should have Execute method");

        // Test method signatures
        var initializeParams = initializeMethod.GetParameters();
        Assert.AreEqual(1, initializeParams.Length, "Initialize method should have 1 parameter");
        Assert.AreEqual("GeneratorInitializationContext", initializeParams[0].ParameterType.Name, "Initialize method first parameter should be GeneratorInitializationContext");

        var executeParams = executeMethod.GetParameters();
        Assert.AreEqual(1, executeParams.Length, "Execute method should have 1 parameter");
        Assert.AreEqual("GeneratorExecutionContext", executeParams[0].ParameterType.Name, "Execute method first parameter should be GeneratorExecutionContext");

        // Test method properties
        Assert.IsTrue(initializeMethod.IsVirtual, "Initialize method should be virtual");
        Assert.IsTrue(initializeMethod.IsAbstract, "Initialize method should be abstract");
        Assert.IsFalse(executeMethod.IsVirtual, "Execute method should not be virtual");
        Assert.IsFalse(executeMethod.IsAbstract, "Execute method should not be abstract");

        // Test instantiation
        var generator = new CSharpGenerator();
        Assert.IsNotNull(generator);
        Assert.IsInstanceOfType(generator, typeof(CSharpGenerator));
        Assert.IsInstanceOfType(generator, typeof(AbstractGenerator));
        Assert.IsInstanceOfType(generator, typeof(Microsoft.CodeAnalysis.ISourceGenerator));
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
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var executeMethod = classType.GetMethod("Execute", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(initializeMethod, "AbstractGenerator should have Initialize method");
        Assert.IsNotNull(executeMethod, "AbstractGenerator should have Execute method");

        // Test method signatures
        var initializeParams = initializeMethod.GetParameters();
        Assert.AreEqual(1, initializeParams.Length, "Initialize method should have 1 parameter");
        Assert.AreEqual("GeneratorInitializationContext", initializeParams[0].ParameterType.Name, "Initialize method first parameter should be GeneratorInitializationContext");

        var executeParams = executeMethod.GetParameters();
        Assert.AreEqual(1, executeParams.Length, "Execute method should have 1 parameter");
        Assert.AreEqual("GeneratorExecutionContext", executeParams[0].ParameterType.Name, "Execute method first parameter should be GeneratorExecutionContext");

        // Test method properties
        Assert.IsTrue(initializeMethod.IsVirtual, "Initialize method should be virtual");
        Assert.IsTrue(initializeMethod.IsAbstract, "Initialize method should be abstract");
        Assert.IsFalse(executeMethod.IsVirtual, "Execute method should not be virtual");
        Assert.IsFalse(executeMethod.IsAbstract, "Execute method should not be abstract");
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
