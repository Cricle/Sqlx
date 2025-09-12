using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class TypeAnalyzerTests
{
    private Compilation _compilation = null!;

    [TestInitialize]
    public void Setup()
    {
        var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace 
{
    public class User 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    public class Product 
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
            });

        _compilation = compilation;
    }

    [TestMethod]
    public void IsLikelyEntityType_WithEntityClass_ReturnsTrue()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(userType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithPrimitiveType_ReturnsFalse()
    {
        // Arrange
        var intType = _compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(intType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithStringType_ReturnsFalse()
    {
        // Arrange
        var stringType = _compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(stringType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithNullType_ReturnsFalse()
    {
        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsCollectionType_WithListType_ReturnsTrue()
    {
        // Arrange
        var listType = GetGenericTypeSymbol("System.Collections.Generic.List", "TestNamespace.User");

        // Act
        var result = TypeAnalyzer.IsCollectionType(listType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsCollectionType_WithNonCollectionType_ReturnsFalse()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = TypeAnalyzer.IsCollectionType(userType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsAsyncType_WithTaskType_ReturnsTrue()
    {
        // Arrange
        var taskType = GetTypeSymbol("System.Threading.Tasks.Task");

        // Act
        var result = TypeAnalyzer.IsAsyncType(taskType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsAsyncType_WithGenericTaskType_ReturnsTrue()
    {
        // Arrange
        var taskType = GetGenericTypeSymbol("System.Threading.Tasks.Task", "TestNamespace.User");

        // Act
        var result = TypeAnalyzer.IsAsyncType(taskType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsAsyncType_WithNonAsyncType_ReturnsFalse()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = TypeAnalyzer.IsAsyncType(userType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsScalarReturnType_WithIntType_ReturnsTrue()
    {
        // Arrange
        var intType = _compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.IsScalarReturnType(intType, false);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsScalarReturnType_WithStringType_ReturnsTrue()
    {
        // Arrange
        var stringType = _compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsScalarReturnType(stringType, false);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsScalarReturnType_WithEntityType_ReturnsFalse()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = TypeAnalyzer.IsScalarReturnType(userType, false);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetDefaultValue_WithIntType_ReturnsCorrectDefault()
    {
        // Arrange
        var intType = _compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.GetDefaultValue(intType);

        // Assert
        Assert.AreEqual("0", result);
    }

    [TestMethod]
    public void GetDefaultValue_WithStringType_ReturnsCorrectDefault()
    {
        // Arrange
        var stringType = _compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.GetDefaultValue(stringType);

        // Assert
        Assert.AreEqual("string.Empty", result);
    }

    [TestMethod]
    public void GetDefaultValue_WithBoolType_ReturnsCorrectDefault()
    {
        // Arrange
        var boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

        // Act
        var result = TypeAnalyzer.GetDefaultValue(boolType);

        // Assert
        Assert.AreEqual("false", result);
    }

    [TestMethod]
    public void GetInnerType_WithListOfUser_ReturnsListType()
    {
        // Arrange
        var listType = GetGenericTypeSymbol("System.Collections.Generic.List", "TestNamespace.User");

        // Act
        var result = TypeAnalyzer.GetInnerType(listType);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.Name); // GetInnerType only unwraps Task<T>, not collections
    }

    [TestMethod]
    public void GetInnerType_WithTaskOfUser_ReturnsUserType()
    {
        // Arrange
        var taskType = GetGenericTypeSymbol("System.Threading.Tasks.Task", "TestNamespace.User");

        // Act
        var result = TypeAnalyzer.GetInnerType(taskType);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("User", result.Name);
    }

    [TestMethod]
    public void GetInnerType_WithNonGenericType_ReturnsOriginalType()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = TypeAnalyzer.GetInnerType(userType);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("User", result.Name);
    }

    #region Helper Methods

    private INamedTypeSymbol GetTypeSymbol(string typeName)
    {
        var type = _compilation.GetTypeByMetadataName(typeName);
        Assert.IsNotNull(type, $"Could not find type: {typeName}");
        return type;
    }

    private INamedTypeSymbol GetGenericTypeSymbol(string genericTypeName, string typeArgumentName)
    {
        var genericType = _compilation.GetTypeByMetadataName(genericTypeName + "`1");
        var typeArgument = GetTypeSymbol(typeArgumentName);

        Assert.IsNotNull(genericType, $"Could not find generic type: {genericTypeName}");
        Assert.IsNotNull(typeArgument, $"Could not find type argument: {typeArgumentName}");

        return genericType.Construct(typeArgument);
    }

    #endregion
}
